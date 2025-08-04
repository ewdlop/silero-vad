//using NAudio.Wave;
//using SpeexDSPSharp.Core;
//using System;
//using System.Text;
//using VadDotNet;

//Console.OutputEncoding = System.Text.Encoding.UTF8;

//var format = new WaveFormat(48000, 16, 1);
//var buffer = new BufferedWaveProvider(format) { ReadFully = true };
//var echo = new SimpleEnhancedEchoCancellation(20, 400, buffer);
//var denoiser = new Denoiser(20, format);

//// Performance tracking
//var performanceTracker = new AudioProcessingTracker();

//var recorder = new WaveInEvent()
//{
//WaveFormat = format,
//BufferMilliseconds = 20  // This ensures 1920-byte frames
//};

//var output = new WaveOutEvent()
//{
//DesiredLatency = 100
//};

//recorder.DataAvailable += Recorder_DataAvailable;
//output.Init(echo);

//// Handle Ctrl+C to show final stats
//Console.CancelKeyPress += (sender, e) =>
//{
//    e.Cancel = true;
//    ShowFinalResults();
//Environment.Exit(0);
//};

//Task.Delay(500).Wait();
//output.Play();
//recorder.StartRecording();

//void Recorder_DataAvailable(object? sender, WaveInEventArgs e)
//{
//try
//{
//if (e.BytesRecorded == 1920)
//{
//// Step 1: Original microphone input
//var originalBuffer = new byte[e.BytesRecorded];
//Array.Copy(e.Buffer, originalBuffer, e.BytesRecorded);

//// Step 2: Echo cancellation
//var echoCancelledBuffer = new byte[e.BytesRecorded];
//echo.Cancel(originalBuffer, echoCancelledBuffer);

//// Track echo cancellation effectiveness
//var echoMetrics = performanceTracker.AnalyzeEchoCancellation(originalBuffer, echoCancelledBuffer);

//// Step 3: Noise reduction
//var finalBuffer = new byte[e.BytesRecorded];
//Array.Copy(echoCancelledBuffer, finalBuffer, e.BytesRecorded);
////int voiceActivity = denoiser.Process(finalBuffer);

//// Track noise reduction effectiveness
////var noiseMetrics = performanceTracker.AnalyzeNoiseReduction(echoCancelledBuffer, finalBuffer);

//// Track overall improvement
//var overallMetrics = performanceTracker.AnalyzeOverallImprovement(originalBuffer, finalBuffer);

//// Display real-time results
//Console.Clear();
//Console.WriteLine("=== REAL-TIME AUDIO PROCESSING ANALYSIS ===");
//Console.WriteLine();

//DisplayMetrics("ECHO CANCELLATION", echoMetrics);
////DisplayMetrics("NOISE REDUCTION", noiseMetrics);
//DisplayMetrics("OVERALL PROCESSING", overallMetrics);

////Console.WriteLine($"Voice Activity: {(voiceActivity > 0 ? "DETECTED" : "SILENCE")}");
//Console.WriteLine($"Playback Frames: {echo.AvailablePlaybackFrames} | Buffered: {echo.BufferedPlaybackBytes} bytes");
//Console.WriteLine();

//// Show running averages
//var summary = performanceTracker.GetSummaryStats();
//Console.WriteLine("=== RUNNING AVERAGES ===");
//Console.WriteLine($"Samples Processed: {summary.TotalSamples}");
//Console.WriteLine($"Echo Cancellation: {summary.AvgEchoReduction:F1}dB reduction ({GetEffectivenessRating(summary.AvgEchoReduction)})");
//Console.WriteLine($"Noise Reduction: {summary.AvgNoiseReduction:F1}dB reduction ({GetEffectivenessRating(summary.AvgNoiseReduction)})");
//Console.WriteLine($"Overall SNR Gain: {summary.AvgSNRImprovement:F1}dB ({GetSNRRating(summary.AvgSNRImprovement)})");
//Console.WriteLine();
//Console.WriteLine("Press Ctrl+C to stop and show final results...");

