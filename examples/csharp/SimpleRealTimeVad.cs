using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading;
using VADdotnet;

namespace VadDotNet;

public class SimpleRealTimeVad : IDisposable
{
    private readonly SileroVadOnnxModel _model;
    private readonly WaveInEvent _waveIn;
    private readonly WaveOutEvent _waveOut;
    private readonly EchoCancellationWaveProvider _echoCancellationProvider;
    private readonly BufferedWaveProvider _bufferedWaveProvider;
    private readonly int _sampleRate;
    private readonly int _windowSize;
    private readonly float _threshold;
    
    private readonly Queue<float> _audioBuffer;
    private readonly object _bufferLock = new object();
    private bool _isRecording = false;
    private bool _isDisposed = false;
    
    // 語音檢測事件
    public event EventHandler<SpeechDetectedEventArgs> SpeechDetected;
    public event EventHandler<SpeechEndedEventArgs> SpeechEnded;
    public event EventHandler<VoiceActivityEventArgs> VoiceActivityChanged;
    
    // 統計信息
    public int TotalFramesProcessed { get; private set; }
    public int SpeechFramesDetected { get; private set; }
    public double SpeechActivityRatio => TotalFramesProcessed > 0 ? (double)SpeechFramesDetected / TotalFramesProcessed : 0;
    
    public SimpleRealTimeVad(string onnxModelPath, float threshold = 0.5f, int sampleRate = 16000)
    {
        _sampleRate = sampleRate;
        _threshold = threshold;
        _windowSize = sampleRate == 16000 ? 512 : 256;
        
        // 直接初始化 ONNX 模型
        _model = new SileroVadOnnxModel(onnxModelPath);
        
        // 初始化音頻緩衝區
        _audioBuffer = new Queue<float>();
        
        // 初始化 WaveIn
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(sampleRate, 16, 1),
            BufferMilliseconds = 20
        };
        
        _waveIn.DataAvailable += OnDataAvailable;

        _waveOut = new WaveOutEvent
        {
            DesiredLatency = 100 // 設置緩衝區延遲
        };

        _bufferedWaveProvider = new BufferedWaveProvider(_waveIn.WaveFormat)
        {
            ReadFully = true // 確保可以完整讀取緩衝區
        };

