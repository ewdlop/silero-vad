# 🎤 Silero VAD 實時語音檢測系統

基於 Silero VAD (Voice Activity Detection) 模型的實時語音檢測系統，使用 C# 和 NAudio 實現。

## 📋 功能特點

- **實時語音檢測**: 使用麥克風實時檢測語音活動
- **文件語音檢測**: 分析音頻文件中的語音段
- **高精度檢測**: 基於 Silero VAD 神經網絡模型
- **可配置參數**: 支持自定義閾值、採樣率等參數
- **事件驅動**: 提供語音開始/結束事件
- **統計信息**: 實時顯示語音活動率等統計數據

## 🚀 快速開始

### 1. 準備模型文件

下載 Silero VAD 模型文件：
```bash
# 創建 resources 目錄
mkdir resources

# 下載模型文件 (需要手動下載)
# 從 https://github.com/snakers4/silero-vad/blob/master/files/silero_vad.onnx
# 將 silero_vad.onnx 文件放到 resources 目錄中
```

### 2. 編譯和運行

```bash
# 編譯項目
dotnet build

# 運行主程序
dotnet run
```

### 3. 選擇功能

程序啟動後會顯示菜單：
```
🎤 Silero VAD 語音檢測系統
═══════════════════════════════════════

請選擇要運行的功能:
1. 實時語音檢測 (推薦)
2. 文件語音檢測
3. 音頻處理 (回音消除 + 降噪)
4. 退出

請輸入選項 (1-4):
```

## 📖 使用說明

### 實時語音檢測

選擇選項 1 開始實時語音檢測：

1. 程序會檢查模型文件是否存在
2. 顯示配置信息
3. 按 Enter 開始檢測
4. 對著麥克風說話
5. 觀察實時狀態顯示
6. 按 Ctrl+C 停止檢測

**實時顯示信息包括：**
- 語音概率 (0-1)
- 當前狀態 (語音/靜音)
- 語音活動率
- 總幀數和語音幀數
- 運行時間

### 文件語音檢測

選擇選項 2 分析音頻文件：

1. 將要分析的音頻文件命名為 `example.wav`
2. 放到 `resources` 目錄中
3. 程序會分析並顯示語音段信息

**輸出信息包括：**
- 總語音段數
- 每個語音段的開始時間、結束時間和持續時間

## 🔧 配置參數

### 實時語音檢測參數

```csharp
var vadDetector = new SimpleRealTimeVad(
    onnxModelPath: "./resources/silero_vad.onnx",
    threshold: 0.5f,      // 語音檢測閾值 (0-1)
    sampleRate: 16000     // 採樣率 (8000 或 16000)
);
```

### 文件語音檢測參數

```csharp
var vadDetector = new SileroVadDetector(
    onnxModelPath: "./resources/silero_vad.onnx",
    threshold: 0.5f,                    // 語音檢測閾值
    sampleRate: 16000,                  // 採樣率
    minSpeechDurationMs: 250,           // 最小語音持續時間 (ms)
    maxSpeechDurationSeconds: float.PositiveInfinity,  // 最大語音持續時間
    minSilenceDurationMs: 100,          // 最小靜音持續時間 (ms)
    speechPadMs: 30                     // 語音填充時間 (ms)
);
```

## 📊 事件系統

### 語音檢測事件

```csharp
// 語音開始事件
vadDetector.SpeechDetected += (sender, e) =>
{
    Console.WriteLine($"語音開始: {e.StartTime}, 概率: {e.SpeechProbability}");
};

// 語音結束事件
vadDetector.SpeechEnded += (sender, e) =>
{
    Console.WriteLine($"語音結束: 持續 {e.Duration.TotalSeconds:F2} 秒");
};

// 語音活動變化事件
vadDetector.VoiceActivityChanged += (sender, e) =>
{
    Console.WriteLine($"語音活動: {e.IsSpeech}, 概率: {e.SpeechProbability}");
};
```

## 📁 項目結構

```
csharp/
├── MainProgram.cs              # 主程序入口
├── SimpleRealTimeVad.cs        # 簡化實時語音檢測器
├── RealTimeVadDetector.cs      # 完整實時語音檢測器
├── SimpleVadExample.cs         # 實時檢測示例
├── RealTimeVadExample.cs       # 完整示例
├── SileroVadDetector.cs        # 文件語音檢測器
├── SileroVadOnnxModel.cs       # ONNX 模型封裝
├── SileroSpeechSegment.cs      # 語音段數據結構
├── Program.cs                  # 原始音頻處理程序
├── SimpleEnhancedEchoCancellation.cs  # 回音消除
├── resources/
│   ├── silero_vad.onnx         # Silero VAD 模型文件
│   └── example.wav             # 示例音頻文件
└── README.md                   # 說明文檔
```

## 🔍 技術細節

### 音頻處理流程

1. **音頻捕獲**: 使用 NAudio WaveIn 捕獲麥克風輸入
2. **數據轉換**: 將字節數據轉換為浮點數 (-1 到 1)
3. **窗口處理**: 將音頻數據分割成固定大小的窗口
4. **VAD 檢測**: 使用 Silero VAD 模型檢測語音概率
5. **事件觸發**: 根據閾值觸發相應事件
6. **狀態更新**: 更新統計信息和顯示

### 模型要求

- **輸入格式**: 16-bit PCM, 單聲道
- **採樣率**: 8000 Hz 或 16000 Hz
- **窗口大小**: 256 樣本 (8kHz) 或 512 樣本 (16kHz)

## 🛠️ 依賴項

- **NAudio**: 音頻處理
- **Microsoft.ML.OnnxRuntime**: ONNX 模型推理
- **.NET 8.0**: 運行時環境

## 📝 注意事項

1. **模型文件**: 確保 `silero_vad.onnx` 文件在正確位置
2. **麥克風權限**: 確保程序有麥克風訪問權限
3. **音頻格式**: 支持 16-bit PCM 格式
4. **性能**: 實時檢測需要足夠的 CPU 性能

## 🐛 故障排除

### 常見問題

1. **找不到模型文件**
   - 檢查 `resources/silero_vad.onnx` 是否存在
   - 確保文件路徑正確

2. **麥克風無法訪問**
   - 檢查系統麥克風權限
   - 確保麥克風設備正常工作

3. **檢測不準確**
   - 調整 `threshold` 參數
   - 檢查音頻輸入質量
   - 確保環境噪音較低

4. **性能問題**
   - 降低採樣率到 8000 Hz
   - 關閉其他音頻應用程序
   - 檢查 CPU 使用率

## 📄 許可證

本項目基於 MIT 許可證開源。

## 🤝 貢獻

歡迎提交 Issue 和 Pull Request！

## 📚 參考資料

- [Silero VAD GitHub](https://github.com/snakers4/silero-vad)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [ONNX Runtime](https://github.com/microsoft/onnxruntime) 