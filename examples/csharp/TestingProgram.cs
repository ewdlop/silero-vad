using NAudio.Wave;
using VadDotNet;


//partially working code
Console.OutputEncoding = System.Text.Encoding.UTF8;
var format = new WaveFormat(16000, 16, 1);
var buffer = new BufferedWaveProvider(format) { ReadFully = true };
var echo = new EchoCancellationWaveProvider(20, 200, buffer);

var speechRawFile = $"speech_raw_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
var speechEchoFile = $"speech_echo_cancelled_{DateTime.Now:yyyyMMdd_HHmmss}.wav";

var rawWriter = new WaveFileWriter(speechRawFile, format);
var echoWriter = new WaveFileWriter(speechEchoFile, format);

// Initialize VAD detector
var vadDetector = new RealTimeVadDetectorTweaked("resources/silero_vad.onnx", threshold: 0.5f, sampleRate: 16000);

var output = new WaveOutEvent()
{
    DesiredLatency = 100
};

bool isSpeaking = false;

// Subscribe to VAD events
vadDetector.VoiceActivityChanged += (sender, e) =>
{
    try
    {
        if (e.IsSpeech && e.AudioData != null)
        {
            // Speech detected - apply echo cancellation
            var echoCancelledData = new byte[e.AudioData.Length];
            //echo.Cancel(e.AudioData, echoCancelledData);

            // Save both versions
            rawWriter.Write(e.AudioData, 0, e.AudioData.Length);
            echoWriter.Write(echoCancelledData, 0, echoCancelledData.Length);

            // Play echo-cancelled speech
            buffer.AddSamples(e.AudioData, 0, e.AudioData.Length);

            if (!isSpeaking)
            {
                Console.WriteLine($"🎤 Speech started at {e.Timestamp:HH:mm:ss.fff}");
                Console.WriteLine($"   Probability: {e.SpeechProbability:F3}");
                isSpeaking = true;
            }
        }
        else if (e.AudioData != null)
        {
            // No speech - pass silence to output
            var silence = new byte[e.AudioData.Length];
            buffer.AddSamples(silence, 0, silence.Length);

            if (isSpeaking)
            {
                Console.WriteLine($"🔇 Speech ended at {e.Timestamp:HH:mm:ss.fff}");
                isSpeaking = false;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error in VAD event handler: {ex.Message}");
    }
};

vadDetector.SpeechDetected += (sender, e) =>
{
    Console.WriteLine($"\n📢 New speech segment started");
    Console.WriteLine($"   Time: {e.StartTime:HH:mm:ss.fff}");
    Console.WriteLine($"   Initial probability: {e.SpeechProbability:F3}");
};

vadDetector.SpeechEnded += (sender, e) =>
{
    Console.WriteLine($"\n📴 Speech segment completed");
    Console.WriteLine($"   Duration: {e.Duration.TotalSeconds:F2} seconds");
    Console.WriteLine($"   Size: {e.AudioData?.Length ?? 0} bytes");

    // Process the entire speech segment with echo cancellation
    if (e.AudioData != null && e.AudioData.Length > 0)
    {
        // Save individual segment
        SaveSegment(e.AudioData, e.StartTime);
    }
};

output.Init(echo);

Console.WriteLine("Starting VAD-Event-Driven Echo Cancellation...");
Console.WriteLine("Echo cancellation applied only to detected speech.");
Console.WriteLine($"Raw speech: {speechRawFile}");
Console.WriteLine($"Echo-cancelled: {speechEchoFile}");
Console.WriteLine("\nPress Enter to stop.");

Task.Delay(500).Wait();
output.Play();
vadDetector.StartRecording();

Console.ReadLine();

vadDetector.StopRecording();
output.Stop();
rawWriter?.Dispose();
echoWriter?.Dispose();
vadDetector?.Dispose();

Console.WriteLine($"\n✅ Raw speech saved to: {speechRawFile}");
Console.WriteLine($"✅ Echo-cancelled speech saved to: {speechEchoFile}");

static void SaveSegment(byte[] audioData, DateTime timestamp)
{
    string segmentFileName = $"{timestamp:yyyyMMdd_HHmmss_fff}.wav";
    using (var writer = new WaveFileWriter(segmentFileName, new WaveFormat(48000, 16, 1)))
    {
        writer.Write(audioData, 0, audioData.Length);
    }
    Console.WriteLine($"Saved segment: {segmentFileName}");
}