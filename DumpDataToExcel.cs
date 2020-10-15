using Microsoft.Office.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Office.Interop.Excel;
using System.Windows.Controls;
using System.Windows;

using System.IO;


static class DumpDataToExcel
{
    private static System.Timers.Timer timer_SaveData;
    private const int tick_save_data = 5;
    private static int[,] save_data = new int[1000, 8];
    private static int save_data_index = 0;

    private static int s_ = 0;
    private static int t_ = 0;
    private static int u_ = 0;
    private static int v_ = 0;
    private static int w_ = 0;
    private static int x_ = 0;
    private static int y_ = 0;
    private static int z_ = 0;

    public static void StartLog()
    {
        timer_SaveData = new System.Timers.Timer(tick_save_data);
        timer_SaveData.Elapsed += SaveData;
        timer_SaveData.AutoReset = true;
        timer_SaveData.Enabled = true;
    }
    private static void SaveData(Object source, ElapsedEventArgs e)
    {
        if (save_data_index < save_data.GetLength(0) - 1)
        {
            save_data_index++;
            save_data[save_data_index, 0] = s_;
            save_data[save_data_index, 1] = t_;
            save_data[save_data_index, 2] = u_;
            save_data[save_data_index, 3] = v_;
            save_data[save_data_index, 4] = w_;
            save_data[save_data_index, 5] = x_;
            save_data[save_data_index, 6] = y_;
            save_data[save_data_index, 7] = z_;
        }
    }
    public static void SaveData()
    {
        if (save_data_index < save_data.GetLength(0) - 1)
        {
            save_data_index++;
            save_data[save_data_index, 0] = v_;
            save_data[save_data_index, 1] = w_;
            save_data[save_data_index, 2] = x_;
            save_data[save_data_index, 3] = y_;
            save_data[save_data_index, 4] = z_;
        }
    }
    public static void DumpDataToSave(int x, int y)
    {
        x_ = x;
        y_ = y;
    }
    public static void DumpDataToSave(int x, int y, int z)
    {
        x_ = x;
        y_ = y;
        z_ = z;
    }
    public static void DumpDataToSave(int s=0, int t = 0, int u = 0, int v = 0, int w = 0, int x = 0, int y = 0, int z = 0)
    {
        s_ = s;
        t_ = t;
        u_ = u;
        v_ = v;
        w_ = w;
        x_ = x;
        y_ = y;
        z_ = z;
    }

    public static void SaveDataToTxtFile()
    {
        string filePath = System.Environment.CurrentDirectory + DateTime.Now.ToString("MMddHHmmss") + ".csv";
        StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII);
        for(int i = 0; i < save_data_index; i++)
        {
            for(int j = 0; j < save_data.GetLength(1); j++)
            {
                sw.Write(save_data[i, j].ToString() + ", ");
            }
            sw.Write("\r\n");
        }
        sw.Close();
    }
    public static void SaveDataToExcelFile()
    {
        string ExcelBookFileName = System.Environment.CurrentDirectory + DateTime.Now.ToString("MMddHHmmss");

        Microsoft.Office.Interop.Excel.Application ExcelApp
            = new Microsoft.Office.Interop.Excel.Application();
        ExcelApp.Visible = false;
        Workbook wb = ExcelApp.Workbooks.Add();

        Worksheet ws1 = wb.Sheets[1];
        ws1.Select(Type.Missing);

        for (int i = 0; i < save_data_index; i++)
        {
            Range   rgn = ws1.Cells[1 + i, 1];
                    rgn.Value2 = save_data[i, 0];
                    rgn = ws1.Cells[1 + i, 2];
                    rgn.Value2 = save_data[i, 1];
                    rgn = ws1.Cells[1 + i, 3];
                    rgn.Value2 = save_data[i, 2];
                    rgn = ws1.Cells[1 + i, 4];
                    rgn.Value2 = save_data[i, 3];
                    rgn = ws1.Cells[1 + i, 5];
                    rgn.Value2 = save_data[i, 4];
                    rgn = ws1.Cells[1 + i, 5];
                    rgn.Value2 = save_data[i, 5];
                    rgn = ws1.Cells[1 + i, 6];
                    rgn.Value2 = save_data[i, 6];
        }

        wb.SaveAs(ExcelBookFileName);
        wb.Close(false);
        ExcelApp.Quit();

        Console.WriteLine("save file");
    }
}