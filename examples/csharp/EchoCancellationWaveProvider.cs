using NAudio.Wave;
using SpeexDSPSharp.Core;
using SpeexDSPSharp.Core.SafeHandlers;

namespace VadDotNet
{
    public class CustomSpeexDSPEchoCanceler(int frameSize, int filterLength) : SpeexDSPEchoCanceler(frameSize, filterLength)
    {
        public SpeexDSPEchoStateSafeHandler Handler => _handler;
    }

    public class CustomSpeexDSPPreprocessor(int frameSize, int filterLength) : SpeexDSPPreprocessor(frameSize, filterLength)
    {
        public SpeexDSPPreprocessStateSafeHandler Handler => _handler;
    }

    public class EchoCancellationWaveProvider : IWaveProvider
    {
        private readonly IWaveProvider _source;
        private readonly CustomSpeexDSPEchoCanceler _canceller;
        private readonly CustomSpeexDSPPreprocessor _preprocessor;
        private readonly int _frameSize;
        private readonly int _sampleRate;

        public WaveFormat WaveFormat => _source.WaveFormat;

        public unsafe EchoCancellationWaveProvider(int frameSizeMS, int filterLengthMS, IWaveProvider source)
        {
            _source = source;
            _sampleRate = source.WaveFormat.SampleRate;

            // Calculate frame size and filter length in samples
            _frameSize = frameSizeMS * _sampleRate / 1000;
            var filterLength = filterLengthMS * _sampleRate / 1000;

            // Initialize echo canceller - equivalent to speex_echo_state_init
            _canceller = new CustomSpeexDSPEchoCanceler(_frameSize, filterLength);

            // Set sampling rate - equivalent to speex_echo_ctl(st, SPEEX_ECHO_SET_SAMPLING_RATE, &sampleRate)
            _canceller.Ctl(EchoCancellationCtl.SPEEX_ECHO_SET_SAMPLING_RATE, ref _sampleRate);

            // Initialize preprocessor - equivalent to speex_preprocess_state_init
            _preprocessor = new CustomSpeexDSPPreprocessor(_frameSize, _sampleRate);

            //// Link preprocessor with echo canceller - equivalent to speex_preprocess_ctl(den, SPEEX_PREPROCESS_SET_ECHO_STATE, st)
            var echoStatePtr = _canceller.Handler.DangerousGetHandle();
            if (NativeSpeexDSP.speex_preprocess_ctl(_preprocessor.Handler, PreprocessorCtl.SPEEX_PREPROCESS_SET_ECHO_STATE.GetHashCode(), echoStatePtr.ToPointer()) == 0)
            {
                Console.WriteLine("Preprocessor linked with echo canceller successfully.");
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesRead = _source.Read(buffer, offset, count);
            if (samplesRead > 0)
            {
                // Feed reference signal (speaker output) to echo canceller
                _canceller.EchoPlayback(buffer);
                return samplesRead;
            }
            return samplesRead;
        }

        /// <summary>
        /// Perform echo cancellation on captured audio
        /// Equivalent to: speex_echo_cancellation(st, ref_buf, echo_buf, e_buf)
        /// followed by: speex_preprocess_run(den, e_buf)
        /// </summary>
        /// <param name="referenceBuffer">Speaker/reference audio (ref_buf)</param>
        /// <param name="capturedBuffer">Microphone captured audio (echo_buf)</param>
        /// <param name="outputBuffer">Echo cancelled output (e_buf)</param>
        public void Cancel(byte[] referenceBuffer, byte[] capturedBuffer, byte[] outputBuffer)
        {
            // Perform echo cancellation
            _canceller.EchoCancel(referenceBuffer, capturedBuffer, outputBuffer);

            // Apply preprocessing (noise reduction, etc.)
            _preprocessor.Run(outputBuffer);
        }

        /// <summary>
        /// Simplified cancellation method for single buffer processing
        /// </summary>
        /// <param name="inputBuffer">Input audio buffer</param>
        /// <param name="outputBuffer">Processed output buffer</param>
        public void Cancel(byte[] inputBuffer, byte[] outputBuffer)
        {
            _canceller.EchoCapture(inputBuffer, outputBuffer);
            _preprocessor.Run(outputBuffer);
        }

        public void EchoPlayBack(byte[] echoPlayback)
        {
            _canceller.EchoPlayback(echoPlayback);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            _canceller?.Dispose();
            _preprocessor?.Dispose();
        }
    }
}