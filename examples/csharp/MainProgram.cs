using System;
using VadDotNet;
using System.IO; // Added for File.Exists

namespace VadDotNet;

class MainProgram
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("🎤 Silero VAD 語音檢測系統");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("請選擇要運行的功能:");
        Console.WriteLine("1. 實時語音檢測 (推薦)");
        Console.WriteLine("2. 文件語音檢測");
        Console.WriteLine("3. 音頻處理 (回音消除 + 降噪)");
        Console.WriteLine("4. 系統測試");
        Console.WriteLine("5. 退出");
        Console.WriteLine();
        Console.Write("請輸入選項 (1-5): ");
        
        var choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                Console.Clear();
                SimpleVadExample.Run();
                break;
                
            case "2":
                Console.Clear();
                RunFileVadDetection();
                break;
                
            case "3":
                Console.Clear();
                RunAudioProcessing();
                break;
                
            case "4":
                Console.Clear();
                RunSystemTest();
                break;
                
            case "5":
                Console.WriteLine("再見!");
                return;
                
            default:
                Console.WriteLine("無效選項，請重新運行程序。");
                break;
        }
    }
    
    private static void RunFileVadDetection()
    {
        Console.WriteLine("📁 文件語音檢測");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        
        const string MODEL_PATH = "./resources/silero_vad.onnx";
        const string EXAMPLE_WAV_FILE = "./resources/example.wav";
        
        try
        {
            // 檢查模型文件
            if (!File.Exists(MODEL_PATH))
            {
                Console.WriteLine($"❌ 錯誤: 找不到模型文件 '{MODEL_PATH}'");
                Console.WriteLine("請確保 silero_vad.onnx 文件位於 resources 目錄中");
                return;
            }
            
            // 檢查音頻文件
            if (!File.Exists(EXAMPLE_WAV_FILE))
            {
                Console.WriteLine($"❌ 錯誤: 找不到音頻文件 '{EXAMPLE_WAV_FILE}'");
                Console.WriteLine("請將要檢測的音頻文件命名為 example.wav 並放在 resources 目錄中");
                return;
            }
            
            Console.WriteLine("🔍 正在分析音頻文件...");
            
            var vadDetector = new SileroVadDetector(
                MODEL_PATH, 
                0.5f,    // 閾值
                16000,   // 採樣率
                250,     // 最小語音持續時間 (ms)
                float.PositiveInfinity,  // 最大語音持續時間
                100,     // 最小靜音持續時間 (ms)
                30       // 語音填充時間 (ms)
            );
            
            var speechSegments = vadDetector.GetSpeechSegmentList(new FileInfo(EXAMPLE_WAV_FILE));
            
            Console.WriteLine($"\n📊 檢測結果:");
            Console.WriteLine($"   總語音段數: {speechSegments.Count}");
            Console.WriteLine();
            
            if (speechSegments.Count > 0)
            {
                Console.WriteLine("🗣️ 語音段詳情:");
                for (int i = 0; i < speechSegments.Count; i++)
                {
                    var segment = speechSegments[i];
                    Console.WriteLine($"   {i + 1}. 開始: {segment.StartSecond:F2}s, 結束: {segment.EndSecond:F2}s, 持續: {segment.EndSecond - segment.StartSecond:F2}s");
                }
            }
            else
            {
                Console.WriteLine("🔇 未檢測到語音活動");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 發生錯誤: {ex.Message}");
        }
        
        Console.WriteLine("\n按 Enter 返回主菜單...");
        Console.ReadLine();
    }
    
    private static void RunAudioProcessing()
    {
        Console.WriteLine("🎵 音頻處理 (回音消除 + 降噪)");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("此功能將啟動實時音頻處理，包括:");
        Console.WriteLine("• 回音消除");
        Console.WriteLine("• 降噪處理");
        Console.WriteLine("• 性能追蹤");
        Console.WriteLine();
        Console.WriteLine("按 Enter 開始處理...");
        Console.ReadLine();
        
        // 這裡可以調用原來的音頻處理代碼
        Console.WriteLine("⚠️ 此功能需要額外的依賴項，請參考原來的 Program.cs 文件");
        Console.WriteLine("按 Enter 返回主菜單...");
        Console.ReadLine();
    }
    
    private static void RunSystemTest()
    {
        Console.WriteLine("🧪 系統測試");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("請選擇測試類型:");
        Console.WriteLine("1. 模型加載測試");
        Console.WriteLine("2. 實時檢測器測試");
        Console.WriteLine("3. 返回主菜單");
        Console.WriteLine();
        Console.Write("請輸入選項 (1-3): ");
        
        var choice = Console.ReadLine();
        
        switch (choice)
        {
            case "1":
                Console.Clear();
                TestVad.TestModelLoading();
                break;
                
            case "2":
                Console.Clear();
                TestVad.TestRealTimeVad();
                break;
                
            case "3":
                return;
                
            default:
                Console.WriteLine("無效選項");
                break;
        }
        
        Console.WriteLine("\n按 Enter 返回主菜單...");
        Console.ReadLine();
    }
} 