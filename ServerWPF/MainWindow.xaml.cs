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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerWPF
{
	/*
	 Простой многопользовательский мессенджер.
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
		TcpListener sockServer;
		Thread threadServer;
		
		bool isStart;
		List<ClientInfo> listClients;

		public MainWindow()
		{
			InitializeComponent();

			sockServer = null;
			threadServer = null;
			isStart = false;//сервер выключен

			listOfIP.Items.Add("0.0.0.0");
			listOfIP.Items.Add("127.0.0.1");//localhost
			IPHostEntry ipServer = Dns.GetHostEntry(Dns.GetHostName());

			foreach (var a in ipServer.AddressList)
			{
				listOfIP.Items.Add(a.ToString());
			}
			listOfIP.SelectedIndex = 0;
			listClients = new List<ClientInfo>();
		}
		private void StartButtonClick(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!isStart)
				{
					buttonStart.Content = "Stop";
					isStart = true;

					sockServer = new TcpListener(
						IPAddress.Parse(listOfIP.SelectedItem.ToString()),
						int.Parse(portTextBox.Text)
						);
					sockServer.Start(100);

					threadServer = new Thread(ServerThreadProc);
					threadServer.IsBackground = true;
					//threadServer.Start(sockServer);
					threadServer.Start();
				}
				else
				{
					buttonStart.Content = "Start";
					isStart = false;
					sockServer.Stop();
					//sockServer.Server.Shutdown(SocketShutdown.Both);
					sockServer.Server.Close();

				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
			
		}
		//void ServerThreadProc(object obj)
		void ServerThreadProc()
		{
			//TcpListener serverSocket = (TcpListener)obj;
			while (true)
			{

				if (isStart == false)
				{
					break;
				}
				else
				{
					//IAsyncResult iAsyncResult = serverSocket.BeginAcceptTcpClient(AcceptClientProc, serverSocket);
					IAsyncResult iAsyncResult = sockServer.BeginAcceptTcpClient(AcceptClientProc, sockServer);
					iAsyncResult.AsyncWaitHandle.WaitOne();//по очереди впускаем клиентов тлько по одному
				}
			}
		}
		void SaveToLog(string str)
		{
			Dispatcher.Invoke(new Action(
				() => { infoTextBox.AppendText(str); }
				));
		}

		void AcceptClientProc(IAsyncResult iARes)
		{
			//сокет для прослушивания и соединения обработка ассинхроннго события
			if (isStart==false)
			{
				return;
			}
			else
			{
				TcpListener socketServer = (TcpListener)iARes.AsyncState;
				TcpClient client = socketServer.EndAcceptTcpClient(iARes);//сокет для обмена данными
				SaveToLog("Клиент соединился с сервером, адрес - " + client.Client.RemoteEndPoint.ToString() + ", ");
				ThreadPool.QueueUserWorkItem(ClientThreadProc, client);
			}
		}
		void ClientThreadProc(object obj)
		{
			TcpClient clientSocket = (TcpClient)obj;
			byte[] recBuf = new byte[4*1024];//4 kb
			try
			{
				int recSize = clientSocket.Client.Receive(recBuf);

				string userName = Encoding.UTF8.GetString(recBuf, 0, recSize);
				SaveToLog($"имя - {userName}\n");
				clientSocket.Client.Send(Encoding.UTF8.GetBytes("Hello " + userName + "!\n"));

				listClients.Add(new ClientInfo { Name = userName, ClientSocket = clientSocket });
				while (true)
				{
					//clientSocket.Client.ReceiveTimeout = 200;
					recSize = clientSocket.Client.Receive(recBuf);
					if (recSize <= 0 || isStart == false || (recSize <= 0 && isStart == false))
					{
						break;
					}
					foreach (var a in listClients)
					{
						if (a.ClientSocket.Client.Connected)
						{
							a.ClientSocket.Client.Send(recBuf, recSize, SocketFlags.None);
						}
					}
					
				}
				clientSocket.Client.Shutdown(SocketShutdown.Both);
				clientSocket.Close();
				//listClients.Remove(A);
				foreach (var a in listClients)
				{
					if (a.ClientSocket == clientSocket)
					{
						listClients.Remove(a);
						break;
					}
				}
			}
			catch (Exception exception)
			{
				MessageBox.Show(exception.ToString());
			}
		}
		private void CloseButtonClick(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}
