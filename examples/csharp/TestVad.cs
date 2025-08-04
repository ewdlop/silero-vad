using System;
using VadDotNet;
using VADdotnet;

namespace VadDotNet;

public class TestVad
{
    private const string MODEL_PATH = "./resources/silero_vad.onnx";
    
    public static void TestModelLoading()
    {
        Console.WriteLine("🧪 測試 Silero VAD 模型加載");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        
        try
        {
            // 檢查模型文件
            if (!File.Exists(MODEL_PATH))
            {
                Console.WriteLine($"❌ 錯誤: 找不到模型文件 '{MODEL_PATH}'");
                Console.WriteLine("請下載 silero_vad.onnx 文件並放到 resources 目錄中");
                return;
            }
            
            Console.WriteLine("✅ 模型文件存在");
            
            // 測試模型加載
            Console.WriteLine("🔄 正在加載模型...");
            using var model = new SileroVadOnnxModel(MODEL_PATH);
            Console.WriteLine("✅ 模型加載成功");
            
            // 測試模型推理
            Console.WriteLine("🔄 正在測試模型推理...");
            
            // 創建測試音頻數據 (靜音)
            var testAudio = new float[512]; // 16kHz 的窗口大小
            for (int i = 0; i < testAudio.Length; i++)
            {
                testAudio[i] = 0.0f; // 靜音
            }
            
            var result = model.Call(new[] { testAudio }, 16000);
            Console.WriteLine($"✅ 模型推理成功，輸出概率: {result[0]:F3}");
            
            // 測試語音檢測器
            Console.WriteLine("🔄 正在測試語音檢測器...");
            var vadDetector = new SileroVadDetector(
                MODEL_PATH, 
                0.5f,    // 閾值
                16000,   // 採樣率
                250,     // 最小語音持續時間
                float.PositiveInfinity,
                100,     // 最小靜音持續時間
                30       // 語音填充時間
            );
            Console.WriteLine("✅ 語音檢測器創建成功");
            
            Console.WriteLine("\n🎉 所有測試通過！系統可以正常使用。");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 測試失敗: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"   內部錯誤: {ex.InnerException.Message}");
            }
        }
    }
    
    public static void TestRealTimeVad()
    {
        Console.WriteLine("🧪 測試實時語音檢測器");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        
        try
        {
            if (!File.Exists(MODEL_PATH))
            {
                Console.WriteLine($"❌ 錯誤: 找不到模型文件 '{MODEL_PATH}'");
                return;
            }
            
            Console.WriteLine("🔄 正在創建實時語音檢測器...");
            using var vadDetector = new SimpleRealTimeVad(
                MODEL_PATH,
                threshold: 0.5f,
                sampleRate: 16000
            );
            Console.WriteLine("✅ 實時語音檢測器創建成功");
            
            // 註冊事件
            vadDetector.SpeechDetected += (sender, e) =>
            {
                Console.WriteLine($"🔊 檢測到語音開始: {e.StartTime:HH:mm:ss.fff}");
            };
            
            vadDetector.SpeechEnded += (sender, e) =>
            {
                Console.WriteLine($"🔇 語音結束: 持續 {e.Duration.TotalSeconds:F2} 秒");
            };
            
            Console.WriteLine("✅ 事件處理器註冊成功");
            Console.WriteLine("\n🎉 實時語音檢測器測試通過！");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 測試失敗: {ex.Message}");
        }
    }
} 