//// Add processed audio to buffer only if there's voice activity or significant processing benefit
//if (overallMetrics.ReductionDb > 2)
//{
//buffer.AddSamples(finalBuffer, 0, finalBuffer.Length);
//}
//else
//{
//// During silence, add original to avoid over-processing artifacts
//buffer.AddSamples(originalBuffer, 0, originalBuffer.Length);
//}
//}
//else
//Console.WriteLine($"Unexpected buffer size: {e.BytesRecorded} bytes (expected 1920)");
//{

//buffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
//}
//}
//catch (Exception ex)
//{
//Console.WriteLine($"Error: {ex.Message}");
//}
//}

//void DisplayMetrics(string stageName, AudioMetrics metrics)
//{
//string symbol = GetQualitySymbol(metrics.ReductionDb, metrics.SNRImprovement);
//Console.WriteLine($"{symbol} {stageName}:");
//Console.WriteLine($"  Signal Level: {metrics.OriginalRMS:F0} → {metrics.ProcessedRMS:F0} RMS");
//Console.WriteLine($"  Noise Floor: {metrics.OriginalNoise:F1} → {metrics.ProcessedNoise:F1} dB");
//Console.WriteLine($"  Reduction: {metrics.ReductionDb:F1}dB ({metrics.ReductionPercent:F1}%)");
//Console.WriteLine($"  SNR: {metrics.OriginalSNR:F1} → {metrics.ProcessedSNR:F1} dB (Δ{metrics.SNRImprovement:+F1}dB)");
//Console.WriteLine();
//}

//string GetQualitySymbol(double reduction, double snrGain)
//{
//if (reduction > 8 && snrGain > 3) return "🟢 EXCELLENT";
//if (reduction > 4 && snrGain > 1) return "🟡 GOOD";
//if (reduction > 2 || snrGain > 0) return "🟠 MODERATE";
//if (reduction > 0) return "🔴 POOR";
//if (reduction < -2) return "❌ HARMFUL";
//return "⚪ UNCHANGED";
//}

//string GetEffectivenessRating(double reduction)
//{
//if (reduction > 10) return "Excellent";
//if (reduction > 6) return "Good";
//if (reduction > 3) return "Moderate";
//if (reduction > 0) return "Poor";
//return "Ineffective";
//}

//string GetSNRRating(double snr)
//{
//if (snr > 6) return "Excellent";
//if (snr > 3) return "Good";
//if (snr > 1) return "Moderate";
//if (snr > 0) return "Poor";
//return "Degraded";
//}

//void ShowFinalResults()
//{
//Console.Clear();
//var summary = performanceTracker.GetSummaryStats();
//var detailed = performanceTracker.GetDetailedAnalysis();

//Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
//Console.WriteLine("║                    FINAL PROCESSING REPORT                  ║");
//Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
//Console.WriteLine();

//Console.WriteLine($"📊 PROCESSING STATISTICS");
//Console.WriteLine($"   Total Samples Processed: {summary.TotalSamples:N0}");
//Console.WriteLine($"   Total Processing Time: {summary.ProcessingTimeSeconds:F1} seconds");
//Console.WriteLine($"   Average Frame Rate: {(summary.TotalSamples / Math.Max(summary.ProcessingTimeSeconds, 1)):F1} fps");
//Console.WriteLine();

//Console.WriteLine($"🎯 ECHO CANCELLATION EFFECTIVENESS");
//Console.WriteLine($"   Average Reduction: {summary.AvgEchoReduction:F2}dB ({GetEffectivenessRating(summary.AvgEchoReduction)})");
//Console.WriteLine($"   Best Performance: {detailed.BestEchoReduction:F1}dB");
//Console.WriteLine($"   Worst Performance: {detailed.WorstEchoReduction:F1}dB");
//Console.WriteLine($"   Consistency: {detailed.EchoReductionStdDev:F1}dB standard deviation");
//Console.WriteLine($"   Success Rate: {detailed.EchoSuccessRate:F1}% (frames with >3dB reduction)");
//Console.WriteLine();

