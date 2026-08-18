using Inspection_Control_App.Model;
using Inspection_Control_App.SQL;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Inspection_Control_App.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MySQL _mySQL;
        private List<POModel> LPoModel;
        private List<CheckStatusModel> checkStatusList;
        private MyUserControl usControl;
        string device = Environment.MachineName;
        private DateTime date_All_PO;
        public MainWindow()
        {
            _mySQL = new MySQL();
            InitializeComponent();
            
            LPoModel = new List<POModel>();
            checkStatusList = new List<CheckStatusModel>();
            usControl = new MyUserControl();
            date_All_PO = DateTime.Now.AddDays(-6);
            
        }
        private async Task LoadAllPO()
        {
            var ListPOAL = await _mySQL.GetAllPOList(device, date_All_PO);
            LPoModel.Clear();
            LPoModel = ListPOAL;
            dgListPO.ItemsSource = LPoModel;
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await loadData();
            _mySQL = new MySQL();
        }

        private async Task loadData()
        {
            //var temp = _mySQL.GetPOModel("520006615033");
            //_ = _mySQL.InsertPOWIP("520006649062","123123");
            int sluongPOALL = 0;
            while (true)
            {
                var ListPOAL = await _mySQL.GetAllPOList(device,date_All_PO);
                if (sluongPOALL != ListPOAL.Count)
                {
                    await LoadAllPO();
                }
                sluongPOALL = ListPOAL.Count;

                //load usrcontrol 
                usControl = await _mySQL.GetPOModel_CHECK(device);
                if (usControl != null)
                {
                    txtInspectionPos.Text = usControl.Ins_Key;
                    txtPoStayCheck.Text = usControl.PO_Check;
                }

                //load list check 
                checkStatusList.Clear();
                checkStatusList = await _mySQL.GetPOByPSTX(txtPoStayCheck.Text,date_All_PO);
                dgCheckStatus.ItemsSource = checkStatusList;

                await Task.Delay(20000);
            }
            
        }

        private async void txtInputWIP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string inputWIP = txtInputWIP.Text.Trim();
                string machine = txtInspectionPos.Text.Trim();
                if (!string.IsNullOrEmpty(inputWIP)&& !string.IsNullOrWhiteSpace(machine))
                {
                    bool check = await _mySQL.InsertPOWIP(inputWIP, machine,device);
                    if (!check) { MessageBox.Show("Đã có lỗi trong quá trình thực hiện"); }
                    txtInputWIP.Clear();

                    await LoadAllPO();
                    await loadData();
                }
            }
        }
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await loadData();
        }

        private void btn_manage_Click(object sender, RoutedEventArgs e)
        {
            var manageScreen = new ManageScreen();
            manageScreen.Owner = this;
            manageScreen.ShowDialog();
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
                        await LoadAllPO();
                        break;
                    case "7 ngày trở lại":
                        date_All_PO = DateTime.Now.AddDays(-6);
                        await LoadAllPO();
                        break;
                    case "30 ngày trở lại":
                        date_All_PO = DateTime.Now.AddDays(-29);
                        await LoadAllPO();
                        break;
                }
            }
        }
    }
}