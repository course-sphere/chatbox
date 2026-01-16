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
using System.Windows.Threading;
using System.Linq; // Cần cái này để lọc List

namespace ChatRoom.Client
{
    public partial class MainWindow : Window
    {
        private ChatService _chatService;
        private string _myUsername;

        // Lưu trữ toàn bộ tin nhắn để phục vụ tìm kiếm
        private List<ChatMessage> _allMessages = new List<ChatMessage>();

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
            _typingTimer.Tick += (s, e) => { lblTyping.Visibility = Visibility.Collapsed; _typingTimer.Stop(); };
        }

        private async void txtMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if ((DateTime.Now - _lastTypingSent).TotalSeconds > 2 && !string.IsNullOrEmpty(txtMessage.Text))
            {
                _lastTypingSent = DateTime.Now;
                await _chatService.SendTypingAsync(_myUsername);
            }
        }

        // --- HÀM TÌM KIẾM TIN NHẮN ---
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = txtSearch.Text.ToLower();

            // 1. Xóa giao diện hiện tại
            lbChat.Items.Clear();

            // 2. Lọc tin nhắn từ kho lưu trữ
            foreach (var msg in _allMessages)
            {
                // Nếu từ khóa rỗng (không tìm gì) HOẶC nội dung chứa từ khóa -> Hiện
                if (string.IsNullOrEmpty(keyword) ||
                    msg.Content.ToLower().Contains(keyword) ||
                    msg.Sender.ToLower().Contains(keyword))
                {
                    lbChat.Items.Add(msg);
                }
            }

            // Cuộn xuống dưới cùng nếu có tin
            if (lbChat.Items.Count > 0)
                lbChat.ScrollIntoView(lbChat.Items[lbChat.Items.Count - 1]);
        }

        private void InitializeEmojiPicker()
        {
            foreach (var emoji in _emojis)
            {
                Button btn = new Button();
                btn.Content = emoji;
                btn.FontSize = 20;
                btn.Width = 40; btn.Height = 40;
                btn.Background = Brushes.Transparent; btn.BorderThickness = new Thickness(0);
                btn.Cursor = Cursors.Hand;
                btn.Click += (s, e) => { txtMessage.Text += emoji; txtMessage.CaretIndex = txtMessage.Text.Length; txtMessage.Focus(); EmojiPopup.Visibility = Visibility.Collapsed; };
                wpEmojis.Children.Add(btn);
            }
        }

        private void btnEmoji_Click(object sender, RoutedEventArgs e)
        {
            EmojiPopup.Visibility = (EmojiPopup.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
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
                    _typingTimer.Stop(); _typingTimer.Start();
                });
                return;
            }

            // --- ÂM THANH THÔNG BÁO ---
            // Nếu tin nhắn không phải của mình -> Kêu "Ting"
            if (packet.Username != _myUsername)
            {
                System.Media.SystemSounds.Exclamation.Play();
            }
            // --------------------------

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
            txtMessage.Clear(); txtMessage.Focus(); EmojiPopup.Visibility = Visibility.Collapsed;
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
                // Kiểm tra xem đây có phải là tin nhắn File không (để gán cờ IsFile)
                bool isFileMsg = content != null && content.StartsWith("[FILE] ");

                var msg = new ChatMessage
                {
                    Sender = sender ?? "Unknown",
                    Content = content ?? "",
                    Time = DateTime.Now,
                    IsMe = isMe,
                    IsFile = isFileMsg // Gán cờ này để XAML tự hiện nút Download
                };

                // 1. Lưu vào kho
                _allMessages.Add(msg);

                // 2. Chỉ hiện lên ListBox nếu khớp với từ khóa tìm kiếm (mặc định là khớp hết)
                string searchKeyword = txtSearch.Text.ToLower();
                if (string.IsNullOrEmpty(searchKeyword) ||
                    msg.Content.ToLower().Contains(searchKeyword) ||
                    msg.Sender.ToLower().Contains(searchKeyword))
                {
                    lbChat.Items.Add(msg);
                    lbChat.ScrollIntoView(msg);
                }
            });
        }

        // Đã xóa hàm FindVisualChild vì giờ dùng Binding xịn rồi
        private void HandleLog(string msg) { AddMessage("System", msg, false); }
        private void txtMessage_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter) btnSend_Click(sender, e); }
        private void btnHistory_Click(object sender, RoutedEventArgs e) { MessageBox.Show("Coming soon!"); }
    }
}