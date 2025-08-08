using NAudio.Wave;

namespace VadDotNet;

public class RealTimeVadDetectorTweaked : IDisposable
{
    private readonly SileroVadDetector _vadDetector;
    private readonly WaveInEvent _waveIn;
    private readonly int _sampleRate;
    private readonly int _windowSize;
    private readonly float _threshold;
    
    private readonly Queue<float> _audioBuffer;
    private readonly object _bufferLock = new object();
    private bool _isRecording = false;
    private bool _isDisposed = false;
    
    // Speech detection events
    public event EventHandler<SpeechDetectedEventArgs> SpeechDetected;
    public event EventHandler<SpeechEndedEventArgs> SpeechEnded;
    public event EventHandler<VoiceActivityEventArgs> VoiceActivityChanged;
    
    // Statistics
    public int TotalFramesProcessed { get; private set; }
    public int SpeechFramesDetected { get; private set; }
    public double SpeechActivityRatio => TotalFramesProcessed > 0 ? (double)SpeechFramesDetected / TotalFramesProcessed : 0;
    
    public RealTimeVadDetectorTweaked(string onnxModelPath, float threshold = 0.5f, int sampleRate = 16000)
    {
        _sampleRate = sampleRate;
        _threshold = threshold;
        _windowSize = sampleRate == 16000 ? 512 : 256;
        
        // Initialize VAD detector
        _vadDetector = new SileroVadDetector(
            onnxModelPath, 
            threshold, 
            sampleRate,
            250,    // Minimum speech duration (ms)
            float.PositiveInfinity,  // Maximum speech duration
            1000,    // Minimum silence duration (ms)
            30      // Speech padding time (ms)
        );
        
        // Initialize audio buffer
        _audioBuffer = new Queue<float>();
        
        // Initialize WaveIn
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(sampleRate, 16, 1),
            BufferMilliseconds = 20
        };
        