//Console.WriteLine($"🔇 NOISE REDUCTION EFFECTIVENESS");
//Console.WriteLine($"   Average Reduction: {summary.AvgNoiseReduction:F2}dB ({GetEffectivenessRating(summary.AvgNoiseReduction)})");
//Console.WriteLine($"   Best Performance: {detailed.BestNoiseReduction:F1}dB");
//Console.WriteLine($"   Worst Performance: {detailed.WorstNoiseReduction:F1}dB");
//Console.WriteLine($"   Consistency: {detailed.NoiseReductionStdDev:F1}dB standard deviation");
//Console.WriteLine($"   Success Rate: {detailed.NoiseSuccessRate:F1}% (frames with >2dB reduction)");
//Console.WriteLine();

//Console.WriteLine($"📈 OVERALL AUDIO QUALITY");
//Console.WriteLine($"   SNR Improvement: {summary.AvgSNRImprovement:F2}dB ({GetSNRRating(summary.AvgSNRImprovement)})");
//Console.WriteLine($"   Voice Activity Rate: {detailed.VoiceActivityRate:F1}% of samples");
//Console.WriteLine($"   Processing Effectiveness: {detailed.OverallEffectiveness:F1}% improvement");
//Console.WriteLine();

//Console.WriteLine($"💡 RECOMMENDATIONS");
//if (summary.AvgEchoReduction < 3)
//{
//Console.WriteLine($"   ⚠️  Echo cancellation is underperforming. Try:");
//Console.WriteLine($"      • Increase filter length to 600-800ms");
//Console.WriteLine($"      • Use headphones instead of speakers");
//Console.WriteLine($"      • Reduce speaker volume");
//Console.WriteLine($"      • Increase distance between mic and speakers");
//}
//else
//{
//Console.WriteLine($"   ✅ Echo cancellation is working well!");
//}

//if (summary.AvgNoiseReduction < 2)
//{
//Console.WriteLine($"   ⚠️  Noise reduction could be improved. Try:");
//Console.WriteLine($"      • Use a better microphone");
//Console.WriteLine($"      • Reduce background noise sources");
//Console.WriteLine($"      • Increase noise suppression aggressiveness");
//}
//else
//{
//Console.WriteLine($"   ✅ Noise reduction is effective!");
//}

//if (summary.AvgSNRImprovement < 1)
//{
//Console.WriteLine($"   ⚠️  Overall audio quality improvement is minimal");
//Console.WriteLine($"      • Consider disabling processing during silence");
//Console.WriteLine($"      • Fine-tune processing parameters");
//}
//else
//{
//Console.WriteLine($"   ✅ Overall audio processing is beneficial!");
//}
//}

//Console.WriteLine("Enhanced echo cancellation + noise reduction with tracking started...");
//Console.WriteLine("Processing will begin shortly. Watch the real-time metrics!");
//Console.ReadLine();

//recorder.StopRecording();
//output.Stop();
//ShowFinalResults();
//Console.ReadLine();

//// Performance tracking classes
//public class AudioMetrics
//{
//    public double OriginalRMS { get; set; }
//    public double ProcessedRMS { get; set; }
//    public double OriginalNoise { get; set; }
//    public double ProcessedNoise { get; set; }
//    public double ReductionDb { get; set; }
//    public double ReductionPercent { get; set; }
//    public double OriginalSNR { get; set; }
//    public double ProcessedSNR { get; set; }
//    public double SNRImprovement { get; set; }
//}

//public class ProcessingSummary
//{
//    public int TotalSamples { get; set; }
//    public double AvgEchoReduction { get; set; }
//    public double AvgNoiseReduction { get; set; }
//    public double AvgSNRImprovement { get; set; }
//    public double ProcessingTimeSeconds { get; set; }
//}

//public class DetailedAnalysis
//{
//    public double BestEchoReduction { get; set; }
//    public double WorstEchoReduction { get; set; }
//    public double EchoReductionStdDev { get; set; }
//    public double EchoSuccessRate { get; set; }

//    public double BestNoiseReduction { get; set; }
//    public double WorstNoiseReduction { get; set; }
//    public double NoiseReductionStdDev { get; set; }
//    public double NoiseSuccessRate { get; set; }

//    public double VoiceActivityRate { get; set; }
//    public double OverallEffectiveness { get; set; }
//}

//public class AudioProcessingTracker
//{
//    private readonly List<double> _echoReductions = new List<double>();
//    private readonly List<double> _noiseReductions = new List<double>();
//    private readonly List<double> _snrImprovements = new List<double>();
//    private readonly DateTime _startTime = DateTime.Now;
//    private int _voiceActivityCount = 0;
//    private int _totalFrames = 0;

