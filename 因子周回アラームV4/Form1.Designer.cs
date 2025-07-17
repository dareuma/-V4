namespace 因子周回アラーム
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.Timer blinkTimer;
        private System.Windows.Forms.Button exitButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button topMostButton; // 新しいボタンの宣言
        private System.Windows.Forms.Button soundModeButton; // [新] 単通知モード/無音モード/連続通知モード切り替えボタン

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.blinkTimer = new System.Windows.Forms.Timer(this.components);
            this.exitButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.topMostButton = new System.Windows.Forms.Button();
            this.soundModeButton = new System.Windows.Forms.Button(); // [新]
            this.SuspendLayout();
            //
            // timer1
            //
            this.timer1.Interval = 1000;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            //
            // button1
            //
            this.button1.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.button1.Location = new System.Drawing.Point(32, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(224, 32);
            this.button1.TabIndex = 0;
            this.button1.Text = "監視を開始";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // notifyIcon1
            //
            this.notifyIcon1.Text = "notifyIcon1";
            this.notifyIcon1.Visible = true;
            //
            // blinkTimer
            //
            this.blinkTimer.Interval = 500;
            this.blinkTimer.Tick += new System.EventHandler(this.blinkTimer_Tick);
            //
            // exitButton
            //
            this.exitButton.Location = new System.Drawing.Point(208, 90);
            this.exitButton.Name = "exitButton";
            this.exitButton.Size = new System.Drawing.Size(64, 23);
            this.exitButton.TabIndex = 1;
            this.exitButton.Text = "終了";
            this.exitButton.UseVisualStyleBackColor = true;
            this.exitButton.Click += new System.EventHandler(this.exitButton_Click);
            //
            // label1
            //
            this.label1.BackColor = System.Drawing.Color.Black;
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(12, 47);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(260, 20);
            this.label1.TabIndex = 2;
            this.label1.Text = "ウマ娘が他のブラウザで隠れないようにしてください";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // label2
            //
            this.label2.BackColor = System.Drawing.Color.Black;
            this.label2.ForeColor = System.Drawing.Color.Red;
            this.label2.Location = new System.Drawing.Point(12, 67);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(260, 20);
            this.label2.TabIndex = 3;
            this.label2.Text = "ウマ娘のブラウザが小さすぎると正常に機能しません";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // topMostButton
            //
            this.topMostButton.BackColor = System.Drawing.Color.Aqua;
            this.topMostButton.Location = new System.Drawing.Point(14, 90);
            this.topMostButton.Name = "topMostButton";
            this.topMostButton.Size = new System.Drawing.Size(95, 23); // 横幅を小さく
            this.topMostButton.TabIndex = 4;
            this.topMostButton.Text = "最前面に固定";
            this.topMostButton.UseVisualStyleBackColor = false;
            this.topMostButton.Click += new System.EventHandler(this.topMostButton_Click);
            //
            // soundModeButton
            //
            this.soundModeButton.BackColor = System.Drawing.Color.Aqua;
            this.soundModeButton.Location = new System.Drawing.Point(116, 90); // topMostButtonの隣に配置を調整
            this.soundModeButton.Name = "soundModeButton";
            this.soundModeButton.Size = new System.Drawing.Size(90, 23); // 横幅を大きく
            this.soundModeButton.TabIndex = 5; // 新しいTabIndex
            this.soundModeButton.Text = "単通知モード"; // 初期テキスト
            this.soundModeButton.UseVisualStyleBackColor = false;
            this.soundModeButton.Click += new System.EventHandler(this.soundModeButton_Click);
            //
            // Form1
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 124);
            this.Controls.Add(this.soundModeButton); // [新]
            this.Controls.Add(this.topMostButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.exitButton);
            this.Controls.Add(this.button1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "因子周回アラーム";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.LocationChanged += new System.EventHandler(this.Form1_LocationChanged);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
