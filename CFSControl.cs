using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using System.Timers;
using System.Windows.Threading;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace CFS_logger
{
	public delegate void SerialReceivedHandle();
	public class CFSControl
	{
		public SerialPort port;
		public string port_name = "";
		private bool request_disconnection = false;
        private bool closing_port = false;
		public int[] Register = new int[64];

		private SerialReceivedHandle _handle;

		private System.Timers.Timer update_timer;
		private System.Timers.Timer offset_timer;
		private int offset_counter = 0;
		private int offset_count = 100;
		private const int tick_receive = 5;

		private int portNo = 0;
        private char[] SerialNo = new char[9];
        private char Status;
        private double[] Limit = new double[6];
        private double[] Data = new double[6];
        private double Fx, Fy, Fz, Mx, My, Mz;
		private double Fx_offset, Fy_offset, Fz_offset, Mx_offset, My_offset, Mz_offset;

		const byte DLE = 0x10;
		const byte STX = 0x02;
		const byte ETX = 0x03;
		const byte NAK = 0x15;

		public void Init()
        {
			SensorInit();

			update_timer = new System.Timers.Timer(tick_receive);
			update_timer.Elapsed += update;
			update_timer.AutoReset = true;
			update_timer.Enabled = true;
		}

		public void RequestDisconnection()
		{
			request_disconnection = true;
		}

		public void SensorInit()
        {
			GetCFSLimit();
			SetCFSFilterFrequency(0x00);
		}

		public void SensorOffset()
        {
			// ここでupdateを止めてるはずなんだが止まらん???
			update_timer.AutoReset = false;
			update_timer.Stop();

			offset_timer = new System.Timers.Timer(10*tick_receive);
			offset_timer.Elapsed += OffsetSequence;
			offset_timer.AutoReset = true;
			offset_timer.Enabled = true;

			Fx_offset = 0;
			Fy_offset = 0;
			Fz_offset = 0;
			Mx_offset = 0;
			My_offset = 0;
			Mz_offset = 0;
		}

		private void OffsetSequence(Object source, ElapsedEventArgs e)
        {
			if (offset_counter < offset_count)
			{
				GetCFSData();
				Fx_offset += Fx;
				Fy_offset += Fy;
				Fz_offset += Fz;
				Mx_offset += Mx;
				My_offset += My;
				Mz_offset += Mz;
				offset_counter++;
			}
            else
            {
				Fx_offset = Fx_offset / offset_count;
				Fy_offset = Fy_offset / offset_count;
				Fz_offset = Fz_offset / offset_count;
				Mx_offset = Mx_offset / offset_count;
				My_offset = My_offset / offset_count;
				Mz_offset = Mz_offset / offset_count;

				offset_timer.Stop();
				offset_timer.Dispose();

				//update_timer.Elapsed += update;
				//update_timer.AutoReset = true;
				//update_timer.Enabled = true;


				update_timer.AutoReset = true;
				update_timer.Start();
			}
		}

		public void SetDatareceivedHandle(SerialReceivedHandle data_received_handle)
		{
			_handle = data_received_handle;
		}

		private void update(Object source, ElapsedEventArgs e)
		{
			GetCFSData();
			Register[0x00] = (int)((Fx - Fx_offset) * 10000);
			Register[0x01] = (int)((Fy - Fy_offset) * 10000);
			Register[0x02] = (int)((Fz - Fz_offset) * 10000);
			Register[0x03] = (int)((Mx - Mx_offset) * 10000);
			Register[0x04] = (int)((My - My_offset) * 10000);
			Register[0x05] = (int)((Mz - Mz_offset) * 10000);

			if (Application.Current.Dispatcher.CheckAccess())
			{
				_handle();
			}
			else
			{
				Application.Current.Dispatcher.BeginInvoke(
				  DispatcherPriority.Background,
				  new Action(() => {
					  _handle();
				  }));
			}
		}

		private void GetCFSInformation()
		{
			byte[] command = { 0x04, 0xFF, 0x2A, 0x00 };
			byte[] read_buffer = new byte[45];

			Command2CFS(command, read_buffer);

			for (int i = 0; i < read_buffer.Length; i++)
			{
				Console.WriteLine(read_buffer[i]);
			}
		}

		private void GetCFSLimit()
		{
			byte[] command = { 0x04, 0xFF, 0x2B, 0x00 };
			byte[] read_buffer = new byte[32];
			int result = Command2CFS(command, read_buffer);
			/*for (int i = 0; i < read_buffer.Length; i++)
			{
				Console.WriteLine(read_buffer[i]);
			}*/
			if (result == 0)
			{
				Limit[0] = BitConverter.ToSingle(read_buffer, 4);
				Limit[1] = BitConverter.ToSingle(read_buffer, 8);
				Limit[2] = BitConverter.ToSingle(read_buffer, 12);
				Limit[3] = BitConverter.ToSingle(read_buffer, 16);
				Limit[4] = BitConverter.ToSingle(read_buffer, 20);
				Limit[5] = BitConverter.ToSingle(read_buffer, 24);
				Console.WriteLine("LimitFx:{0}, LimitFy:{1}, LimitFz:{2}", Limit[0], Limit[1], Limit[2]);
				Console.WriteLine("LimitMx:{0}, LimitMy:{1}, LimitMz:{2}", Limit[3], Limit[4], Limit[5]);
			}
		}

		private void GetCFSFilterFrequency()
		{
			byte[] command = { 0x04, 0xFF, 0xB6, 0x00 };
			byte[] read_buffer = new byte[14];
			int result = Command2CFS(command, read_buffer);
			if (result == 0)
			{
				int response = read_buffer[4];
				int frequency = 0;
				switch (response)
				{
					case 0x00:
						frequency = 0;
						break;
					case 0x01:
						frequency = 10;
						break;
					case 0x02:
						frequency = 30;
						break;
					case 0x03:
						frequency = 100;
						break;
				}
				Console.WriteLine("Cut off frequency is {0} Hz", frequency);
			}
		}

		// you can chose the 0x00, 0x01, 0x02, 0x03 as frequency OFF, 10Hz, 30Hz, 100Hz
		private void SetCFSFilterFrequency(int frequency)
		{
			byte[] command = { 0x08, 0xFF, 0xA6, 0x00, 0x00, 0x00, 0x00, 0x00 };
			command[4] = (byte)frequency;
			byte[] read_buffer = new byte[10];
			int result = Command2CFS(command, read_buffer);
			for (int i = 0; i < read_buffer.Length; i++)
			{
				Console.WriteLine(read_buffer[i]);
			}
			if (result == 0)
			{
				if (read_buffer[3] == 0x00)
				{
					Console.WriteLine("Successfully set the cutoff frequency. {0}", frequency);
					Console.WriteLine("Turn off the CFS sensor");
				}
				else
				{
					Console.WriteLine("error{0}, Faled to set the cutoff frequency.", read_buffer[3]);
				}
			}
		}

		private void GetCFSData()
		{
			byte[] command = { 0x04, 0xFF, 0x30, 0x00 };
			byte[] read_buffer = new byte[30];
			int result = Command2CFS(command, read_buffer);
			/*for (int i = 0; i < read_buffer.Length; i++)
			{
				Console.WriteLine(read_buffer[i]);
			}*/
			if (result == 0)
			{
				int fx = BitConverter.ToInt16(read_buffer, 4);
				int fy = BitConverter.ToInt16(read_buffer, 6);
				int fz = BitConverter.ToInt16(read_buffer, 8);
				int mx = BitConverter.ToInt16(read_buffer, 10);
				int my = BitConverter.ToInt16(read_buffer, 12);
				int mz = BitConverter.ToInt16(read_buffer, 14);

				Fx = Limit[0] / 10000 * fx;
				Fy = Limit[1] / 10000 * fy;
				Fz = Limit[2] / 10000 * fz;
				Mx = Limit[0] / 10000 * mx;
				My = Limit[1] / 10000 * my;
				Mz = Limit[2] / 10000 * mz;
			}
		}

		private void StartCFSData()
		{
			byte[] command = { 0x04, 0xFF, 0x32, 0x00 };
			byte[] read_buffer = new byte[10];
			int result = Command2CFS(command, read_buffer);
			for (int i = 0; i < read_buffer.Length; i++)
			{
				Console.WriteLine(read_buffer[i]);
			}
		}

		private void StopCFSData()
		{
			byte[] command = { 0x04, 0xFF, 0x33, 0x00 };
			byte[] read_buffer = new byte[10];
			int result = Command2CFS(command, read_buffer);
			for (int i = 0; i < read_buffer.Length; i++)
			{
				Console.WriteLine(read_buffer[i]);
			}
		}

		private void GetCFSDataUntilStop()
		{
			byte[] read_buffer = new byte[22];
			int result = ReadCFS(read_buffer);

			if (result == 0)
			{
				int fx = BitConverter.ToInt16(read_buffer, 4);
				int fy = BitConverter.ToInt16(read_buffer, 6);
				int fz = BitConverter.ToInt16(read_buffer, 8);
				int mx = BitConverter.ToInt16(read_buffer, 10);
				int my = BitConverter.ToInt16(read_buffer, 12);
				int mz = BitConverter.ToInt16(read_buffer, 14);

				Fx = Limit[0] / 10000 * fx;
				Fy = Limit[1] / 10000 * fy;
				Fz = Limit[2] / 10000 * fz;
				Mx = Limit[0] / 10000 * mx;
				My = Limit[1] / 10000 * my;
				Mz = Limit[2] / 10000 * mz;

				Console.WriteLine("Fx:{0}, Fy:{1}, Fz:{2}", Fx, Fy, Fz);
				Console.WriteLine("Mx:{0}, My:{1}, Mz:{2}", Mx, My, Mz);
			}
		}

		private byte CalculateBCC(byte[] command)
		{
			byte BCC = 0;
			for (int i = 0; i < command.Length; i++)
			{
				BCC = (byte)(BCC ^ command[i]);
			}
			BCC = (byte)(BCC ^ ETX);    // ETX 直前の DLE を含まない
			return BCC;
		}

		private int Command2CFS(byte[] command, byte[] read_buffer)
		{
			byte BCC = CalculateBCC(command);
			byte[] START = { DLE, STX };
			byte[] END = { DLE, ETX, BCC };
			byte[] buffer = new byte[START.Length + command.Length + END.Length];
			for (int i = 0; i < START.Length; i++)
			{
				buffer[i] = START[i];
			}
			for (int i = 0; i < command.Length; i++)
			{
				buffer[START.Length + i] = command[i];
			}
			for (int i = 0; i < END.Length; i++)
			{
				buffer[START.Length + command.Length + i] = END[i];
			}

			port.Write(buffer, 0, buffer.Length);
			/*for(int i = 0; i < buffer.Length; i++)
            {
				WriteOneByteData(buffer[i]);
			}*/

			//port.Read(read_buffer, 0, read_buffer.Length);

			int result = ReadCFS(read_buffer);

			// 切断処理
			if (request_disconnection)
			{
				port.DiscardInBuffer();
				port.Close();
				port.Dispose();
				request_disconnection = false;
				closing_port = true;
				update_timer.Stop();
				update_timer.Dispose();
			}

			return result;
		}

		// BCC計算対象範囲まで格納
		private int ReadCFS(byte[] read_buffer)
		{
			int read_byte = port.ReadByte();

			// 最初がDLEでなかったら失敗
			if (read_byte != DLE)
			{
				Console.WriteLine("received {0}, non HEAD byte:{1}", read_byte, DLE);
				return -1;
			}

			else
			{
				read_byte = port.ReadByte();
				if (read_byte == NAK)
				{
					// 否定応答
					Console.WriteLine("received NAK:{0}", NAK);
					return -1;
				}
				else if (read_byte == STX)
				{
					byte BCC = 0;
					int cnt = 0;
					while (true)
					{
						read_byte = (byte)port.ReadByte();
						if (read_byte == DLE)
						{
							read_byte = (byte)port.ReadByte();
							if (read_byte == ETX)
							{
								read_buffer[cnt] = (byte)read_byte;
								BCC = (byte)(BCC ^ read_buffer[cnt]);
								byte read_bcc = (byte)port.ReadByte();
								if (BCC == read_bcc)
								{
									return 0;
								}
								else
								{
									// BCCエラー
									Console.WriteLine("BCC eroor, received {0}, but in the calculations {1}", read_bcc, BCC);
									return -1;
								}
							}
							else if (read_byte == 0x10)
							{
								read_buffer[cnt] = (byte)read_byte;
								BCC = (byte)(BCC ^ read_buffer[cnt]);
								cnt++;
							}
						}
						else
						{
							read_buffer[cnt] = (byte)read_byte;
							BCC = (byte)(BCC ^ read_buffer[cnt]);
							cnt++;
						}
					}
				}
				else
				{
					// ここはもうありえないけど
					Console.WriteLine("non STX, received {0}", read_byte);
					return -1;
				}
			}
		}


	}
}