//    public AudioMetrics AnalyzeEchoCancellation(byte[] original, byte[] processed)
//    {
//        var metrics = CalculateMetrics(original, processed);
//        _echoReductions.Add(metrics.ReductionDb);
//        return metrics;
//    }

//    public AudioMetrics AnalyzeNoiseReduction(byte[] original, byte[] processed)
//    {
//        var metrics = CalculateMetrics(original, processed);
//        _noiseReductions.Add(metrics.ReductionDb);
//        return metrics;
//    }

//    public AudioMetrics AnalyzeOverallImprovement(byte[] original, byte[] processed)
//    {
//        var metrics = CalculateMetrics(original, processed);
//        _snrImprovements.Add(metrics.SNRImprovement);
//        _totalFrames++;

//        if (metrics.ProcessedRMS > 500) // Voice activity threshold
//        {
//            _voiceActivityCount++;
//        }

//        return metrics;
//    }

//    private AudioMetrics CalculateMetrics(byte[] original, byte[] processed)
//    {
//        var originalSamples = BytesToShorts(original);
//        var processedSamples = BytesToShorts(processed);

//        var originalRMS = CalculateRMS(originalSamples);
//        var processedRMS = CalculateRMS(processedSamples);

//        // Estimate noise floor (20th percentile)
//        var originalNoise = EstimateNoiseFloor(originalSamples);
//        var processedNoise = EstimateNoiseFloor(processedSamples);

//        var originalNoiseDb = 20 * Math.Log10(Math.Max(originalNoise, 1.0));
//        var processedNoiseDb = 20 * Math.Log10(Math.Max(processedNoise, 1.0));

//        var originalSNR = 20 * Math.Log10(Math.Max(originalRMS, 1.0) / Math.Max(originalNoise, 1.0));
//        var processedSNR = 20 * Math.Log10(Math.Max(processedRMS, 1.0) / Math.Max(processedNoise, 1.0));

//        return new AudioMetrics
//        {
//            OriginalRMS = originalRMS,
//            ProcessedRMS = processedRMS,
//            OriginalNoise = originalNoiseDb,
//            ProcessedNoise = processedNoiseDb,
//            ReductionDb = originalNoiseDb - processedNoiseDb,
//            ReductionPercent = (1.0 - (processedNoise / Math.Max(originalNoise, 1.0))) * 100,
//            OriginalSNR = originalSNR,
//            ProcessedSNR = processedSNR,
//            SNRImprovement = processedSNR - originalSNR
//        };
//    }

//    private short[] BytesToShorts(byte[] bytes)
//    {
//        var shorts = new short[bytes.Length / 2];
//        for (int i = 0; i < shorts.Length; i++)
//        {
//            shorts[i] = BitConverter.ToInt16(bytes, i * 2);
//        }
//        return shorts;
//    }

//    private double CalculateRMS(short[] samples)
//    {
//        if (samples.Length == 0) return 0;
//        double sum = samples.Sum(s => (double)s * s);
//        return Math.Sqrt(sum / samples.Length);
//    }

//    private double EstimateNoiseFloor(short[] samples)
//    {
//        var sorted = samples.Select(s => Math.Abs(s)).OrderBy(s => s).ToArray();
//        int index = (int)(sorted.Length * 0.2);
//        return sorted[Math.Min(index, sorted.Length - 1)];
//    }

//    public ProcessingSummary GetSummaryStats()
//    {
//        return new ProcessingSummary
//        {
//            TotalSamples = _totalFrames,
//            AvgEchoReduction = _echoReductions.Count > 0 ? _echoReductions.Average() : 0,
//            AvgNoiseReduction = _noiseReductions.Count > 0 ? _noiseReductions.Average() : 0,
//            AvgSNRImprovement = _snrImprovements.Count > 0 ? _snrImprovements.Average() : 0,
//            ProcessingTimeSeconds = (DateTime.Now - _startTime).TotalSeconds
//        };
//    }

