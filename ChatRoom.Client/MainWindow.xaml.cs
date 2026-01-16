using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ChatRoom.Client.Services;
using ChatRoom.Client.Models;
using Microsoft.Win32;
using ChatRoom.Core;
using System.Collections.Generic;
using System.Windows.Threading; // QUAN TRỌNG: Để dùng Timer

namespace ChatRoom.Client
{
    public partial class MainWindow : Window
    {
        private ChatService _chatService;
        private string _myUsername;

        private DispatcherTimer _typingTimer;
        private DateTime _lastTypingSent = DateTime.MinValue;


        private readonly List<string> _emojis = new List<string>
        {
            "😀", "😂", "🥰", "😎", "😭", "😡", "👍", "👎", "❤️", "💔",
            "🎉", "🔥", "💩", "👻", "👀", "👋", "🙏", "💪", "🧠", "💻",
            "🚀", "🍕", "🍺", "⚽", "🎵", "☀️", "🌈", "⭐", "✅", "❌"
        };

        public MainWindow()
        {
            InitializeComponent();

            Random rnd = new Random();
            _myUsername = "User_" + rnd.Next(100, 999);
            this.Title = $"LAN CHAT ROOM - {_myUsername}";

            _chatService = new ChatService();
            _chatService.OnLog += HandleLog;
            _chatService.OnMessageReceived += HandleNewMessage;

            InitializeEmojiPicker();

            _typingTimer = new DispatcherTimer();
            _typingTimer.Interval = TimeSpan.FromSeconds(3);
            _typingTimer.Tick += (s, e) =>
            {
                lblTyping.Visibility = Visibility.Collapsed;
                _typingTimer.Stop();
            };
        }

        private async void txtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((DateTime.Now - _lastTypingSent).TotalSeconds > 2 && !string.IsNullOrEmpty(txtMessage.Text))
            {
                _lastTypingSent = DateTime.Now;
                await _chatService.SendTypingAsync(_myUsername);
            }
        }

        private void InitializeEmojiPicker()
        {
            foreach (var emoji in _emojis)
            {
                Button btn = new Button();
                btn.Content = emoji;
                btn.FontSize = 20;
                btn.Width = 40;
                btn.Height = 40;
                btn.Background = Brushes.Transparent;
                btn.BorderThickness = new Thickness(0);
                btn.Cursor = Cursors.Hand;

                btn.Click += (s, e) =>
                {
                    txtMessage.Text += emoji;
                    txtMessage.CaretIndex = txtMessage.Text.Length;
                    txtMessage.Focus();
                    EmojiPopup.Visibility = Visibility.Collapsed;
                };

                wpEmojis.Children.Add(btn);
            }
        }

        private void btnEmoji_Click(object sender, RoutedEventArgs e)
        {
            if (EmojiPopup.Visibility == Visibility.Visible)
                EmojiPopup.Visibility = Visibility.Collapsed;
            else
                EmojiPopup.Visibility = Visibility.Visible;
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtIP.Text.Trim();
            btnConnect.IsEnabled = false;
            AddMessage("System", $"Connecting to {ip}...", false);
            bool connected = await _chatService.ConnectAsync(ip, 9999);
            if (connected)
            {
                AddMessage("System", "Connected successfully!", false);
                _ = _chatService.StartReadingLoop();
                btnConnect.Content = "CONNECTED";
            }
            else
            {
                AddMessage("System", "Connection failed!", false);
                btnConnect.IsEnabled = true;
            }
        }

        private void HandleNewMessage(ChatPacket packet)
        {
            if (packet.Type == PacketType.Typing)
            {
                Dispatcher.Invoke(() =>
                {
                    lblTyping.Text = $"{packet.Username} is typing...";
                    lblTyping.Visibility = Visibility.Visible;

                    _typingTimer.Stop();
                    _typingTimer.Start();
                });
                return;
            }

            if (packet.Message != null && packet.Message.StartsWith("[FILE_UPLOADED]|"))
            {
                string fileName = packet.Message.Split('|')[1];
                AddMessage(packet.Username, $"[FILE] {fileName}", false);
            }
            else
            {
                AddMessage(packet.Username, packet.Message, false);
            }
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text)) return;

            AddMessage(_myUsername, txtMessage.Text, true);
            await _chatService.SendMessageAsync(txtMessage.Text, _myUsername);

            txtMessage.Clear();
            txtMessage.Focus();
            EmojiPopup.Visibility = Visibility.Collapsed;
        }

        private async void btnFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = openFileDialog.SafeFileName;
                AddMessage("System", $"Uploading: {fileName}...", true);
                await _chatService.UploadFileAsync(filePath, _myUsername);
            }
        }

        private async void btnDownload_Chat_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            string content = (string)btn.Tag;

            if (content.StartsWith("[FILE] "))
            {
                string fileName = content.Substring(7);
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.FileName = fileName;
                if (saveDialog.ShowDialog() == true)
                {
                    string folderPath = System.IO.Path.GetDirectoryName(saveDialog.FileName);
                    _chatService.DownloadFolderPath = folderPath;
                    AddMessage("System", $"Requesting download: {fileName}...", true);
                    await _chatService.RequestDownloadAsync(fileName);
                }
            }
        }

        private void AddMessage(string? sender, string? content, bool isMe)
        {
            Dispatcher.Invoke(() =>
            {
                var msg = new ChatMessage
                {
                    Sender = sender ?? "Unknown",
                    Content = content ?? "",
                    Time = DateTime.Now,
                    IsMe = isMe
                };

                lbChat.Items.Add(msg);
                lbChat.ScrollIntoView(msg);
                lbChat.UpdateLayout();

                var container = lbChat.ItemContainerGenerator.ContainerFromItem(msg) as ListBoxItem;
                if (container != null && content != null && content.StartsWith("[FILE] "))
                {
                    var btn = FindVisualChild<Button>(container);
                    if (btn != null) btn.Visibility = Visibility.Visible;
                }
            });
        }

        private T? FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t) return t;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null) return childOfChild;
            }
            return null;
        }

        private void HandleLog(string msg) { AddMessage("System", msg, false); }
        private void txtMessage_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) btnSend_Click(sender, e); }
        private void btnHistory_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Coming soon!"); }
    }
}