        _echoCancellationProvider = new EchoCancellationWaveProvider(20, 200, _bufferedWaveProvider);
        _waveOut.Init(_echoCancellationProvider);
    }
    
    public void Play()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimpleRealTimeVad));
        if (!_isRecording) throw new InvalidOperationException("請先開始錄音才能播放音頻。");
        
        _waveOut.Play();
        Console.WriteLine("🔊 開始播放實時音頻...");
    }

    public void Stop()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimpleRealTimeVad));
        if (!_isRecording) throw new InvalidOperationException("請先開始錄音才能暫停播放。");
        
        _waveOut.Stop();
        Console.WriteLine("⏸️ 暫停實時音頻播放...");
    }

    public void StartRecording()
    {
        if (_isDisposed) throw new ObjectDisposedException(nameof(SimpleRealTimeVad));
        if (_isRecording) return;
        
        _isRecording = true;
        _waveIn.StartRecording();
        Console.WriteLine($"🎤 開始實時語音檢測 (採樣率: {_sampleRate}Hz, 閾值: {_threshold:F2})");
    }
    
    public void StopRecording()
    {
        if (!_isRecording) return;
        
        _isRecording = false;
        _waveIn.StopRecording();
        Console.WriteLine("⏹️ 停止實時語音檢測");
    }
    
    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (!_isRecording) return;
        
        try
        {
            // 將字節數據轉換為浮點數
            var samples = ConvertBytesToFloats(e.Buffer, e.BytesRecorded);
            
            lock (_bufferLock)
            {
                // 將樣本添加到緩衝區
                foreach (var sample in samples)
                {
                    _audioBuffer.Enqueue(sample);
                }
                
                // 處理完整的窗口
                ProcessAudioWindows();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 處理音頻數據時發生錯誤: {ex.Message}");
        }
    }
    
    private void ProcessAudioWindows()
    {
        while (_audioBuffer.Count >= _windowSize)
        {
            // 提取一個窗口的樣本
            var window = new float[_windowSize];
            for (int i = 0; i < _windowSize; i++)
            {
                window[i] = _audioBuffer.Dequeue();
            }
            
            // 使用 VAD 檢測語音
            ProcessAudioWindow(window);
        }
    }
    
    private void ProcessAudioWindow(float[] window)
    {
        TotalFramesProcessed++;
        
        try
        {
            // 檢測語音活動
            float speechProbability = _model.Call(new[] { window }, _sampleRate)[0];
            
            bool isSpeech = speechProbability >= _threshold;
            
            if (isSpeech)
            {
                SpeechFramesDetected++;

                var output = ConvertFloatsToBytes(window);
                var cancelledOutput = new byte[output.Length];
                _echoCancellationProvider.Cancel(output, cancelledOutput);
                _bufferedWaveProvider.AddSamples(cancelledOutput, 0, cancelledOutput.Length);
            }


            // 觸發語音活動事件
            VoiceActivityChanged?.Invoke(this, new VoiceActivityEventArgs
            {
                SpeechProbability = speechProbability,
                IsSpeech = isSpeech,
                Timestamp = DateTime.Now,
                FrameIndex = TotalFramesProcessed
            });
            
            // 檢測語音開始和結束
            DetectSpeechSegments(speechProbability);
            
            // 顯示實時狀態
            DisplayRealTimeStatus(speechProbability, isSpeech);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ VAD 處理錯誤: {ex.Message}");
        }
    }
    
    private bool _lastSpeechState = false;
    private DateTime _speechStartTime = DateTime.MinValue;
    
    private void DetectSpeechSegments(float speechProbability)
    {
        bool currentSpeechState = speechProbability >= _threshold;
        
        // 檢測語音開始
        if (currentSpeechState && !_lastSpeechState)
        {
            _speechStartTime = DateTime.Now;
            SpeechDetected?.Invoke(this, new SpeechDetectedEventArgs
            {
                StartTime = _speechStartTime,
                SpeechProbability = speechProbability
            });
        }
        
        // 檢測語音結束
        if (!currentSpeechState && _lastSpeechState)
        {
            var speechDuration = DateTime.Now - _speechStartTime;
            SpeechEnded?.Invoke(this, new SpeechEndedEventArgs
            {
                StartTime = _speechStartTime,
                EndTime = DateTime.Now,
                Duration = speechDuration,
                AverageProbability = speechProbability
            });
        }
        
        _lastSpeechState = currentSpeechState;
    }
    
    private void DisplayRealTimeStatus(float speechProbability, bool isSpeech)
    {
        // 每 50 幀更新一次顯示（約每秒 2-3 次）
        if (TotalFramesProcessed % 50 == 0)
        {
            Console.Clear();
            Console.WriteLine("🎤 實時語音檢測狀態");
            Console.WriteLine("═══════════════════════════════════════");
            Console.WriteLine($"📊 語音概率: {speechProbability:F3}");
            Console.WriteLine($"🎯 當前狀態: {(isSpeech ? "🔊 語音" : "🔇 靜音")}");
            Console.WriteLine($"📈 語音活動率: {SpeechActivityRatio:P1}");
            Console.WriteLine($"📊 總幀數: {TotalFramesProcessed:N0}");
            Console.WriteLine($"🎤 語音幀數: {SpeechFramesDetected:N0}");
            Console.WriteLine($"⏱️  運行時間: {GetRunningTime()}");
            Console.WriteLine();
            Console.WriteLine("按 Ctrl+C 停止檢測...");
        }
    }
    
    private string GetRunningTime()
    {
        // 假設每幀 20ms
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
            samples[i] = sample / 32768f; // 正規化到 [-1, 1]
        }
        return samples;
    }

    private byte[] ConvertFloatsToBytes(float[] samples)
    {
        var buffer = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short sample = (short)(samples[i] * 32768); // 將浮點數轉換為短整型
            BitConverter.GetBytes(sample).CopyTo(buffer, i * 2);
        }
        return buffer;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        
        StopRecording();
        _waveIn?.Dispose();
        _model?.Dispose();
        _isDisposed = true;
    }
} 