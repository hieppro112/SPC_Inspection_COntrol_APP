using Inspection_Control_App.Model;
using Inspection_Control_App.SQL;
using System.Windows;
using System.Windows.Input;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Hộp thoại đổi mã máy của 1 ô trên sơ đồ, lưu thẳng vào bảng [KVH].[dbo].[MachineLayout].
    /// </summary>
    public partial class EditMachineDialog : Window
    {
        private readonly MachineSlot _slot;
        private readonly MySQL _mySQL;
        private readonly string _updatedBy;

        public EditMachineDialog(MachineSlot slot, MySQL mySQL, string updatedBy)
        {
            InitializeComponent();
            _slot = slot;
            _mySQL = mySQL;
            _updatedBy = updatedBy;

            TxtPosition.Text = $"Vị trí: hàng {slot.RowNo}, cột {slot.ColNo}"
                + (string.IsNullOrWhiteSpace(slot.IndexTerminal) ? "" : $"  ·  Bàn {slot.IndexTerminal}");
            TxtOld.Text = slot.MachineName;
            TxtNew.Text = slot.MachineName;
            BtnUseThis.ToolTip = $"Điền tên máy đang chạy app: {Environment.MachineName}";

            Loaded += (_, _) => { TxtNew.Focus(); TxtNew.SelectAll(); };
        }

        private void BtnUseThis_Click(object sender, RoutedEventArgs e)
        {
            TxtNew.Text = Environment.MachineName;
            TxtNew.Focus();
        }

        private async void TxtNew_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) await SaveAsync();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            string newName = TxtNew.Text.Trim();

            if (string.IsNullOrEmpty(newName))
            {
                ShowError("Vui lòng nhập mã máy.");
                return;
            }
            if (string.Equals(newName, _slot.MachineName, StringComparison.Ordinal))
            {
                DialogResult = false; // không đổi gì
                return;
            }

            BtnSave.IsEnabled = false;
            string? error = await _mySQL.UpdateMachineName(_slot.Id, newName, _updatedBy);
            BtnSave.IsEnabled = true;

            if (error != null)
            {
                ShowError(error);
                return;
            }
            DialogResult = true;
        }

        private void ShowError(string message)
        {
            TxtError.Text = "⚠ " + message;
            TxtError.Visibility = Visibility.Visible;
        }
    }
}
