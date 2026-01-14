using System;
using System.Windows;
using ChatRoom.Server.Services; 

namespace ChatRoom.Server
{
    public partial class MainWindow : Window
    {
        private ServerListener? _server; 

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Port phải là số!");
                return;
            }
            btnStart.IsEnabled = false;
            txtPort.IsEnabled = false;
            btnStop.IsEnabled = true;

          
            _server = new ServerListener(port, AddLog);
            await _server.StartAsync();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_server != null)
            {
                _server.Stop();
            }

            btnStart.IsEnabled = true;
            txtPort.IsEnabled = true;
            btnStop.IsEnabled = false;
        }
        private void AddLog(string msg)
        {
            Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                lbLogs.Items.Insert(0, $"[{time}] {msg}");
            });
        }
    }
}