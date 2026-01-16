using System;
using System.Windows;
using System.Windows.Input;
using ChatRoom.Client.Services;
using ChatRoom.Client.Models;
using Microsoft.Win32;

namespace ChatRoom.Client
{
    public partial class MainWindow : Window
    {
        private ChatService _chatService;
        private string _myUsername = "Me";

        public MainWindow()
        {
            InitializeComponent();
            _chatService = new ChatService();
            _chatService.OnLog += HandleLog;

            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            AddMessage("System", "Connecting to server...", false);
            bool connected = await _chatService.ConnectAsync("127.0.0.1", 9999);
            if (connected)
                AddMessage("System", "Connected successfully!", false);
            else
                AddMessage("System", "Connection failed!", false);
        }
        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text)) return;

            AddMessage(_myUsername, txtMessage.Text, true);

            await _chatService.SendMessageAsync(txtMessage.Text, _myUsername);

            txtMessage.Clear();
            txtMessage.Focus();
        }
        private void btnFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = openFileDialog.SafeFileName;
                AddMessage(_myUsername, $"[Đang gửi file]: {fileName}...", true);

                
            }
        }

        private void btnEmoji_Click(object sender, RoutedEventArgs e)
        {
            txtMessage.Text += "😊";
            txtMessage.CaretIndex = txtMessage.Text.Length;
            txtMessage.Focus();
        }

        private void btnHistory_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Tính năng xem lịch sử sẽ lấy từ SQL Server (Giai đoạn sau)");
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnSend_Click(sender, e);
        }
        private void HandleLog(string msg)
        {
            AddMessage("System", msg, false);
        }
        private void AddMessage(string sender, string content, bool isMe)
        {
            Dispatcher.Invoke(() =>
            {
                var msg = new ChatMessage
                {
                    Sender = sender,
                    Content = content,
                    Time = DateTime.Now,
                    IsMe = isMe
                };
                lbChat.Items.Add(msg);

                if (lbChat.Items.Count > 0)
                    lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
            });
        }
    }
}