        _waveIn.DataAvailable += OnDataAvailable;
    }
    
    public void StartRecording()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(RealTimeVadDetector));
        if (_isRecording) return;
        
        _isRecording = true;
        _waveIn.StartRecording();
        Console.WriteLine($"🎤 Started real-time speech detection (Sample rate: {_sampleRate}Hz, Threshold: {_threshold:F2})");
    }
    
    public void StopRecording()
    {
        if (!_isRecording) return;
        
        _isRecording = false;
        _waveIn.StopRecording();
        Console.WriteLine("⏹️ Stopped real-time speech detection");
    }
    
    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (!_isRecording) return;
        
        try
        {
            // Convert byte data to float
            var samples = ConvertBytesToFloats(e.Buffer, e.BytesRecorded);
            
            lock (_bufferLock)
            {
                // Add samples to buffer
                foreach (var sample in samples)
                {
                    _audioBuffer.Enqueue(sample);
                }
                
                // Process complete windows
                ProcessAudioWindows();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing audio data: {ex.Message}");
        }
    }
    
    private void ProcessAudioWindows()
    {
        while (_audioBuffer.Count >= _windowSize)
        {
            // Extract one window of samples
            var window = new float[_windowSize];
            for (int i = 0; i < _windowSize; i++)
            {
                window[i] = _audioBuffer.Dequeue();
            }
            
            // Detect speech using VAD
            ProcessAudioWindow(window);
        }
    }
    
    private void ProcessAudioWindow(float[] window)
    {
        TotalFramesProcessed++;
        
        try
        {
            // Reset VAD state (for real-time processing)
            _vadDetector.Reset();
            
            // Detect voice activity
            float speechProbability = _vadDetector._model.Call(new[] { window }, _sampleRate)[0];
            
            bool isSpeech = speechProbability >= _threshold;
            
            if (isSpeech)
            {
                SpeechFramesDetected++;
            }
            
            // Trigger voice activity event
            VoiceActivityChanged?.Invoke(this, new VoiceActivityEventArgs
            {
                SpeechProbability = speechProbability,
                IsSpeech = isSpeech,
                Timestamp = DateTime.Now,
                FrameIndex = TotalFramesProcessed,
                AudioData = ConvertFloatsToBytes(window)
            });
            
            // Detect speech start and end
            DetectSpeechSegments(speechProbability, ConvertFloatsToBytes(window));
            
            // Display real-time status
            DisplayRealTimeStatus(speechProbability, isSpeech);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ VAD processing error: {ex.Message}");
        }
    }
    
    private bool _lastSpeechState = false;
    private DateTime _speechStartTime = DateTime.MinValue;

    private readonly List<float> _currentSpeechSegment = [];
    private List<byte> _currentSpeechBytes = [];

    private void DetectSpeechSegments(float speechProbability, byte[] currentWindowBytes)
    {
        bool currentSpeechState = speechProbability >= _threshold;

        // Detect speech start
        if (currentSpeechState && !_lastSpeechState)
        {
            _speechStartTime = DateTime.Now;
            _currentSpeechSegment.Clear();
            _currentSpeechBytes.Clear();

            // Add current window to speech segment
            _currentSpeechBytes.AddRange(currentWindowBytes);

            SpeechDetected?.Invoke(this, new SpeechDetectedEventArgs
            {
                StartTime = _speechStartTime,
                SpeechProbability = speechProbability,
                InitialAudioData = currentWindowBytes
            });
        }
        else if (currentSpeechState)
        {
            // Continue accumulating speech data
            _currentSpeechBytes.AddRange(currentWindowBytes);
        }

        // Detect speech end
        if (!currentSpeechState && _lastSpeechState)
        {
            var speechDuration = DateTime.Now - _speechStartTime;

            // Convert accumulated speech segment to bytes
            byte[] fullSpeechSegmentBytes = _currentSpeechBytes.ToArray();;

            SpeechEnded?.Invoke(this, new SpeechEndedEventArgs
            {
                StartTime = _speechStartTime,
                EndTime = DateTime.Now,
                Duration = speechDuration,
                AverageProbability = speechProbability,
                AudioData = fullSpeechSegmentBytes
            });

            // Clear speech buffers
            _currentSpeechSegment.Clear();
            _currentSpeechBytes.Clear();
        }

        _lastSpeechState = currentSpeechState;
    }

    private void DisplayRealTimeStatus(float speechProbability, bool isSpeech)
    {
        // Update display every 50 frames (approximately 2-3 times per second)
        if (TotalFramesProcessed % 50 == 0)
        {
            Console.WriteLine("🎤 Real-time Speech Detection Status");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"📊 Speech Probability: {speechProbability:F3}");
            Console.WriteLine($"🎯 Current Status: {(isSpeech ? "🔊 Speech" : "🔇 Silence")}");
            Console.WriteLine($"📈 Speech Activity Ratio: {SpeechActivityRatio:P1}");
            Console.WriteLine($"📊 Total Frames: {TotalFramesProcessed:N0}");
            Console.WriteLine($"🎤 Speech Frames: {SpeechFramesDetected:N0}");
            Console.WriteLine($"⏱️  Runtime: {GetRunningTime()}");
        }
    }
    
    private string GetRunningTime()
    {
        // Assume 20ms per frame
        var totalSeconds = TotalFramesProcessed * 0.02;
        var minutes = (int)(totalSeconds / 60);
        var seconds = (int)(totalSeconds % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }
    
    private float[] ConvertBytesToFloats(byte[] buffer, int bytesRecorded)
    {
        var samples = new float[bytesRecorded / 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = BitConverter.ToInt16(buffer, i * 2);
            samples[i] = sample / 32768f; // Normalize to [-1, 1]
        }
        return samples;
    }

    private byte[] ConvertFloatsToBytes(float[] samples)
    {
        var buffer = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            // Clamp to [-1, 1] range to prevent overflow
            float sample = Math.Max(-1.0f, Math.Min(1.0f, samples[i]));

            // Convert to 16-bit integer
            short sampleInt16 = (short)(sample * 32767f);

            // Convert to bytes
            byte[] sampleBytes = BitConverter.GetBytes(sampleInt16);
            buffer[i * 2] = sampleBytes[0];
            buffer[i * 2 + 1] = sampleBytes[1];
        }
        return buffer;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        StopRecording();
        _waveIn?.Dispose();
        //_vadDetector?.Dispose();
        _isDisposed = true;
    }
}