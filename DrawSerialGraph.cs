using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using InteractiveDataDisplay.WPF;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;

using CFS_logger;

public class GraphData
{
    public int register = 0;
    public int[] x = new int[100];
    public int[] y = new int[100];
    public LineGraph linegraph = new LineGraph();
    public SolidColorBrush lg_color = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
    public string description = "";

    public void ShiftData(int new_x, int new_y)
    {
        int size = x.Length;
        for (int i = 0; i < size - 1; i++)
        {
            x[i] = x[i + 1];
            y[i] = y[i + 1];
        }
        x[size - 1] = new_x;
        y[size - 1] = new_y;
    }

    public void PlotGraph()
    {
        if (Application.Current.Dispatcher.CheckAccess())
        {
            linegraph.Plot(x, y);
        }
        else
        {
            int[] tmpx = x;
            int[] tmpy = y;
            LineGraph tmp_lg = linegraph;
            Application.Current.Dispatcher.BeginInvoke(
              DispatcherPriority.Background,
              new Action(() => {
                  tmp_lg.Plot(tmpx, tmpy);
              }));
        }
    }
}

public class Device
{
    public CFSControl cfs_control;// = new SerialPortControl();
    public List<GraphData> graph = new List<GraphData>();
    
    public void Update(int frame_count)
    {
        
        foreach (var graphdata in graph)
        {
            int addr = graphdata.register;
            graphdata.ShiftData(frame_count, cfs_control.Register[addr]);

            // ここの処理はlineが増えていくとどうしようかな
            graphdata.linegraph.PlotOriginX = graphdata.x[0];
            graphdata.linegraph.PlotWidth = 100;

            graphdata.PlotGraph();
        }
    }
}

// シリアルポートからの読み取りとグラフの描画
static class DrawSerialGraph
{
    private static List<Device> device = new List<Device>();
    private static MainWindow mainWindow;
    private static ComboBox SerialPortComboBox;
    private static int frame_count;
    public static void Init()
    {
        mainWindow = (MainWindow)App.Current.MainWindow;
        SerialPortComboBox = mainWindow.CFSPortComboBox;

        device.Add(new Device());
        device.Last().cfs_control = new CFSControl();
    }

    public static void AddSerialDevice(string port_name)
    {
        SerialReceivedHandle data_received_handler = UpdateChartHandler;
        device.Last().cfs_control.SetDatareceivedHandle(data_received_handler);

        CFSPortSelector.ConnectPort(port_name, ref device.Last().cfs_control);

        device.Add(new Device());   // 次に使うやつ
        device.Last().cfs_control = new CFSControl();
    }
    public static void RemoveSerialDevice(string port_name)
    {
        Device tmp = new Device();
        foreach (var d in device)
        {
            if (d.cfs_control.port != null)
                if (d.cfs_control.port.PortName == port_name)
                {
                    CFSPortSelector.DisconnectPort(port_name, ref d.cfs_control);
                    tmp = d;    
                }
        }
        device.Remove(tmp);

        // lineの削除
        foreach (var graph in tmp.graph)
        {
            mainWindow.lines_left.Children.Remove(graph.linegraph);
            mainWindow.lines_right.Children.Remove(graph.linegraph);
        }
    }


    public static void LinkingRegister2Graph(int register, int left_or_right, string description, string color)
    {
        device.Last().graph.Add(new GraphData());
        device.Last().graph.Last().register = register;
        if (left_or_right == 0)
        {
            mainWindow.lines_left.Children.Add(device.Last().graph.Last().linegraph);
        }
        else
        {
            mainWindow.lines_right.Children.Add(device.Last().graph.Last().linegraph);
        }

        SolidColorBrush green = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
        SolidColorBrush red = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        SolidColorBrush blue = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255));

        switch(color)
        {
            case "red":
                device.Last().graph.Last().linegraph.Stroke = red;
                break;
            case "green":
                device.Last().graph.Last().linegraph.Stroke = green;
                break;
            case "blue":
                device.Last().graph.Last().linegraph.Stroke = blue;
                break;
        }
        device.Last().graph.Last().linegraph.Description = String.Format(description);
        device.Last().graph.Last().linegraph.StrokeThickness = 2;
    }

    public static void ResetFrameCounter()
    {
        frame_count = 0;
    }

    public static void UpdateChartHandler()
    {
        frame_count++;

        for (int index = 0; index < device.Count - 1; index++)
        {
            device[index].Update(frame_count);
        }


        if(device[0].graph.Count > 0)
        {
            int[] tmp = new int[7];
            int y_index = device[0].graph[0].y.Length - 1;
            for (int i = 0; i < device[0].graph.Count; i++)
            {
                tmp[i] = device[0].graph[i].y[y_index];
            }
            DumpDataToExcel.DumpDataToSave(frame_count, tmp[0], tmp[1], tmp[2], tmp[3], tmp[4], tmp[5]);
        }
    }

}
