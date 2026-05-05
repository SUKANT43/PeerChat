using Microsoft.Win32;
using PeerChat.Base;
using PeerChat.Enums;
using PeerChat.Helper;
using PeerChat.models;
using PeerChat.Models;
using PeerChat.Protocol;
using PeerChat.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PeerChat.ViewModel
{
    public class ChatViewModel : Observable
    {
        private readonly MainViewModel _mainViewModel;
        private readonly TcpClient _client;
        private readonly ChatService _chatService;
        private readonly string _myName;
        private string _peerName;
        private UserRole _role;
        private Timer _typingTimer;
        private bool _isTyping;

        public ObservableCollection<MessageModel> MessageList { get; private set; }

        public ChatViewModel(MainViewModel mainViewModel, TcpClient client, string name, UserRole role)
        {
            _mainViewModel = mainViewModel;
            _client = client;
            _myName = name;
            _role = role;
            _chatService = new ChatService(_client);
            MessageList = new ObservableCollection<MessageModel>();

            _typingTimer = new Timer(StopTyping, null, Timeout.Infinite, Timeout.Infinite);

            SendMessageCommand = new RelayCommand(async o => await SendMessage());
            OpenImageFolderCommand = new RelayCommand(async o => await OpenImageFolder());
            SendImageCommand = new RelayCommand(async o => await SendImage());
            CancelImageCommand = new RelayCommand(o => ClearPreview());
            ToggleThemeCommand = new RelayCommand(o => ToggleTheme());

            Initialize();
        }

        public ICommand SendMessageCommand { get; }
        public ICommand OpenImageFolderCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand CancelImageCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        public string PeerName
        {
            get => _peerName;
            private set
            {
                _peerName = value;
                OnPropertyChanged();
            }
        }


        private string _message;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsVisible));

                if (!string.IsNullOrEmpty(value))
                {
                    SendTypingIndicator();
                }
            }
        }

        public bool IsVisible => !string.IsNullOrEmpty(Message);

        private string _typingStatus;
        public string TypingStatus
        {
            get => _typingStatus;
            set
            {
                _typingStatus = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTypingVisible));
            }
        }

        public bool IsTypingVisible => !string.IsNullOrEmpty(TypingStatus);

        private byte[] _selectedImageBytes;
        private string _selectedImageName;
        private BitmapImage _previewImage;

        public BitmapImage PreviewImage
        {
            get => _previewImage;
            set
            {
                _previewImage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsImagePreviewVisible));
            }
        }

        public bool IsImagePreviewVisible => PreviewImage != null;


        private async void Initialize()
        {
            try
            {
                await _chatService.SendNameAsync(_myName);
                PeerName = await _chatService.ReceiveNameAsync();
                StartReceiveLoop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization error: {ex.Message}");
            }
        }

        public async Task SendMessage()
        {
            try
            {
                if (string.IsNullOrEmpty(Message))
                    return;

                var data = Encoding.UTF8.GetBytes(Message);
                await _chatService.SendMessageAsync((byte)MessageType.Text, data);

                MessageList.Add(new MessageModel(MessageType.Text, MessageDirection.Sent)
                {
                    Content = Message
                });

                Message = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Send failed: {ex.Message}");
            }
        }

        private DateTime _lastTypingSent = DateTime.MinValue;

        private async void SendTypingIndicator()
        {

            if ((DateTime.Now - _lastTypingSent).TotalMilliseconds < 500)
            {
                return;
            }

            _lastTypingSent = DateTime.Now;

            await _chatService.SendMessageAsync((byte)MessageType.Typing, Array.Empty<byte>());

            _isTyping = true;

            _typingTimer.Change(1500, Timeout.Infinite);
        }

        private async void StopTyping(object state)
        {
            if (!_isTyping)
                return;

            _isTyping = false;

            await _chatService.SendMessageAsync((byte)MessageType.Typing, new byte[] { 0 });
        }

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {

                while (true)
                {
                    try
                    {
                        MessageFrameModel frame = await _chatService.ReceiveMessageAsync();

                        if (frame == null || frame.Payload == null) continue;

                        if ((MessageType)frame.Type == MessageType.Text)
                        {
                            TypingStatus = null;
                            string message = Encoding.UTF8.GetString(frame.Payload);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageList.Add(new MessageModel(MessageType.Text, MessageDirection.Received)
                                {
                                    Content = message
                                });
                            });
                        }
                        else if ((MessageType)frame.Type == MessageType.Typing)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                if (frame.Payload.Length == 0)
                                {
                                    TypingStatus = $"{PeerName} is typing...";
                                    Task.Delay(2000).ContinueWith(o =>
                                    {
                                        Application.Current.Dispatcher.Invoke(() =>
                                        {
                                            if (TypingStatus != null)
                                                TypingStatus = null;
                                        });
                                    });
                                }
                                else
                                {
                                    TypingStatus = null;
                                }
                            });
                        }
                        else if ((MessageType)frame.Type == MessageType.Image)
                        {
                            byte[] imageData;
                            string filename;
                            FileHelper.DecodeImagePayload(frame.Payload, out imageData, out filename);

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageList.Add(new MessageModel(MessageType.Image, MessageDirection.Received)
                                {
                                    ImageData = FileHelper.ConvertToImage(imageData),
                                    FileName = filename
                                });
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Connection lost: {ex.Message}");
                        });
                        break;
                    }

                }
            });
        }
        public async Task OpenImageFolder()
        {
            try
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Image Files (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
                };

                if (dialog.ShowDialog() == true)
                {
                    string filePath = dialog.FileName;

                    if (!File.Exists(filePath))
                        throw new FileNotFoundException("File not found");

                    _selectedImageBytes = await Task.Run(() => File.ReadAllBytes(filePath));
                    _selectedImageName = Path.GetFileName(filePath);

                    PreviewImage = FileHelper.ConvertToImage(_selectedImageBytes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File error: {ex.Message}");
            }
        }


        private async Task SendImage()
        {
            try
            {
                if (_selectedImageBytes == null)
                    return;

                byte[] payload = FileHelper.EncodeImagePayload(_selectedImageName, _selectedImageBytes);
                await _chatService.SendMessageAsync((byte)MessageType.Image, payload);

                MessageList.Add(new MessageModel(MessageType.Image, MessageDirection.Sent)
                {
                    FileName = _selectedImageName,
                    ImageData = FileHelper.ConvertToImage(_selectedImageBytes)
                });

                ClearPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image send failed: {ex.Message}");
            }
        }

        private void ClearPreview()
        {
            _selectedImageBytes = null;
            _selectedImageName = null;
            PreviewImage = null;
        }

        private bool _isDarkMode = false;

        private void ToggleTheme()
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var themeDictionary = dictionaries[0];

            if (_isDarkMode)
            {
                themeDictionary.Source = new Uri("themes/LightTheme.xaml", UriKind.Relative);

            }
            else
            {
                themeDictionary.Source = new Uri("themes/DarkTheme.xaml", UriKind.Relative);

            }
            _isDarkMode = !_isDarkMode;

        }
    }
}