using Inspection_Control_App.Helper;
using Inspection_Control_App.Model;
using Inspection_Control_App.SQL;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(20);
        private const int MemoryLogEveryTicks = 90; // ~30 phút

        private readonly MySQL _mySQL = new MySQL();
        private readonly string device = Environment.MachineName;
        private readonly DispatcherTimer _timer;

        private List<CheckStatusModel> checkStatusList = new();
        private string _poListSignature = "";

        // Số ngày lọc; mốc thời gian tính lại mỗi lần làm mới (app chạy nhiều ngày không bị trôi mốc)
        private int _filterDays = 6;
        private DateTime FilterFrom => DateTime.Now.AddDays(-_filterDays);

        // Chỉ 1 lần làm mới chạy tại một thời điểm; yêu cầu đến khi đang chạy sẽ được gộp lại
        private bool _refreshing;
        private bool _refreshPending;
        private int _tickCount;

        public MainWindow()
        {
            InitializeComponent();
            _timer = new DispatcherTimer { Interval = RefreshInterval };
            _timer.Tick += async (_, _) =>
            {
                if (++_tickCount % MemoryLogEveryTicks == 0) AppLog.Memory("Định kỳ");
                await RefreshAsync();
            };
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await machineMap.LoadLayoutAsync(_mySQL);
            await RefreshAsync();
            _timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }

        /// <summary>
        /// Làm mới toàn bộ dữ liệu màn chính một lần. Gọi chồng nhau (timer, Refresh, Enter)
        /// sẽ không tạo thêm vòng lặp: lần gọi đến khi đang chạy chỉ đánh dấu chạy lại 1 lần nữa.
        /// </summary>
        private async Task RefreshAsync()
        {
            if (_refreshing) { _refreshPending = true; return; }
            _refreshing = true;
            try
            {
                do
                {
                    _refreshPending = false;
                    await RefreshOnceAsync();
                } while (_refreshPending);
            }
            catch (Exception ex)
            {
                AppLog.Error("Lỗi khi làm mới màn chính", ex);
            }
            finally
            {
                _refreshing = false;
            }
        }

        private async Task RefreshOnceAsync()
        {
            DateTime from = FilterFrom;

            // 1. Danh sách PO của máy này (chỉ gán lại bảng khi dữ liệu đổi)
            await LoadAllPO(from);

            // 2. Bàn + PO đang check của máy này
            var usControl = await _mySQL.GetPOModel_CHECK(device);
            if (usControl != null)
            {
                txtInspectionPos.Text = usControl.Ins_Key;
                txtPoStayCheck.Text = usControl.PO_Check;
            }

            // 3. Các PO cùng type + sơ đồ máy
            checkStatusList = await _mySQL.GetPOByPSTX(txtPoStayCheck.Text, from);
            dgCheckStatus.ItemsSource = checkStatusList;
            await machineMap.LoadLayoutAsync(_mySQL); // chỉ vẽ lại khi sơ đồ trên DB thay đổi
            machineMap.Highlight(checkStatusList, device);
        }

        private async Task LoadAllPO(DateTime from)
        {
            var list = await _mySQL.GetAllPOList(device, from);
            string signature = list.Count + "|" + string.Join(",", list.Select(p => p.PONumber + "@" + p.Time.Ticks));
            if (signature == _poListSignature) return;
            _poListSignature = signature;
            dgListPO.ItemsSource = list;
        }

        private async void txtInputWIP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string inputWIP = txtInputWIP.Text.Trim();
            string machine = txtInspectionPos.Text.Trim();
            if (string.IsNullOrEmpty(inputWIP) || string.IsNullOrWhiteSpace(machine)) return;

            bool check = await _mySQL.InsertPOWIP(inputWIP, machine, device);
            if (!check) { MessageBox.Show("Đã có lỗi trong quá trình thực hiện"); }
            txtInputWIP.Clear();

            await RefreshAsync();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAsync();
        }

        private void btn_manage_Click(object sender, RoutedEventArgs e)
        {
            var manageScreen = new ManageScreen();
            manageScreen.Owner = this;
            manageScreen.ShowDialog();
        }

        private async void cboFilterDate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboFilterDate.SelectedItem is not ComboBoxItem selectedItem) return;

            switch (selectedItem.Content?.ToString())
            {
                case "Hôm nay": _filterDays = 1; break;
                case "7 ngày trở lại": _filterDays = 6; break;
                case "30 ngày trở lại": _filterDays = 29; break;
                default: return;
            }

            // Lúc khởi tạo cửa sổ (InitializeComponent) chưa cần tải
            if (!IsLoaded) return;
            _poListSignature = "";
            await RefreshAsync();
        }
    }
}
