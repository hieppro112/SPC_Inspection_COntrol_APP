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
        public ManageScreen()
        {
            _mySQL = new MySQL();
            InitializeComponent();
            date_All_PO = DateTime.Now.AddDays(-6);
            checkStatusList = new List<CheckStatusModel>();
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
            }
            else
            {
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
