using System.Windows;
using System.Windows.Input;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Hộp thoại nhập mã máy (dùng cho Thêm máy và Đổi mã máy).
    /// Không ghi DB: chỉ trả về mã mới, việc lưu do nút Lưu của chế độ chỉnh sửa đảm nhận.
    /// </summary>
    public partial class EditMachineDialog : Window
    {
        private readonly string _currentName;
        private readonly Func<string, string?> _validate;

        /// <summary>Mã máy mới (đã Trim) khi DialogResult = true.</summary>
        public string NewName { get; private set; } = "";

        /// <param name="title">Tiêu đề, ví dụ "THÊM MÁY" / "ĐỔI MÃ MÁY".</param>
        /// <param name="positionText">Dòng mô tả vị trí.</param>
        /// <param name="currentName">Mã hiện tại ("" khi thêm mới).</param>
        /// <param name="validate">Trả về null nếu hợp lệ, ngược lại là thông báo lỗi.</param>
        public EditMachineDialog(string title, string positionText, string currentName, Func<string, string?> validate)
        {
            InitializeComponent();
            _currentName = currentName ?? "";
            _validate = validate;

            Title = title;
            TxtTitle.Text = title;
            TxtPosition.Text = positionText;
            TxtNew.Text = _currentName;

            bool isNew = string.IsNullOrEmpty(_currentName);
            PnlOld.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
            TxtOld.Text = _currentName;
            BtnUseThis.ToolTip = $"Điền tên máy đang chạy app: {Environment.MachineName}";

            Loaded += (_, _) => { TxtNew.Focus(); TxtNew.SelectAll(); };
        }

        private void BtnUseThis_Click(object sender, RoutedEventArgs e)
        {
            TxtNew.Text = Environment.MachineName;
            TxtNew.Focus();
        }

        private void TxtNew_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) Confirm();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e) => Confirm();

        private void Confirm()
        {
            string name = TxtNew.Text.Trim();
            if (name == _currentName)
            {
                DialogResult = false; // không đổi gì
                return;
            }

            string? error = _validate(name);
            if (error != null)
            {
                TxtError.Text = "⚠ " + error;
                TxtError.Visibility = Visibility.Visible;
                return;
            }

            NewName = name;
            DialogResult = true;
        }
    }
}
