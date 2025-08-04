using System;
using VadDotNet;

namespace VadDotNet;

public class SimpleVadExample
{
    private const string MODEL_PATH = "./resources/silero_vad.onnx";
    
    public static void Run()
    {
        Console.WriteLine("🎤 Silero VAD 實時語音檢測器 (簡化版)");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        
        try
        {
            // 檢查模型文件是否存在
            if (!File.Exists(MODEL_PATH))
            {
                Console.WriteLine($"❌ 錯誤: 找不到模型文件 '{MODEL_PATH}'");
                Console.WriteLine("請確保 silero_vad.onnx 文件位於 resources 目錄中");
                Console.WriteLine("您可以從以下地址下載模型文件:");
                Console.WriteLine("https://github.com/snakers4/silero-vad/blob/master/files/silero_vad.onnx");
                return;
            }
            
            // 創建實時語音檢測器
            using var vadDetector = new SimpleRealTimeVad(
                onnxModelPath: MODEL_PATH,
                threshold: 0.5f,      // 語音檢測閾值
                sampleRate: 16000     // 採樣率
            );
            
            // 註冊事件處理器
            vadDetector.SpeechDetected += OnSpeechDetected;
            vadDetector.SpeechEnded += OnSpeechEnded;
            
            // 設置 Ctrl+C 處理
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("\n⏹️ 正在停止語音檢測...");
                vadDetector.StopRecording();
                
                // 顯示最終統計
                Console.WriteLine("\n📊 最終統計:");
                Console.WriteLine($"   總處理幀數: {vadDetector.TotalFramesProcessed:N0}");
                Console.WriteLine($"   語音幀數: {vadDetector.SpeechFramesDetected:N0}");
                Console.WriteLine($"   語音活動率: {vadDetector.SpeechActivityRatio:P1}");
            };
            
            Console.WriteLine("📋 配置信息:");
            Console.WriteLine($"   • 模型路徑: {MODEL_PATH}");
            Console.WriteLine($"   • 採樣率: 16000 Hz");
            Console.WriteLine($"   • 檢測閾值: 0.5");
            Console.WriteLine($"   • 窗口大小: 512 樣本");
            Console.WriteLine();
            Console.WriteLine("🎯 功能說明:");
            Console.WriteLine("   • 實時檢測麥克風輸入的語音活動");
            Console.WriteLine("   • 顯示語音概率和當前狀態");
            Console.WriteLine("   • 統計語音活動率");
            Console.WriteLine("   • 檢測語音開始和結束時間");
            Console.WriteLine();
            Console.WriteLine("按 Enter 開始檢測，按 Ctrl+C 停止...");
            Console.ReadLine();
            
            // 開始實時語音檢測
            vadDetector.StartRecording();
            
            // 保持程序運行
            Console.WriteLine("檢測已開始，請對著麥克風說話...");
            Console.WriteLine("按 Ctrl+C 停止檢測");
            
            // 等待用戶停止
            while (true)
            {
                Thread.Sleep(100);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 發生錯誤: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   內部錯誤: {ex.InnerException.Message}");
            }
        }
    }
    
    private static void OnSpeechDetected(object sender, SpeechDetectedEventArgs e)
    {
        Console.WriteLine($"\n🔊 檢測到語音開始!");
        Console.WriteLine($"   時間: {e.StartTime:HH:mm:ss.fff}");
        Console.WriteLine($"   概率: {e.SpeechProbability:F3}");
    }
    
    private static void OnSpeechEnded(object sender, SpeechEndedEventArgs e)
    {
        Console.WriteLine($"\n🔇 語音結束");
        Console.WriteLine($"   開始時間: {e.StartTime:HH:mm:ss.fff}");
        Console.WriteLine($"   結束時間: {e.EndTime:HH:mm:ss.fff}");
        Console.WriteLine($"   持續時間: {e.Duration.TotalSeconds:F2} 秒");
        Console.WriteLine($"   平均概率: {e.AverageProbability:F3}");
    }
} 