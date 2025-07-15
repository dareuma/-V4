using System;
using System.Drawing;
using System.Windows.Forms;
// using System.Threading.Tasks; // このusingは必要に応じて残すか削除してください

namespace 因子周回アラーム
{
    public partial class Form2 : Form
    {
        private Form1 _parentForm;
        private bool _isTopMostEnabled; // 最前面表示の状態を保持するフィールド
        private bool _isProgrammaticClose = false; // プログラムからのクローズかどうかを示すフラグ

        public Form2(Form1 parentForm, bool isTopMostEnabled)
        {
            InitializeComponent(); // Form2.Designer.cs で定義されているメソッドを呼び出す
            _parentForm = parentForm;
            _isTopMostEnabled = isTopMostEnabled; // 受け取った状態をフィールドに保存
            this.ShowInTaskbar = true;
            this.Load += Form2_Load;
            this.FormClosing += Form2_FormClosing;
            this.LocationChanged += Form2_LocationChanged;

            this.TopMost = _isTopMostEnabled; // Form2のTopMostを設定
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.Opacity = 1.0;
        }

        // Form1から呼び出され、プログラム的にForm2を閉じるためのメソッド
        public void CloseForMonitoringStop()
        {
            _isProgrammaticClose = true; // フラグを立てる
            this.Close(); // フォームを閉じる
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isProgrammaticClose)
            {
                // プログラム的に閉じられた場合は、何もしない（アプリは終了させない）
                return;
            }

            // ユーザーが「×」ボタンで閉じた場合のみアプリケーションを終了させる
            if (e.CloseReason == CloseReason.UserClosing)
            {
                Application.Exit();
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            // 親フォームのStopMonitoring()を呼び出し、親フォーム側で適切に閉じるようにする
            // （親フォームがCloseForMonitoringStop()を呼び出す）
            _parentForm.StopMonitoring();
        }

        private void Form2_LocationChanged(object sender, EventArgs e)
        {
            if (!_parentForm.IsSyncing)
            {
                _parentForm.IsSyncing = true;
                _parentForm.Location = this.Location;
                _parentForm.IsSyncing = false;
            }
        }
    }
}