//    public DetailedAnalysis GetDetailedAnalysis()
//    {
//        return new DetailedAnalysis
//        {
//            BestEchoReduction = _echoReductions.Count > 0 ? _echoReductions.Max() : 0,
//            WorstEchoReduction = _echoReductions.Count > 0 ? _echoReductions.Min() : 0,
//            EchoReductionStdDev = CalculateStdDev(_echoReductions),
//            EchoSuccessRate = _echoReductions.Count > 0 ? _echoReductions.Count(r => r > 3) * 100.0 / _echoReductions.Count : 0,

//            BestNoiseReduction = _noiseReductions.Count > 0 ? _noiseReductions.Max() : 0,
//            WorstNoiseReduction = _noiseReductions.Count > 0 ? _noiseReductions.Min() : 0,
//            NoiseReductionStdDev = CalculateStdDev(_noiseReductions),
//            NoiseSuccessRate = _noiseReductions.Count > 0 ? _noiseReductions.Count(r => r > 2) * 100.0 / _noiseReductions.Count : 0,

//            VoiceActivityRate = _totalFrames > 0 ? _voiceActivityCount * 100.0 / _totalFrames : 0,
//            OverallEffectiveness = _snrImprovements.Count > 0 ? _snrImprovements.Count(s => s > 1) * 100.0 / _snrImprovements.Count : 0
//        };
//    }

//    private double CalculateStdDev(List<double> values)
//    {
//        if (values.Count < 2) return 0;
//        double avg = values.Average();
//        double sumSquaredDiffs = values.Sum(v => Math.Pow(v - avg, 2));
//        return Math.Sqrt(sumSquaredDiffs / (values.Count - 1));
//    }
//}

//public class Denoiser
//{
//    private SpeexDSPPreprocessor _preprocessor;

//    public Denoiser(int frameSizeMs, WaveFormat waveFormat)
//    {
//        int enable = 1;
//        float agcLevel = 8000f;
//        int frameSize = frameSizeMs * waveFormat.SampleRate / 1000;

//        _preprocessor = new SpeexDSPPreprocessor(frameSize, waveFormat.SampleRate);

//        int noiseSuppress = -50;
//        int echoSuppress = -40;
//        int echoProbStart = 35;
//        int echoProbContinue = 20;

//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref enable);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_NOISE_SUPPRESS, ref noiseSuppress);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_ECHO_SUPPRESS, ref echoSuppress);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_ECHO_SUPPRESS_ACTIVE, ref echoSuppress);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_PROB_START, ref echoProbStart);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_PROB_CONTINUE, ref echoProbContinue);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref enable);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_LEVEL, ref agcLevel);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref enable);
//        _preprocessor.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref enable);
//    }

//    public int Process(byte[] bytes)
//    {
//        return _preprocessor.Run(bytes);
//    }

//    public void Dispose()
//    {
//        _preprocessor?.Dispose();
//    }
//}

//class Program2
//{
//    private const string MODEL_PATH = "./resources/silero_vad.onnx";
//    private const string EXAMPLE_WAV_FILE = "./resources/example.wav";
//    private const int SAMPLE_RATE = 16000;
//    private const float THRESHOLD = 0.5f;
//    private const int MIN_SPEECH_DURATION_MS = 250;
//    private const float MAX_SPEECH_DURATION_SECONDS = float.PositiveInfinity;
//    private const int MIN_SILENCE_DURATION_MS = 100;
//    private const int SPEECH_PAD_MS = 30;

//    public static void Test(string[] args)
//    {
        
//            var vadDetector = new SileroVadDetector(MODEL_PATH, THRESHOLD, SAMPLE_RATE,
//                MIN_SPEECH_DURATION_MS, MAX_SPEECH_DURATION_SECONDS, MIN_SILENCE_DURATION_MS, SPEECH_PAD_MS);
//            List<SileroSpeechSegment> speechTimeList = vadDetector.GetSpeechSegmentList(new FileInfo(EXAMPLE_WAV_FILE));
//            //Console.WriteLine(speechTimeList.ToJson());
//            StringBuilder sb = new StringBuilder();
//            foreach (var speechSegment in speechTimeList)
//            {
//                sb.Append($"start second: {speechSegment.StartSecond}, end second: {speechSegment.EndSecond}\n");
                
//            }
//            Console.WriteLine(sb.ToString());
       
//    }


//}