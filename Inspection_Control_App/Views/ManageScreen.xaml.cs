using Inspection_Control_App.Model;
using Inspection_Control_App.SQL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Interaction logic for ManageScreen.xaml
    /// </summary>
    public partial class ManageScreen : Window
    {
        private List<CheckStatusModel> checkStatusList;
        private MySQL _mySQL;
        private bool check_nv;
        private DateTime date_All_PO;
        // MSNV quản lý đã xác nhận thành công (ghi vào UpdatedBy khi lưu sơ đồ)
        private string verifiedManagerId = "";
        private bool closeConfirmed;
        public ManageScreen()
        {
            _mySQL = new MySQL();
            InitializeComponent();
            date_All_PO = DateTime.Now.AddDays(-6);
            checkStatusList = new List<CheckStatusModel>();
            machineMap.EditStateChanged += UpdateEditUi;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await machineMap.LoadLayoutAsync(_mySQL);
            machineMap.Highlight(null, Environment.MachineName);
        }

        // ================= Chỉnh sửa sơ đồ máy =================

        private void UpdateEditUi()
        {
            bool verified = !string.IsNullOrEmpty(verifiedManagerId);
            bool editing = machineMap.IsEditing;

            btnEditMap.Visibility = verified && !editing ? Visibility.Visible : Visibility.Collapsed;
            btnEditMap.IsEnabled = machineMap.CanEdit;
            btnEditMap.ToolTip = machineMap.CanEdit
                ? "Kéo thả, thêm, xóa, đổi mã máy"
                : "Sơ đồ đang dùng dữ liệu mặc định. Hãy chạy script SQL/Scripts/MachineLayout.sql trước.";

            pnlEditBar.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            btnSaveMap.IsEnabled = machineMap.HasChanges;
            lblMapChanges.Visibility = editing ? Visibility.Visible : Visibility.Collapsed;
            lblMapChanges.Text = machineMap.HasChanges
                ? $"● Có {machineMap.ChangeCount} thay đổi chưa lưu"
                : "Chưa có thay đổi";
        }

        private void btnEditMap_Click(object sender, RoutedEventArgs e)
        {
            if (!machineMap.BeginEdit())
                MessageBox.Show(this, "Sơ đồ đang dùng dữ liệu mặc định vì chưa có bảng MachineLayout trên DB.\n" +
                    "Hãy chạy script SQL/Scripts/MachineLayout.sql trước.",
                    "Chỉnh sửa sơ đồ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void btnCancelMap_Click(object sender, RoutedEventArgs e)
        {
            if (machineMap.HasChanges &&
                MessageBox.Show(this, "Bỏ tất cả thay đổi chưa lưu?", "Hủy chỉnh sửa",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await machineMap.LoadLayoutAsync(_mySQL); // tải lại từ DB, thoát chỉnh sửa
        }

        private async void btnSaveMap_Click(object sender, RoutedEventArgs e) => await SaveMapAsync();

        /// <summary>Lưu sơ đồ. Trả về true nếu đã lưu (hoặc không có gì để lưu).</summary>
        private async Task<bool> SaveMapAsync()
        {
            if (string.IsNullOrEmpty(verifiedManagerId) || txtID.Text.Trim() != verifiedManagerId)
            {
                MessageBox.Show(this, "Mã Số Quản lý đã thay đổi. Vui lòng bấm XÁC NHẬN lại trước khi lưu.",
                    "Lưu sơ đồ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            btnSaveMap.IsEnabled = false;
            string? error = await machineMap.SaveAsync(_mySQL, verifiedManagerId);
            UpdateEditUi();
            if (error != null)
            {
                MessageBox.Show(this, error, "Lưu sơ đồ", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            machineMap.Highlight(checkStatusList, Environment.MachineName);
            return true;
        }

        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (closeConfirmed || !machineMap.HasChanges) return;

            e.Cancel = true;
            var answer = MessageBox.Show(this, "Sơ đồ máy có thay đổi chưa lưu. Lưu trước khi đóng?",
                "Đóng màn quản lý", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (answer == MessageBoxResult.Cancel) return;
            if (answer == MessageBoxResult.Yes && !await SaveMapAsync()) return;

            closeConfirmed = true;
            Close();
        }

        private async void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            check_nv = await CheckMSNV();
            await ShowTable(check_nv);
        }
        private async Task<bool> CheckMSNV()
        {
            return await _mySQL.GetEmployee(txtID.Text);
        }

        private async Task ShowTable(bool check)
        {
            if (check) {
                ClearIDError();
                checkStatusList.Clear();
                checkStatusList = await _mySQL.GetPOByPSTX(txtPONumber.Text, date_All_PO);
                dgCheckStatus.ItemsSource = checkStatusList;
                machineMap.Highlight(checkStatusList, Environment.MachineName);
                verifiedManagerId = txtID.Text.Trim();
                UpdateEditUi();
            }
            else
            {
                checkStatusList = new List<CheckStatusModel>();
                machineMap.Highlight(null, Environment.MachineName);
                verifiedManagerId = "";
                if (machineMap.IsEditing)
                {
                    if (machineMap.HasChanges)
                        MessageBox.Show(this, "Mã Số Quản lý không hợp lệ. Các thay đổi sơ đồ chưa lưu đã bị bỏ.",
                            "Chỉnh sửa sơ đồ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    machineMap.CancelEdit();
                }
                UpdateEditUi();
                txtID.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555")); // Viền đỏ
                txtID.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2A1818"));  // Nền đỏ nhạt
                lbl_id_manager.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5555"));

                lblIDError.Text = "Vui lòng, Kiểm tra lại msnv";
                lblIDError.Visibility = Visibility.Visible; // Hiện dòng chữ báo lỗi

                // Hiệu ứng Rung (Shake) ô TextBox khi nhập sai
                TranslateTransform tt = new TranslateTransform();
                txtID.RenderTransform = tt;
                DoubleAnimation da = new DoubleAnimation
                {
                    From = -5,
                    To = 5,
                    Duration = TimeSpan.FromMilliseconds(50),
                    AutoReverse = true,
                    RepeatBehavior = new RepeatBehavior(3)
                };
                tt.BeginAnimation(TranslateTransform.XProperty, da);
            }
        }

        private void ClearIDError()
        {
            txtID.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F3F46")); // Viền xám cũ
            txtID.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));  // Nền đen cũ
            lbl_id_manager.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9CDCFE"));  // Nền xanh cũ
            lblIDError.Visibility = Visibility.Collapsed; // Ẩn dòng chữ lỗi
        }

        // Xóa báo lỗi ngay khi người dùng click vào gõ lại
        private void txtID_GotFocus(object sender, RoutedEventArgs e)
        {
            ClearIDError();
        }

        private async void txtID_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                check_nv = await CheckMSNV();
                await ShowTable(check_nv);
            }
        }

        private async void cboFilterDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboFilterDate.SelectedItem is ComboBoxItem selectedItem)
            {
                string filterType = selectedItem.Content.ToString();

                switch (filterType)
                {
                    case "Hôm nay":
                        date_All_PO = DateTime.Now.AddDays(-1);
                        check_nv = await CheckMSNV();
                        await ShowTable(check_nv);
                        break;
                    case "7 ngày trở lại":
                        date_All_PO = DateTime.Now.AddDays(-6);
                        check_nv = await CheckMSNV();
                        await ShowTable(check_nv);
                        break;
                    case "30 ngày trở lại":
                        date_All_PO = DateTime.Now.AddDays(-29);
                        check_nv = await CheckMSNV();
                        await ShowTable(check_nv);
                        break;
                }
            }
        }
    }
}
