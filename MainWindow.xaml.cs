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

        private void CFSConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CFSPortSelector.IsComboBoxItemConnected())
            {
                DrawSerialGraph.LinkingRegister2Graph(0x00, 0, "Fx", "red");
                DrawSerialGraph.LinkingRegister2Graph(0x01, 0, "Fy", "blue");
                DrawSerialGraph.LinkingRegister2Graph(0x02, 0, "Fz", "green");
                DrawSerialGraph.LinkingRegister2Graph(0x03, 1, "Mx", "red");
                DrawSerialGraph.LinkingRegister2Graph(0x04, 1, "My", "blue");
                DrawSerialGraph.LinkingRegister2Graph(0x05, 1, "Mz", "green");
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
            DrawSerialGraph.ResetFrameCounter();
            DumpDataToExcel.StartLog();
        }

        private void LogStopButton_Click(object sender, RoutedEventArgs e)
        {
            //DumpDataToExcel.SaveDataToExcelFile();
            DumpDataToExcel.SaveDataToTxtFile();
        }

        private void OffseetButton_Click(object sender, RoutedEventArgs e)
        {
            DrawSerialGraph.CFSOffset();
        }
    }
}
