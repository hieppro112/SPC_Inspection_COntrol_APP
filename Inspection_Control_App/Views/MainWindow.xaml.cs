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
        public MainWindow()
        {
            InitializeComponent();
            _mySQL = new MySQL();
            LPoModel = new List<POModel>();
            checkStatusList = new List<CheckStatusModel>();
            usControl = new MyUserControl();
            
        }

        private async Task loadData()
        {
            //var temp = _mySQL.GetPOModel("520006615033");
            //_ = _mySQL.InsertPOWIP("520006649062","123123");
            while (true)
            {
                LPoModel.Clear();
                LPoModel = await _mySQL.GetAllPOList(device);
                dgListPO.ItemsSource = LPoModel;

                //load list check 
                checkStatusList.Clear();
                checkStatusList = await _mySQL.GetPOByPSTX(txtPoStayCheck.Text);
                dgCheckStatus.ItemsSource = checkStatusList;

                //load usrcontrol 
                
                usControl = await _mySQL.GetPOModel_CHECK(device);
                txtInspectionPos.Text = usControl.Ins_Key;
                txtPoStayCheck.Text = usControl.PO_Check;

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
                    await loadData();
                }
            }
        }

        private void txtInputWIP_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await loadData();
        }

        private void txtPoStayCheck_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private async Task GetCenter(string inspec)
        {
            while (true)
            {

            }
        }


    }
}