using System;
using System.Windows;
using System.Windows.Input;
using ChatRoom.Client.Services;
using ChatRoom.Client.Models;
using Microsoft.Win32;
using ChatRoom.Core;

namespace ChatRoom.Client
{
    public partial class MainWindow : Window
    {
        private ChatService _chatService;
        private string _myUsername;

        public MainWindow()
        {
            InitializeComponent();

            Random rnd = new Random();
            _myUsername = "User_" + rnd.Next(100, 999);
            this.Title = $"LAN CHAT ROOM - {_myUsername}";

            _chatService = new ChatService();
            _chatService.OnLog += HandleLog;
            _chatService.OnMessageReceived += HandleNewMessage;

        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = txtIP.Text.Trim();

            btnConnect.IsEnabled = false;

            AddMessage("System", $"Connecting to {ipAddress}...", false);

            bool connected = await _chatService.ConnectAsync(ipAddress, 9999);

            if (connected)
            {
                AddMessage("System", "Connected successfully!", false);
                _ = _chatService.StartReadingLoop();

                btnConnect.Content = "CONNECTED";
            }
            else
            {
                AddMessage("System", "Connection failed! Check IP/Firewall.", false);
                btnConnect.IsEnabled = true; 
            }
        }


        private void HandleNewMessage(ChatPacket packet)
        {
            AddMessage(packet.Username, packet.Message, false);
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text)) return;
            AddMessage(_myUsername, txtMessage.Text, true);
            await _chatService.SendMessageAsync(txtMessage.Text, _myUsername);
            txtMessage.Clear();
            txtMessage.Focus();
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

        private void HandleLog(string msg) { AddMessage("System", msg, false); }
        private void txtMessage_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) btnSend_Click(sender, e); }
        private void btnFile_Click(object sender, RoutedEventArgs e) { /* Code file cũ */ }
        private void btnEmoji_Click(object sender, RoutedEventArgs e) { /* Code emoji cũ */ }
        private void btnHistory_Click(object sender, RoutedEventArgs e) { /* Code history cũ */ }
    }
}