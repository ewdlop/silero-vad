using NAudio.Wave;
using SpeexDSPSharp.Core;
using System.Collections.Concurrent;

namespace VadDotNet
{
    public class SimpleEnhancedEchoCancellation : IWaveProvider
    {
        private IWaveProvider _source;
        private SpeexDSPEchoCanceler _canceller;
        private SpeexDSPPreprocessor _preprocessor;
        private readonly int _frameSize;
        private readonly int _frameSizeBytes;
        private readonly ConcurrentQueue<byte[]> _playbackFrames = new ConcurrentQueue<byte[]>();
        private byte[] _playbackBuffer = new byte[0];
        private readonly object _lockObject = new object();

        public WaveFormat WaveFormat => _source.WaveFormat;

        public SimpleEnhancedEchoCancellation(int frameSizeMs, int filterLengthMs, IWaveProvider source)
        {
            _source = source;
            var sampleRate = WaveFormat.SampleRate;
            _frameSize = frameSizeMs * sampleRate / 1000;
            _frameSizeBytes = _frameSize * (WaveFormat.BitsPerSample / 8) * WaveFormat.Channels;

            // Use longer filter for better echo modeling - try 400-800ms
            var filterLength = Math.Max(filterLengthMs * sampleRate / 1000, _frameSize * 12);

            Console.WriteLine($"Enhanced canceler frame size: {_frameSize} samples ({_frameSizeBytes} bytes)");
            Console.WriteLine($"Enhanced filter length: {filterLength} samples (~{filterLength * 1000 / sampleRate}ms)");

            // Initialize echo canceller with better settings
            _canceller = new SpeexDSPEchoCanceler(_frameSize, filterLength);
            _canceller.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref sampleRate);



            // Initialize separate preprocessor (not linked)
            _preprocessor = new SpeexDSPPreprocessor(_frameSize, sampleRate);
            ConfigurePreprocessor();
        }

        private void ConfigurePreprocessor()
        {
            int enable = 1;

            // Very aggressive noise and residual echo suppression
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref enable);

