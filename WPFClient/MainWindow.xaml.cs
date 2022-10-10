using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isConnect = false;
        private const string ipAddress = "127.0.0.1";
        private const int port = 2854;
        private TcpClient tcpClient;
        private NetworkStream ns;
        private Task task;
        private CancellationTokenSource ctSource;
        private CancellationToken ct;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectServer()
        {
            try
            {
                IPAddress ip = IPAddress.Parse(ipAddress);
                tcpClient = new TcpClient();
                tcpClient.Connect(ip, port);
                ns = tcpClient.GetStream();
                ctSource = new CancellationTokenSource();
                ct = ctSource.Token;
                task = new Task(() => ReceiveData(tcpClient), ct);
                task.Start();
                isConnect = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Server Connection Problem " + ex.Message, ex.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void btnSendMessage_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(txtClientMessage.Text) || !isConnect) return;
            byte[] buffer = Encoding.UTF8.GetBytes(txtClientMessage.Text);
            ns.Write(buffer, 0, buffer.Length);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (isConnect)
            {
                MessageBox.Show("You're already connected", "Connection Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Thread thread = new Thread(ConnectServer);
            thread.Start();
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ctSource.Cancel();
                tcpClient.Client.Shutdown(SocketShutdown.Both);
                task.Wait();
                ns.Close();
                tcpClient.Close();
                isConnect = false;
                txtServerResponding.Text = "Dissconected";
            }
            catch
            {
                MessageBox.Show("Your are not connected", "Disconnection error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
        }

        private void ReceiveData(object o)
        {
            NetworkStream ns = (o as TcpClient).GetStream();
            byte[] receivedBytes = new byte[1024]; ;
            int byte_count;
            if (ct.IsCancellationRequested) return; // вообще не працює чогось
            while ((byte_count = ns.Read(receivedBytes, 0, receivedBytes.Length)) > 0)
            {
                this.Dispatcher.Invoke(() => 
                {
                    try
                    {
                        txtServerResponding.Text = Encoding.UTF8.GetString(receivedBytes, 0, 1024);
                    }
                    catch
                    {
                        return;
                    }
                });
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                ctSource.Cancel();
                tcpClient.Client.Shutdown(SocketShutdown.Both);
                task.Wait();
                ns.Close();
                tcpClient.Close();
            }
            catch
            {
                return;
            }
        }
    }
}
