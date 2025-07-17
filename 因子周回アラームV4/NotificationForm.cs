using System;
using System.Drawing;
using System.Windows.Forms;
using System.Media; // SystemSounds のために必要
using System.IO;
using static 因子周回アラーム.Form1; // [新] Form1.AlarmModeを参照できるように追加

namespace 因子周回アラーム
{
    public partial class NotificationForm : Form
    {
        private System.Windows.Forms.Timer _blinkTimer;
        private System.Windows.Forms.Timer _alarmTimer;
        private bool _isWhite = false;
        private SoundPlayer _soundPlayer;
        private AlarmMode _currentAlarmMode; // [新] アラームモードを保持するフィールド

        // 手動でコンポーネントを定義
        private Label messageLabel;
        private Button resumeButton; // 周回を再開ボタン
        private Button exitButton;   // 周回を終了ボタン

        // 必要なデザイナー変数 (「components」エラーを解決)
        private System.ComponentModel.IContainer components = null;

        // Form1から渡される情報を受け取るコンストラクタを修正
        public NotificationForm(string message, Point location, Size formSize, bool isTopMostEnabled, AlarmMode alarmMode) // [変更] alarmMode引数を追加
        {
            InitializeComponent(); // ここでデザイナーで定義されたコンポーネントを初期化 (もしあれば)

            this.TopMost = isTopMostEnabled; // 最前面表示設定を適用
            this.StartPosition = FormStartPosition.Manual;
            this.Location = location; // Form1から渡された位置に設定
            this.ControlBox = true;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.Size = formSize; // Form1から渡されたサイズに設定
            this.Text = "因子周回アラーム";
            this.ShowInTaskbar = true; // タスクバーに表示しない

            _currentAlarmMode = alarmMode; // [新] アラームモードを保存

            // メッセージラベルの再設定
            messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.AutoSize = false;
            messageLabel.Font = new Font("Meiryo UI", 12F, FontStyle.Bold);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            messageLabel.Size = new Size(this.ClientSize.Width, 40);
            messageLabel.Location = new Point(0, 10);
            this.Controls.Add(messageLabel);

            // 「周回を再開」ボタンの設置
            resumeButton = new Button();
            resumeButton.Text = "周回を再開";
            resumeButton.Size = new Size(120, 40); // ボタンのサイズを調整
            // ボタンの位置をフォームの下部に配置
            resumeButton.Location = new Point(
                (this.ClientSize.Width / 2) - resumeButton.Width - 10, // 中央から左に寄せる
                this.ClientSize.Height - resumeButton.Height - 10
            );
            resumeButton.Click += ResumeButton_Click;
            resumeButton.BackColor = Color.Yellow; // 背景色を黄色に設定
            this.Controls.Add(resumeButton);

            // 「周回を終了」ボタンの設置
            exitButton = new Button();
            exitButton.Text = "周回を終了";
            exitButton.Size = new Size(120, 40); // ボタンのサイズを調整
            // ボタンの位置をフォームの下部に配置
            exitButton.Location = new Point(
                (this.ClientSize.Width / 2) + 10, // 中央から右に寄せる
                this.ClientSize.Height - exitButton.Height - 10
            );
            exitButton.Click += ExitButton_Click;
            exitButton.BackColor = Color.Yellow; // 背景色を黄色に設定
            this.Controls.Add(exitButton);

            // タイマーのセットアップ
            _blinkTimer = new System.Windows.Forms.Timer();
            _blinkTimer.Interval = 500; // メインウィンドウに合わせて0.5秒ごとに点滅
            _blinkTimer.Tick += BlinkTimer_Tick;
            _blinkTimer.Start();

            _alarmTimer = new System.Windows.Forms.Timer();
            _alarmTimer.Interval = 30000; // 30秒ごとにアラーム音

            // サウンドファイルのロード (適宜パスを調整してください)
            string soundFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sound.wav");
            if (File.Exists(soundFilePath))
            {
                _soundPlayer = new SoundPlayer(soundFilePath);
            }

            // [変更] アラームモードに基づいてタイマーの動作とサウンド再生を制御
            if (_currentAlarmMode != AlarmMode.Silent)
            {
                PlayAlarmSound(); // まずは一度鳴らす (無音モード以外)

                if (_currentAlarmMode == AlarmMode.Continuous)
                {
                    _alarmTimer.Start(); // 連続通知モードの場合はタイマーを開始
                    _alarmTimer.Tick += AlarmTimer_Tick;
                }
            }
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            if (_isWhite)
            {
                this.BackColor = Color.LightSkyBlue;
            }
            else
            {
                this.BackColor = Color.White;
            }
            _isWhite = !_isWhite;
        }

        private void AlarmTimer_Tick(object sender, EventArgs e)
        {
            PlayAlarmSound();
        }

        private void PlayAlarmSound()
        {
            if (_currentAlarmMode == AlarmMode.Silent) // [新] 無音モードの場合は再生しない
            {
                return;
            }

            try
            {
                if (_soundPlayer != null)
                {
                    _soundPlayer.Play();
                }
                else
                {
                    SystemSounds.Exclamation.Play();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"アラーム音の再生エラー: {ex.Message}");
                SystemSounds.Exclamation.Play();
            }
        }

        // 「周回を再開」ボタンクリック時の処理
        private void ResumeButton_Click(object sender, EventArgs e)
        {
            StopTimers();
            this.DialogResult = DialogResult.Yes; // Form1に「監視を再開」を指示
            this.Close();
        }

        // 「周回を終了」ボタンクリック時の処理
        private void ExitButton_Click(object sender, EventArgs e)
        {
            StopTimers();
            this.DialogResult = DialogResult.No; // Form1に「監視を停止」を指示
            this.Close();
        }

        private void StopTimers()
        {
            if (_blinkTimer != null)
            {
                _blinkTimer.Stop();
                _blinkTimer.Dispose();
                _blinkTimer = null; // Dispose後に参照をクリア
            }
            if (_alarmTimer != null)
            {
                _alarmTimer.Stop();
                _alarmTimer.Dispose();
                _alarmTimer = null; // Dispose後に参照をクリア
            }
            if (_soundPlayer != null)
            {
                _soundPlayer.Dispose();
                _soundPlayer = null; // Dispose後に参照をクリア
            }
        }

        // Disposeメソッドをオーバーライドしてリソースを解放
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            StopTimers(); // タイマーを確実に停止・破棄
            base.Dispose(disposing);
        }

        // InitializeComponent()は通常、Designer.csで自動生成されるが、
        // NotificationFormが手動でコンポーネントを定義している場合は、空のメソッドとして残すか、
        // もしくはコンポーネントの初期化をここで行う。
        // 今回のコードではコンストラクタ内で全てのコンポーネントを定義・追加しているため、
        // このメソッドは空でも問題ありません。
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotificationForm));
            this.SuspendLayout();
            //
            // NotificationForm
            //
            this.ClientSize = new System.Drawing.Size(284, 161);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "NotificationForm";
            this.ResumeLayout(false);

        }
    }
}
