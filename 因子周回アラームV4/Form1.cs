using System;
using System.Drawing;
using System.Windows.Forms;
using Tesseract;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;

namespace 因子周回アラーム
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const string UmaMusumeWindowName = "umamusume";
        private static readonly string[] SkillPtKeywords = { "スキルPt", "スキル" };
        private static readonly string[] TrainingCompleteKeywords = { "育成完了", "完了", "育成", "前成完了" };
        private static readonly string[] FanCountKeywords = { "ファン数", "ファン", "ラァン" };

        private const int MaxMissCount = 10;
        private const int SearchInterval = 2000; // [変更] 2.0秒ごとに監視 (0.5秒から変更)

        // 画面変化検知と許容範囲に関する定数と変数
        private const int NoChangeThresholdSeconds = 10; // 10秒間変化がなかったら通知
        private const double ChangeTolerancePercentage = 0.20; // 20%までの変化は変化していないものと定義
        private DateTime _lastScreenChangeTime;
        private System.Windows.Forms.Timer _noChangeTimer;
        private Bitmap _lastMonitoredRegionScreenshot; // 監視対象領域の最後のスクリーンショット

        private IntPtr _umaMusumeHandle;
        private Form2 _monitoringForm;
        private System.Windows.Forms.Timer _timer1;
        private System.Windows.Forms.Timer _blinkTimer;
        private int _missCount;
        private bool _isRed = false;
        private bool _isSyncing = false;
        private bool _isTopMostEnabled = false;
        private bool _isApplicationExiting = false;

        // [新] アラーム音の鳴動モードを定義する列挙型
        public enum AlarmMode
        {
            Continuous, // 連続通知モード (初期状態)
            Single,     // 単通知モード
            Silent      // 無音モード
        }

        private AlarmMode _currentAlarmMode = AlarmMode.Continuous; // [新] 現在のアラームモード

        public bool IsSyncing
        {
            get => _isSyncing;
            set => _isSyncing = value;
        }

        public Form1()
        {
            InitializeComponent();
            _timer1 = timer1;
            _blinkTimer = blinkTimer;
            _blinkTimer.Interval = 500;
            _blinkTimer.Start();
            notifyIcon1.Visible = true;
            notifyIcon1.Text = "因子周回アラーム";
            this.button1.BackColor = Color.Yellow;

            // 画面変化検知用タイマーの初期化
            _noChangeTimer = new System.Windows.Forms.Timer();
            _noChangeTimer.Interval = 1000; // 1秒ごとにNoChangeTimer_Tickを実行
            _noChangeTimer.Tick += NoChangeTimer_Tick;
            Debug.WriteLine("NoChangeTimerが初期化されました。");

            // [新] soundModeButtonの初期テキストを設定
            UpdateSoundModeButtonText();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("監視を開始ボタンがクリックされました。");
            _blinkTimer.Stop();
            this.Hide();

            _umaMusumeHandle = FindWindow(null, UmaMusumeWindowName);

            if (_umaMusumeHandle == IntPtr.Zero)
            {
                Debug.WriteLine($"ウマ娘のウィンドウが見つかりません。指定されたウィンドウ名: {UmaMusumeWindowName}");
                if (!Application.OpenForms.OfType<NotificationForm>().Any())
                {
                    // [変更] NotificationFormに_currentAlarmModeを渡す
                    var notification = new NotificationForm("ウマ娘のウィンドウが見つかりません。", this.Location, new Size(300, 163), _isTopMostEnabled, _currentAlarmMode);
                    notification.ShowDialog();
                }
                Show();
                return;
            }
            Debug.WriteLine($"ウマ娘のウィンドウハンドルを取得しました: {_umaMusumeHandle}");

            string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
            if (!Directory.Exists(tessdataPath))
            {
                Debug.WriteLine($"tessdataフォルダが見つかりません: {tessdataPath}");
                if (!Application.OpenForms.OfType<NotificationForm>().Any())
                {
                    // [変更] NotificationFormに_currentAlarmModeを渡す
                    var notification = new NotificationForm("tessdataフォルダが見つかりません。\n学習済みデータを正しく配置してください。", this.Location, new Size(300, 163), _isTopMostEnabled, _currentAlarmMode);
                    notification.ShowDialog();
                }
                Show();
                return;
            }
            Debug.WriteLine($"tessdataフォルダが見つかりました: {tessdataPath}");

            StartMonitoring();
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("終了ボタンがクリックされました。");
            _isApplicationExiting = true;
            Application.Exit();
        }

        private void topMostButton_Click(object sender, EventArgs e)
        {
            _isTopMostEnabled = !_isTopMostEnabled;
            topMostButton.Text = _isTopMostEnabled ? "最前面を解除" : "最前面に固定";
            Debug.WriteLine($"最前面表示設定を変更しました: {_isTopMostEnabled}");

            foreach (Form form in Application.OpenForms)
            {
                form.TopMost = _isTopMostEnabled;
            }
        }

        // [新] soundModeButton_Clickイベントハンドラ
        private void soundModeButton_Click(object sender, EventArgs e)
        {
            switch (_currentAlarmMode)
            {
                case AlarmMode.Continuous:
                    _currentAlarmMode = AlarmMode.Single;
                    break;
                case AlarmMode.Single:
                    _currentAlarmMode = AlarmMode.Silent;
                    break;
                case AlarmMode.Silent:
                    _currentAlarmMode = AlarmMode.Continuous;
                    break;
            }
            UpdateSoundModeButtonText();
            Debug.WriteLine($"アラームモードを切り替えました: {_currentAlarmMode}");
        }

        // [新] soundModeButtonのテキストを更新するヘルパーメソッド
        private void UpdateSoundModeButtonText()
        {
            switch (_currentAlarmMode)
            {
                case AlarmMode.Continuous:
                    soundModeButton.Text = "連続通知モード";
                    break;
                case AlarmMode.Single:
                    soundModeButton.Text = "単通知モード";
                    break;
                case AlarmMode.Silent:
                    soundModeButton.Text = "無音モード";
                    break;
            }
        }

        public void StopMonitoring()
        {
            Debug.WriteLine("監視を停止します。");
            _timer1.Stop();
            _noChangeTimer.Stop(); // NoChangeTimerも停止
            Debug.WriteLine("NoChangeTimerが停止されました。");

            if (_monitoringForm != null)
            {
                _monitoringForm.CloseForMonitoringStop();
                _monitoringForm = null;
            }
            _missCount = 0;
            _blinkTimer.Start();

            if (!this.IsDisposed)
            {
                Show();
            }
            // 監視対象領域のスクリーンショットを解放
            if (_lastMonitoredRegionScreenshot != null)
            {
                Debug.WriteLine("_lastMonitoredRegionScreenshotを破棄します。");
                _lastMonitoredRegionScreenshot.Dispose();
                _lastMonitoredRegionScreenshot = null;
            }
        }

        public void StartMonitoring()
        {
            Debug.WriteLine("監視を開始します。");
            this.TopMost = _isTopMostEnabled;
            _timer1.Interval = SearchInterval;
            _timer1.Start();
            _monitoringForm = new Form2(this, _isTopMostEnabled);
            _monitoringForm.StartPosition = FormStartPosition.Manual;
            _monitoringForm.Location = this.Location;
            _monitoringForm.Show();

            // 画面変化時刻の初期化とタイマースタート
            _lastScreenChangeTime = DateTime.Now;
            _noChangeTimer.Start();
            Debug.WriteLine($"初回画面変化時刻を設定し、NoChangeTimerを開始しました: {_lastScreenChangeTime}");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Debug.WriteLine("timer1_Tickが実行されました。");
            try
            {
                RECT windowRect;
                if (!GetWindowRect(_umaMusumeHandle, out windowRect))
                {
                    _missCount++;
                    Debug.WriteLine($"ウマ娘のウィンドウが見つかりません。ミス回数: {_missCount}");
                    if (_missCount >= MaxMissCount)
                    {
                        StopMonitoring();
                        if (!Application.OpenForms.OfType<NotificationForm>().Any())
                        {
                            // [変更] NotificationFormに_currentAlarmModeを渡す
                            var notification = new NotificationForm("ウマ娘のウィンドウが見つかりません。\n監視を終了します。", this.Location, new Size(300, 163), _isTopMostEnabled, _currentAlarmMode);
                            notification.ShowDialog();
                        }
                    }
                    return;
                }

                _missCount = 0;
                int width = windowRect.Right - windowRect.Left;
                int height = windowRect.Bottom - windowRect.Top;
                Debug.WriteLine($"ウマ娘ウィンドウのサイズ: {width}x{height}");

                if (width <= 0 || height <= 0)
                {
                    Debug.WriteLine("ウマ娘ウィンドウのサイズが無効です (0以下)。");
                    return;
                }

                using (Bitmap capturedImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (Graphics graphics = Graphics.FromImage(capturedImage))
                    {
                        graphics.CopyFromScreen(windowRect.Left, windowRect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
                    }
                    Debug.WriteLine("ウマ娘ウィンドウ全体をキャプチャしました。");

                    // ウマ娘ウィンドウの下半分を定義
                    int bottomHalfY = height / 2;
                    int bottomHalfHeight = height - bottomHalfY;
                    if (bottomHalfHeight <= 0) bottomHalfHeight = 1;

                    Rectangle bottomHalfRect = new Rectangle(0, bottomHalfY, width, bottomHalfHeight);
                    Debug.WriteLine($"下半分領域: {bottomHalfRect}");

                    // 下半分の中心から正方形に50%の範囲を参照
                    int smallerDimOfBottomHalf = Math.Min(bottomHalfRect.Width, bottomHalfRect.Height);
                    int monitoredSide = (int)(smallerDimOfBottomHalf * 0.50);

                    // 監視領域のサイズが0になるのを防ぐ
                    if (monitoredSide <= 0) monitoredSide = 1;

                    int centerXOfBottomHalf = bottomHalfRect.X + bottomHalfRect.Width / 2;
                    int centerYOfBottomHalf = bottomHalfRect.Y + bottomHalfRect.Height / 2;

                    // 監視領域のRectを計算
                    Rectangle monitoredRegionRect = new Rectangle(
                        centerXOfBottomHalf - monitoredSide / 2,
                        centerYOfBottomHalf - monitoredSide / 2,
                        monitoredSide,
                        monitoredSide
                    );

                    // 監視領域がキャプチャされた画像の範囲内に収まるように調整
                    monitoredRegionRect.X = Math.Max(0, monitoredRegionRect.X);
                    monitoredRegionRect.Y = Math.Max(0, monitoredRegionRect.Y);
                    monitoredRegionRect.Width = Math.Min(monitoredRegionRect.Width, capturedImage.Width - monitoredRegionRect.X);
                    monitoredRegionRect.Height = Math.Min(monitoredRegionRect.Height, capturedImage.Height - monitoredRegionRect.Y);

                    // 最終的な監視領域のサイズが有効であることを確認
                    if (monitoredRegionRect.Width <= 0 || monitoredRegionRect.Height <= 0)
                    {
                        Debug.WriteLine("監視対象の領域サイズが無効です (0以下)。");
                        return;
                    }

                    Debug.WriteLine($"監視対象領域: {monitoredRegionRect}");

                    // 監視領域の比較 (下半分中心50%の監視)
                    using (Bitmap currentMonitoredRegion = capturedImage.Clone(monitoredRegionRect, capturedImage.PixelFormat))
                    {
                        if (_lastMonitoredRegionScreenshot == null || !AreBitmapsEqual(currentMonitoredRegion, _lastMonitoredRegionScreenshot))
                        {
                            _lastScreenChangeTime = DateTime.Now;
                            Debug.WriteLine($"監視対象領域が変化しました。最終変化時刻を更新: {_lastScreenChangeTime}");
                            if (_lastMonitoredRegionScreenshot != null)
                            {
                                _lastMonitoredRegionScreenshot.Dispose();
                                Debug.WriteLine("古い_lastMonitoredRegionScreenshotを破棄しました。");
                            }
                            _lastMonitoredRegionScreenshot = (Bitmap)currentMonitoredRegion.Clone();
                            Debug.WriteLine("_lastMonitoredRegionScreenshotを更新しました。");
                        }
                        else
                        {
                            Debug.WriteLine("監視対象領域に変化がありませんでした (許容範囲内)。");
                        }
                    }

                    // OCR処理と新しいアラームトリガー
                    Rectangle skillPtRect = new Rectangle((int)(width * 0.160), (int)(height * 0.830), (int)(width * 0.220), (int)(height * 0.045));
                    Rectangle completeRect = new Rectangle((int)(width * 0.585), (int)(height * 0.830), (int)(width * 0.350), (int)(height * 0.040));
                    Rectangle fanCountRect = new Rectangle((int)(width * 0.440), (int)(height * 0.205), (int)(width * 0.400), (int)(height * 0.040));

                    bool foundSkillPt = CheckForKeywordsWithoutPreprocessing(capturedImage, skillPtRect, SkillPtKeywords, "スキルPt");
                    bool foundComplete = CheckForKeywordsWithoutPreprocessing(capturedImage, completeRect, TrainingCompleteKeywords, "育成完了");
                    bool foundFanCount = CheckForKeywordsWithoutPreprocessing(capturedImage, fanCountRect, FanCountKeywords, "ファン数");

                    Debug.WriteLine($"OCR結果: スキルPt={foundSkillPt}, 育成完了={foundComplete}, ファン数={foundFanCount}");

                    // 新しいアラームトリガーロジック
                    string alarmMessage = "";
                    if (foundSkillPt || foundComplete || foundFanCount)
                    {
                        alarmMessage = "周回が終了しました";
                    }

                    if (!string.IsNullOrEmpty(alarmMessage))
                    {
                        Debug.WriteLine($"OCRキーワード検出アラームトリガー: {alarmMessage}");
                        _noChangeTimer.Stop(); // 画面変化なしタイマーを停止
                        _timer1.Stop();       // メインの監視タイマーも停止

                        if (!Application.OpenForms.OfType<NotificationForm>().Any())
                        {
                            NotificationForm notification;
                            Point notificationLocation = (_monitoringForm != null) ? _monitoringForm.Location : this.Location;
                            // [変更] NotificationFormに_currentAlarmModeを渡す
                            notification = new NotificationForm(alarmMessage, notificationLocation, new Size(300, 163), _isTopMostEnabled, _currentAlarmMode);

                            DialogResult result = notification.ShowDialog();

                            if (_monitoringForm != null)
                            {
                                _monitoringForm.CloseForMonitoringStop();
                                _monitoringForm = null;
                            }

                            if (result == DialogResult.Yes) // 「再開」が選択された場合
                            {
                                Debug.WriteLine("OCR検出通知フォームで「再開」が選択されました。監視を再開します。");
                                StartMonitoring();
                            }
                            else if (result == DialogResult.No) // 「終了」が選択された場合
                            {
                                Debug.WriteLine("OCR検出通知フォームで「終了」が選択されました。監視を停止します。");
                                StopMonitoring();
                            }
                            else // result == DialogResult.Cancel (通知ウィンドウの「×」ボタンで閉じられた場合)
                            {
                                Debug.WriteLine("OCR検出通知フォームが閉じられました。アプリケーションを終了します。");
                                Application.Exit();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"timer1_Tickで予期せぬエラーが発生しました: {ex.Message}");
                StopMonitoring();
                if (!Application.OpenForms.OfType<NotificationForm>().Any())
                {
                    // [変更] NotificationFormに_currentAlarmModeを渡す
                    var notification = new NotificationForm("監視中にエラーが発生しました。\n監視を終了します。", this.Location, new Size(300, 163), _isTopMostEnabled, _currentAlarmMode);
                    notification.ShowDialog();
                }
            }
        }

        private bool CheckForKeywordsWithoutPreprocessing(Bitmap image, Rectangle rect, string[] keywords, string filenamePrefix)
        {
            try
            {
                if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0 || rect.X + rect.Width > image.Width || rect.Y + rect.Height > image.Height)
                {
                    Debug.WriteLine($"OCR対象領域が無効、または画像範囲外です: {filenamePrefix}領域. rect: {rect}, Image Size: {image.Size}");
                    return false;
                }

                using (Bitmap croppedImage = image.Clone(rect, image.PixelFormat))
                {
                    // OCRの精度向上のため、画像を拡大する
                    using (Bitmap resizedImage = new Bitmap(croppedImage, croppedImage.Width * 2, croppedImage.Height * 2))
                    {
                        string tessdataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                        if (!Directory.Exists(tessdataPath))
                        {
                            Debug.WriteLine($"tessdataフォルダが見つかりません。OCRをスキップします。パス: {tessdataPath}");
                            return false;
                        }

                        using (var engine = new TesseractEngine(tessdataPath, "jpn", EngineMode.Default))
                        {
                            // ページセグメンテーションモードを単一のテキストブロックとして扱う (PSM_SINGLE_BLOCK = 6)
                            engine.SetVariable("tessedit_pageseg_mode", "6");
                            using (var page = engine.Process(resizedImage))
                            {
                                string recognizedText = page.GetText().Replace(" ", "").Replace("\n", "");
                                Debug.WriteLine($"OCR実行: {filenamePrefix}領域の認識テキスト: \"{recognizedText}\"");

                                foreach (var keyword in keywords)
                                {
                                    if (recognizedText.Contains(keyword))
                                    {
                                        Debug.WriteLine($"キーワード「{keyword}」を検出しました。");
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OCRエラー for {filenamePrefix}: {ex.Message}");
            }
            return false;
        }

        // 2つのBitmapがある程度の変化率内であるかどうかを比較する
        private bool AreBitmapsEqual(Bitmap bmp1, Bitmap bmp2)
        {
            if (bmp1 == null || bmp2 == null) return false;
            if (bmp1.Size != bmp2.Size) return false;

            // BitmapDataの取得
            Rectangle rect = new Rectangle(0, 0, bmp1.Width, bmp1.Height);
            BitmapData bmpData1 = null;
            BitmapData bmpData2 = null;

            try
            {
                bmpData1 = bmp1.LockBits(rect, ImageLockMode.ReadOnly, bmp1.PixelFormat);
                bmpData2 = bmp2.LockBits(rect, ImageLockMode.ReadOnly, bmp2.PixelFormat);

                IntPtr ptr1 = bmpData1.Scan0;
                IntPtr ptr2 = bmpData2.Scan0;

                int bytes = Math.Abs(bmpData1.Stride) * bmp1.Height;
                byte[] rgbValues1 = new byte[bytes];
                byte[] rgbValues2 = new byte[bytes];

                System.Runtime.InteropServices.Marshal.Copy(ptr1, rgbValues1, 0, bytes);
                System.Runtime.InteropServices.Marshal.Copy(ptr2, rgbValues2, 0, bytes);

                int diffBytes = 0;
                for (int i = 0; i < bytes; i++)
                {
                    // RGB各成分で比較。色が完全に一致しない場合に差分としてカウント
                    if (rgbValues1[i] != rgbValues2[i])
                    {
                        diffBytes++;
                    }
                }

                int totalBytes = bytes;
                // 許容する差分バイト数のしきい値 (totalBytesのChangeTolerancePercentage)
                int toleranceBytes = (int)(totalBytes * ChangeTolerancePercentage);

                Debug.WriteLine($"画像比較結果: 異なるバイト数 = {diffBytes}, 許容される異なるバイト数 = {toleranceBytes}");

                return diffBytes <= toleranceBytes; // 差分が許容範囲内なら変化なしとみなす
            }
            finally
            {
                if (bmpData1 != null) bmp1.UnlockBits(bmpData1);
                if (bmpData2 != null) bmp2.UnlockBits(bmpData2);
            }
        }

        private void NoChangeTimer_Tick(object sender, EventArgs e)
        {
            double elapsedSeconds = (DateTime.Now - _lastScreenChangeTime).TotalSeconds;
            Debug.WriteLine($"NoChangeTimer_Tickが実行されました。監視対象領域の変化からの経過時間: {elapsedSeconds}秒");

            if (elapsedSeconds >= NoChangeThresholdSeconds)
            {
                Debug.WriteLine($"監視対象領域に変化がありません。通知を表示します。経過時間: {elapsedSeconds}秒 (閾値: {NoChangeThresholdSeconds}秒)");
                _noChangeTimer.Stop(); // 通知を出すのでタイマーを停止
                _timer1.Stop();       // メインの監視タイマーも停止

                if (!Application.OpenForms.OfType<NotificationForm>().Any())
                {
                    NotificationForm notification;
                    Point notificationLocation = (_monitoringForm != null) ? _monitoringForm.Location : this.Location;
                    // [変更] NotificationFormに_currentAlarmModeを渡す
                    notification = new NotificationForm("周回が停止していませんか？", notificationLocation, new Size(300, 163), _isTopMostEnabled, _currentAlarmMode);

                    DialogResult result = notification.ShowDialog(); // ダイアログを表示し結果を取得

                    if (_monitoringForm != null)
                    {
                        _monitoringForm.CloseForMonitoringStop();
                        _monitoringForm = null;
                    }

                    if (result == DialogResult.Yes) // 「再開」が選択された場合
                    {
                        Debug.WriteLine("通知フォームで「再開」が選択されました。監視を再開します。");
                        StartMonitoring();
                    }
                    else if (result == DialogResult.No) // 「終了」が選択された場合
                    {
                        Debug.WriteLine("通知フォームで「終了」が選択されました。監視を停止します。");
                        StopMonitoring();
                    }
                    else // result == DialogResult.Cancel (通知ウィンドウの「×」ボタンで閉じられた場合)
                    {
                        Debug.WriteLine("通知フォームが閉じられました。アプリケーションを終了します。");
                        Application.Exit();
                    }
                }
            }
            else
            {
                Debug.WriteLine($"監視対象領域に変化あり、または経過時間不足。経過時間: {elapsedSeconds}秒 (閾値: {NoChangeThresholdSeconds}秒)");
            }
        }

        private void blinkTimer_Tick(object sender, EventArgs e)
        {
            if (_isRed)
            {
                BackColor = Color.White;
            }
            else
            {
                BackColor = Color.Orange;
            }
            _isRed = !_isRed;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // exitButton_Clickで設定された明示的な終了フラグがtrueの場合、通常の終了を許可
            if (_isApplicationExiting)
            {
                return;
            }

            // これ以外のあらゆる理由（「×」ボタンクリックなど）でフォームが閉じられようとした場合、
            // アプリケーション全体を終了させる
            Application.Exit();

            // リソースを解放する新しいメソッドを呼び出す
            CleanUpResources();
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            if (_monitoringForm != null && !_isSyncing)
            {
                _isSyncing = true;
                _monitoringForm.Location = this.Location;
                _isSyncing = false;
                Debug.WriteLine($"Form1の場所が変更されました。Form2も移動しました。新しい場所: {this.Location}");
            }
        }

        // 新しいリソース解放メソッド
        private void CleanUpResources()
        {
            // リソースの解放を確実に行う
            if (_lastMonitoredRegionScreenshot != null)
            {
                _lastMonitoredRegionScreenshot.Dispose();
                _lastMonitoredRegionScreenshot = null;
                Debug.WriteLine("CleanUpResources: _lastMonitoredRegionScreenshotを破棄しました。");
            }
            if (_noChangeTimer != null)
            {
                _noChangeTimer.Stop();
                _noChangeTimer.Dispose();
                _noChangeTimer = null;
                Debug.WriteLine("CleanUpResources: _noChangeTimerを破棄しました。");
            }
        }
    }
}