            int noiseSuppress = -40;  // Aggressive noise suppression
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_NOISE_SUPPRESS, ref noiseSuppress);

            // Aggressive residual echo suppression (independent of main canceller)
            int echoSuppress = -55;   // Very aggressive
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_ECHO_SUPPRESS, ref echoSuppress);
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_ECHO_SUPPRESS_ACTIVE, ref echoSuppress);

            // Tune probability thresholds for better echo detection
            int probStart = 45;       // Higher threshold to start suppression
            int probContinue = 30;    // Higher threshold to continue suppression
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_PROB_START, ref probStart);
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_PROB_CONTINUE, ref probContinue);

            // Enable other enhancements
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref enable);
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref enable);
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref enable);

            float agcLevel = 7000f;
            _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_LEVEL, ref agcLevel);

            Console.WriteLine("Enhanced preprocessor configured with aggressive residual echo suppression");
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);

            if (samplesRead > 0)
            {
                lock (_lockObject)
                {
                    ProcessPlaybackFrames(buffer, offset, samplesRead);
                }
            }

            return samplesRead;
        }

        private void ProcessPlaybackFrames(byte[] buffer, int offset, int samplesRead)
        {
            // Add new data to playback buffer
            byte[] newBuffer = new byte[_playbackBuffer.Length + samplesRead];
            Array.Copy(_playbackBuffer, 0, newBuffer, 0, _playbackBuffer.Length);
            Array.Copy(buffer, offset, newBuffer, _playbackBuffer.Length, samplesRead);
            _playbackBuffer = newBuffer;

            // Process complete frames
            while (_playbackBuffer.Length >= _frameSizeBytes)
            {
                byte[] frame = new byte[_frameSizeBytes];
                Array.Copy(_playbackBuffer, 0, frame, 0, _frameSizeBytes);

                // Optional: Apply slight volume reduction to reduce echo at source
                ApplyPlaybackVolumeReduction(frame, 0.9); // 10% reduction

                _playbackFrames.Enqueue(frame);

                // Limit queue size
                while (_playbackFrames.Count > 25)
                {
                    _playbackFrames.TryDequeue(out _);
                }

                _canceller.EchoPlayback(frame);

                // Remove processed frame
                byte[] remainingBuffer = new byte[_playbackBuffer.Length - _frameSizeBytes];
                Array.Copy(_playbackBuffer, _frameSizeBytes, remainingBuffer, 0, remainingBuffer.Length);
                _playbackBuffer = remainingBuffer;
            }
        }

        private void ApplyPlaybackVolumeReduction(byte[] frame, double volumeFactor)
        {
            for (int i = 0; i < frame.Length; i += 2)
            {
                short sample = BitConverter.ToInt16(frame, i);
                sample = (short)(sample * volumeFactor);
                BitConverter.GetBytes(sample).CopyTo(frame, i);
            }
        }

        public void Cancel(byte[] micBuffer, byte[] outputBuffer)
        {
            lock (_lockObject)
            {
                if (micBuffer.Length != _frameSizeBytes)
                {
                    Console.WriteLine($"WARNING: Buffer size mismatch {micBuffer.Length} != {_frameSizeBytes}");
                    Array.Copy(micBuffer, outputBuffer, Math.Min(micBuffer.Length, outputBuffer.Length));
                    return;
                }

                if (_playbackFrames.TryDequeue(out byte[] playbackFrame))
                {
                    Console.WriteLine("Enhanced 3-stage processing: Echo → Residual → Noise");

                    // Stage 1: Primary echo cancellation with optimized canceller
                    byte[] stage1Buffer = new byte[_frameSizeBytes];
                    _canceller.EchoCancel(micBuffer, playbackFrame, stage1Buffer);

                    // Stage 2: Secondary echo suppression + noise reduction
                    Array.Copy(stage1Buffer, outputBuffer, _frameSizeBytes);
                    int voiceActivity = _preprocessor.Run(outputBuffer);

                    // Stage 3: Additional manual suppression for persistent echoes
                    ApplyAdditionalEchoSuppression(outputBuffer, playbackFrame);

                    Console.WriteLine($"Voice activity: {voiceActivity != 0}");
                }
                else
                {
                    Console.WriteLine("No playback frame - noise suppression only");
                    Array.Copy(micBuffer, outputBuffer, micBuffer.Length);
                    _preprocessor.Run(outputBuffer);
                }
            }
        }

        private void ApplyAdditionalEchoSuppression(byte[] buffer, byte[] referenceFrame)
        {
            // Calculate energy levels
            double bufferRMS = CalculateRMS(buffer);
            double referenceRMS = CalculateRMS(referenceFrame);

            // If reference signal is strong and buffer still has significant energy,
            // apply additional suppression
            if (referenceRMS > 1000 && bufferRMS > 500)
            {
                double suppressionFactor = 0.4; // Aggressive suppression
                Console.WriteLine($"Applying additional suppression: Ref={referenceRMS:F0}, Buf={bufferRMS:F0}");

                for (int i = 0; i < buffer.Length; i += 2)
                {
                    short sample = BitConverter.ToInt16(buffer, i);
                    sample = (short)(sample * suppressionFactor);
                    BitConverter.GetBytes(sample).CopyTo(buffer, i);
                }
            }
        }

        private double CalculateRMS(byte[] buffer)
        {
            if (buffer.Length < 2) return 0;

            double sum = 0;
            int sampleCount = buffer.Length / 2;

            for (int i = 0; i < buffer.Length; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                sum += sample * sample;
            }

            return Math.Sqrt(sum / sampleCount);
        }

        public int AvailablePlaybackFrames => _playbackFrames.Count;
        public int BufferedPlaybackBytes => _playbackBuffer.Length;

        public void Dispose()
        {
            _canceller?.Dispose();
            _preprocessor?.Dispose();
        }
    }
}