using System;
using System.Windows.Controls;
using System.IO.Ports;
using System.Windows.Threading;
using System.ComponentModel;
using System.Collections.Generic;

using CFS_logger;

delegate void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e);

//シリアルポートの接続と切断を管理
static class CFSPortSelector
{
    private static int BAUDRATE = 1000000;//115200;
    private static MainWindow mainWindow;
    private static ComboBox CFSPortComboBox;
    private static Button ConnectButton;
    private static DispatcherTimer _timer;

    private static List<string> Connected_list = new List<string>();

    private static DataReceivedHandler data_received_handle_;

    public static void Init()
    {
        mainWindow = (MainWindow)App.Current.MainWindow;
        CFSPortComboBox = mainWindow.CFSPortComboBox;
        ConnectButton = mainWindow.ConnectButton;
        CFSPortComboBox.SelectedIndex = 0;
        SetTimer();
    }
    public static void SetBaudrate(int baudrate)
    {
        BAUDRATE = baudrate;
    }
    public static bool IsConnected(string port_name)
    {
        return Connected_list.Contains(port_name);
    }

    public static bool IsComboBoxItemConnected()
    {
        return IsConnected((string)CFSPortComboBox.SelectedItem);
    }

    public static void ConnectPort(string port_name, ref CFSControl cfs_control)
    {
        UpdateSerialPortComboBox();
        if (String.IsNullOrEmpty(port_name)) return;
        cfs_control.port = new SerialPort(port_name, BAUDRATE, Parity.None, 8, StopBits.One);
        try
        {
            cfs_control.port.Open();
            cfs_control.port.DtrEnable = true;
            cfs_control.port.RtsEnable = true;
            ConnectButton.Content = "Disconnect";
            Console.WriteLine("Connected.");
            cfs_control.port.DiscardInBuffer();
            //serial_control.SetReceiveInterrupt();
            
            cfs_control.Init();
            Connected_list.Add(port_name);
        }
        catch (Exception err)
        {
            Console.WriteLine("Unexpected exception : {0}", err.ToString());
        }
    }
    public static void DisconnectPort(string port_name, ref CFSControl cfs_control)
    {
        if (IsConnected(port_name))
        {
            cfs_control.RequestDisconnection();
            //cfs_control.Disconnect();
            ConnectButton.Content = "Connect";
            Console.WriteLine("Disconnected.");
            Connected_list.Remove(port_name);
        }
    }

    public static void SetDataReceivedHandle(DataReceivedHandler data_received_handle)
    {
        data_received_handle_ = data_received_handle;
    }

    private static void UpdateSerialPortComboBox()
    {
        // 前に選んでいたポートの取得
        string prev_selected_port = "";
        if (CFSPortComboBox.SelectedItem != null)
            prev_selected_port = CFSPortComboBox.SelectedItem.ToString();

        // ポート一覧の更新
        string[] port_list = SerialPort.GetPortNames();
        CFSPortComboBox.Items.Clear();
        foreach (var i in port_list) CFSPortComboBox.Items.Add(i);

        // 前に選択していたポートを再度選択
        for (int i = 0; i < CFSPortComboBox.Items.Count; i++)
        {
            if (CFSPortComboBox.Items[i].ToString() == prev_selected_port)
                CFSPortComboBox.SelectedIndex = i;
        }
        // ポート数が1以下であれば0番目を選択
        if (CFSPortComboBox.Items.Count <= 1)
            CFSPortComboBox.SelectedIndex = 0;

        if (IsConnected((string)CFSPortComboBox.SelectedItem))
        {
            ConnectButton.Content = "Disconnect";
        }
        else
        {
            ConnectButton.Content = "Connect";
        }
    }
    private static void SetTimer()
    {
        _timer = new DispatcherTimer();
        _timer.Interval = new TimeSpan(0, 0, 1);
        _timer.Tick += new EventHandler(OnTimedEvent);
        _timer.Start();
        mainWindow.Closing += new CancelEventHandler(StopTimer);
    }
    private static void OnTimedEvent(Object source, EventArgs e)
    {
        UpdateSerialPortComboBox();
    }
    private static void StopTimer(object sender, CancelEventArgs e)
    {
        _timer.Stop();
    }
}