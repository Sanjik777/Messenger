using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

namespace ClientWPF
{
	/*
	 * Простой многопользовательский мессенджер.
Доделать работу в классе - клиент-серверное приложение для отправки и получения
сообщений между клиентами-участниками конференции соединившимися с сервером.
1) Реализовать механизм рассылки сообщений всем участниками сеанса связи:
    один отправляет сообщение - все получают это сообщение.
2) Реализовать механизм отправки сообщения только одному выбранному участнику конференции:
    отправляется сообщение только выбранному, указаному пользователю.
3) Продумать механизм в протоколе обмена данными между клиентом и сервером для получения
    списка пользователей - клиентов участников конференции.

A) Придумать алгоритм однократного запуска приложения сервер - вторую копию запустить нельзя;
или
B) Разрешить множественный запуск приложения сервер, но запретить старт
 двух серверов с одинаковыми IP-адресом и портом.
	 */
	public partial class MainWindow : Window
	{
		TcpClient clientSocket;
		Thread threadClient;
		string sendInfo;
		string nick;
		bool isActive;

		public MainWindow()
		{
			InitializeComponent();
		}
		private void ConnectButtonClick(object sender, RoutedEventArgs e)
		{
			if (isActive==false)
			{
				clientSocket = new TcpClient();
			
				try
				{
					clientSocket.Connect(ipServerTextBox.Text, int.Parse(portTextBox.Text));
					if (clientSocket.Client.Connected)
					{
						isActive = true;
						threadClient = new Thread(new ThreadStart(StartTransmission));
						threadClient.IsBackground = true;
						threadClient.Start();
					}
					else { MessageBox.Show("нет сигнала"); }
				}
				catch (Exception exception)
				{
					MessageBox.Show("Error: " + exception);
				}
			}
			else
			{
				MessageBox.Show("Вы уже подключены");
			}	
		}
		private void DisonnectButtonClick(object sender, RoutedEventArgs e)
		{			
			if (isActive==false)
			{
				MessageBox.Show("Вы еще не подключились к серверу!");
			}
			else
			{
				isActive = false;
				clientSocket.Close();
				MessageBox.Show("Сервер отключен");
			}
		}
		void NickDispatcher()
		{
			Dispatcher.Invoke(new Action(
				() =>
				{
					nick=nickTextBox.Text;
				}
				));
		}
		void ChatDispatcher(string text)
		{
			Dispatcher.Invoke(new Action(
				() =>
				{
					messageReceiveTextBox.Text += text +"\n";
				}
				));
		}
		void StartTransmission()
		{
			try
			{
				byte[] recBuf = new byte[4 * 1024];//4 kb
				NickDispatcher();
				clientSocket.Client.Send(Encoding.UTF8.GetBytes(nick));
				int recSize = clientSocket.Client.Receive(recBuf);
				string greeting = Encoding.UTF8.GetString(recBuf, 0, recSize);
				MessageBox.Show(greeting);
				ChatDispatcher(greeting);

				while (true)
				{
					if (recSize<=0)
					{
						MessageBox.Show("Связь разорвана");
						isActive = false;
						clientSocket.Client.Shutdown(SocketShutdown.Both);
						clientSocket.Close();
						break;
					}
					recSize = clientSocket.Client.Receive(recBuf);
					string receiveMessage = Encoding.UTF8.GetString(recBuf, 0, recSize);
					ChatDispatcher(receiveMessage);
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.Message);
			}
		}
		private void SendButtonClick(object sender, RoutedEventArgs e)
		{
			if (isActive == true)
			{
				Thread threadSend = new Thread(new ThreadStart(SendMessage));
				threadSend.IsBackground = true;
				threadSend.Start();
			}
			else { MessageBox.Show("Вы не подключились к серверу!"); }
		}
		void SendMessage()
		{
			try
			{
				Dispatcher.Invoke(new Action(() => sendInfo = nick + ": " + messageSendTextBox.Text));
				clientSocket.Client.Send(Encoding.UTF8.GetBytes(sendInfo));
			}
			catch(Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
			
		}		
	}
}
