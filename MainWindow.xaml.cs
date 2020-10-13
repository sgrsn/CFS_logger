using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ComponentModel;

namespace CFS_logger
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CFSPortSelector.Init();
            DrawSerialGraph.Init();

            this.Closing += new CancelEventHandler(CloseEvent);
        }

        private void CloseEvent(object sender, CancelEventArgs e)
        {
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CFSPortSelector.IsComboBoxItemConnected())
            {
                DrawSerialGraph.LinkingRegister2Graph(0x00, 0);
                DrawSerialGraph.LinkingRegister2Graph(0x01, 0);
                DrawSerialGraph.LinkingRegister2Graph(0x02, 0);
                DrawSerialGraph.AddSerialDevice(CFSPortComboBox.Text);
            }

            else
            {
                DrawSerialGraph.RemoveSerialDevice(CFSPortComboBox.Text);
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void LogStartButton_Click(object sender, RoutedEventArgs e)
        {
            DumpDataToExcel.StartLog();
        }

        private void LogStopButton_Click(object sender, RoutedEventArgs e)
        {
            DumpDataToExcel.SaveDataToExcelFile();
        }
    }
}
