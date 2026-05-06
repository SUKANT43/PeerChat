using Microsoft.Win32;
using PeerChat.Base;
using PeerChat.Enums;
using PeerChat.Helper;
using PeerChat.models;
using PeerChat.Models;
using PeerChat.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        private string _connectionIPAdress;

        public ObservableCollection<MessageModel> MessageList { get; private set; }
        public ObservableCollection<DebugLogModel> DebugLogList { get; private set; }

        public ChatViewModel(MainViewModel mainViewModel, TcpClient client, string name, UserRole role, string connectionIPAdress)
        {
            _mainViewModel = mainViewModel;
            _client = client;
            _myName = name;
            _role = role;
            _connectionIPAdress = connectionIPAdress;
            _chatService = new ChatService(_client);
            MessageList = new ObservableCollection<MessageModel>();
            DebugLogList = new ObservableCollection<DebugLogModel>();

            _typingTimer = new Timer(StopTyping, null, Timeout.Infinite, Timeout.Infinite);

            SendMessageCommand = new RelayCommand(async o => await SendMessage());
            OpenImageFolderCommand = new RelayCommand(async o => await OpenImageFolder());
            SendImageCommand = new RelayCommand(async o => await SendImage());
            CancelImageCommand = new RelayCommand(o => ClearPreview());
            ToggleThemeCommand = new RelayCommand(o => ToggleTheme());
            SendVideoCommand = new RelayCommand(async o => await SendVideo());
            Initialize();
        }

        public ICommand SendMessageCommand { get; }
        public ICommand OpenImageFolderCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand CancelImageCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SendVideoCommand { get; }

        public string PeerName
        {
            get => _peerName;
            private set
            {
                _peerName = value;
                OnPropertyChanged();
            }
        }

        private string _userStatus;
        public string UserStatus
        {
            get => _userStatus;
            set
            {
                _userStatus = value;
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

        private bool _isDebugConsoleVisible = false;
        public bool IsDebugConsoleVisible
        {
            get => _isDebugConsoleVisible;
            set
            {
                _isDebugConsoleVisible = value;
                OnPropertyChanged();
            }
        }

        public bool _isTextFiledVisible = true;
        public bool IsTextFieldVisible
        {
            get => _isTextFiledVisible;
            set
            {
                _isTextFiledVisible = value;
                OnPropertyChanged();
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
                _mainViewModel.CurrentTitle = $"PeerChat — {_myName} ↔ {_connectionIPAdress} ({PeerName})";
                UserStatus = "Online";
                ThemeText = "🌙";
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
                AddDebugLog(MessageDirection.Sent, MessageType.Text, data);
                Message = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Send failed: {ex.Message}");
                HandleDisconnect();
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

        private bool _isRunning = true;


        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    try
                    {
                        MessageFrameModel frame = await _chatService.ReceiveMessageAsync();

                        if (frame != null && frame.Payload != null)
                        {
                            AddDebugLog(MessageDirection.Received, (MessageType)frame.Type, frame.Payload);
                        }

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
                                    //Task.Delay(2000).ContinueWith(o =>
                                    //{
                                    //    Application.Current.Dispatcher.Invoke(() =>
                                    //    {
                                    //        if (TypingStatus != null)
                                    //            TypingStatus = null;
                                    //    });
                                    //});this leads to continous refresh after 3 seconds
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
                        else if ((MessageType)frame.Type == MessageType.Disconnect)
                        {
                            HandleDisconnect();
                        }
                        else if ((MessageType)frame.Type == MessageType.Video)
                        {
                            HandleVideoChunk(frame.Payload);
                        }
                    }
                    catch (IOException)
                    {
                        HandleDisconnect();
                        break;
                    }
                    catch (SocketException)
                    {
                        HandleDisconnect();
                        break;
                    }
                    catch (Exception)
                    {
                        HandleDisconnect();
                        break;
                    }
                }
            });
        }

        private void HandleDisconnect()
        {
            _isRunning = false;
            IsTextFieldVisible = false;
            PreviewImage = null;
            TypingStatus = null;
            UserStatus = "Disconnected";
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

                    IsTextFieldVisible = false;

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"File error: {ex.Message}");
            }
        }


        public async Task SendImage()
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
                AddDebugLog(MessageDirection.Sent, MessageType.Image, payload);
                ClearPreview();
                IsTextFieldVisible = true;
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
            IsTextFieldVisible = true;
        }

        private bool _isDarkMode = false;
        private string _themeText;
        public string ThemeText
        {
            get => _themeText;
            set
            {
                _themeText = value;
                OnPropertyChanged();
            }
        }

        private void ToggleTheme()
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;

            var themeDictionary = dictionaries[0];

            if (_isDarkMode)
            {
                themeDictionary.Source = new Uri("themes/LightTheme.xaml", UriKind.Relative);
                ThemeText = "🌙";
            }
            else
            {
                themeDictionary.Source = new Uri("themes/DarkTheme.xaml", UriKind.Relative);
                ThemeText = "☀";
            }
            _isDarkMode = !_isDarkMode;

        }

        private void AddDebugLog(MessageDirection direction, MessageType type, byte[] payload)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugLogList.Add(new DebugLogModel(direction, type, payload.Length));
            });

        }

        public async Task DisconnectAsync()
        {
            try
            {
                await _chatService.SendMessageAsync((byte)MessageType.Disconnect, Array.Empty<byte>());
            }
            catch
            {

            }

            try
            {
                _client?.Close();
            }
            catch
            {

            }
        }


        private bool _isRecivingVideo = false;
        public bool IsRecivingVideo
        {
            get => _isRecivingVideo;
            set
            {
                _isRecivingVideo = value;
                OnPropertyChanged();
            }
        }

        private object _lockReciveObj = new object();
        private object _lockSendObj = new object();
        private long _totalVideoSize;
        private string _fileName;
        private long _recivedSize;
        private MemoryStream _videoStream;
        private MessageModel _sendMessageModel;
        private MessageModel _reciveMessageModel;
        public async Task SendVideo()
        {
            try
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Video Files(*.mp4;*.avi*.mkv) | *.mp4;*.avi*.mkv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var path = dialog.FileName;

                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException("File not found");
                    }

                    string videoName = Path.GetFileName(path);
                    var videoSize = await Task.Run(() => File.ReadAllBytes(path));
                    bool isFirstChunk = true;
                    int bytesRead;
                    byte[] buffer = new byte[1024 * 64];
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] payLoad;

                            if (isFirstChunk)
                            {
                                isFirstChunk = false;

                                byte[] nameBytes = new byte[260];
                                var fileNameBytes = Encoding.UTF8.GetBytes(videoName);

                                if (nameBytes.Length > 260)
                                    throw new ArgumentException("Filename exceeds 260 bytes");

                                Array.Copy(fileNameBytes, nameBytes, fileNameBytes.Length);

                                byte[] videoLenght = new byte[8];
                                videoLenght[0] = (byte)(videoSize.Length >> 56);
                                videoLenght[1] = (byte)(videoSize.Length >> 48);
                                videoLenght[2] = (byte)(videoSize.Length >> 40);
                                videoLenght[3] = (byte)(videoSize.Length >> 32);
                                videoLenght[4] = (byte)(videoSize.Length >> 24);
                                videoLenght[5] = (byte)(videoSize.Length >> 16);
                                videoLenght[6] = (byte)(videoSize.Length >> 8);
                                videoLenght[7] = (byte)(videoSize.Length);

                                payLoad = new byte[260 + 8 + bytesRead];
                                Buffer.BlockCopy(nameBytes, 0, payLoad, 0, 260);
                                Buffer.BlockCopy(videoLenght, 0, payLoad, 260, 8);
                                Buffer.BlockCopy(buffer, 0, payLoad, 268, bytesRead);
                                _sendMessageModel = new MessageModel(MessageType.Video, MessageDirection.Sent)
                                {
                                    FileName = videoName,
                                    FilePath = path
                                };

                                Application.Current.Dispatcher.Invoke(() =>
                                MessageList.Add(_sendMessageModel));
                            }
                            else
                            {
                                payLoad = new byte[bytesRead];
                                Buffer.BlockCopy(buffer, 0, payLoad, 0, bytesRead);
                            }
                            await _chatService.SendMessageAsync((byte)MessageType.Video, payLoad);
                            AddDebugLog(MessageDirection.Sent, MessageType.Video, payLoad);
                        }
                }
            }
            catch (Exception ex)
            {
                MessageList.Remove(_sendMessageModel);
            }
        }


        private void HandleVideoChunk(byte[] payload)
        {
            lock (_lockReciveObj)
            {
                try
                {
                    if (!_isRecivingVideo)
                    {
                        _isRecivingVideo = true;

                        byte[] nameByte = new byte[260];
                        Array.Copy(payload, nameByte, 260);
                        _fileName = Encoding.UTF8.GetString(nameByte).TrimEnd('\0');

                        byte[] videoLength = new byte[8];
                        Array.Copy(payload, 260, videoLength, 0, 8);

                        _totalVideoSize = (long)(videoLength[0] << 56) | (long)(videoLength[1] << 48) | (long)(videoLength[2] << 40) | (long)(videoLength[3] << 32) | (long)(videoLength[4] << 24) | (long)(videoLength[5] << 16) | (long)(videoLength[6] << 8) | (long)(videoLength[7]);

                        _videoStream = new MemoryStream();
                        _videoStream.Write(payload, 268, payload.Length - 268);
                        _recivedSize += payload.Length - 268;
                        _reciveMessageModel = new MessageModel(MessageType.Video, MessageDirection.Received)
                        {
                            FileName = _fileName
                        };
                        Application.Current.Dispatcher.Invoke(() =>
                        MessageList.Add(_reciveMessageModel));
                    }
                    else
                    {
                        _videoStream.Write(payload, 268, payload.Length - 268);
                        _recivedSize += payload.Length;
                        if (_recivedSize == _totalVideoSize)
                        {
                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                            string folderPath = Path.Combine(desktopPath, "peer chat");

                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }

                            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            string extension = Path.GetExtension(_fileName);
                            string newFileName = $"PeerChat_{timeStamp}{extension}";

                            string finalPath = Path.Combine(folderPath, newFileName);

                            File.WriteAllBytes(finalPath, _videoStream.ToArray());

                            _reciveMessageModel.FilePath = finalPath;

                            _isRecivingVideo = false;
                            _totalVideoSize = 0;
                            _fileName = null;
                            _recivedSize = 0;
                            _videoStream.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _videoStream.Dispose();
                }
            }
        }
    }
}