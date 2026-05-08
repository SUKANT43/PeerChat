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
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly object _videoLock = new object();

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
            _videoStream = new MemoryStream();

            SendMessageCommand = new RelayCommand(async o => await SendMessage());
            OpenImageFolderCommand = new RelayCommand(async o => await OpenImageFolder());
            SendImageCommand = new RelayCommand(async o => await SendImage());
            CancelImageCommand = new RelayCommand(o => ClearPreview());
            ToggleThemeCommand = new RelayCommand(o => ToggleTheme());
            SendVideoCommand = new RelayCommand(async o => await SendVideo());
            PlayVideoCommand = new RelayCommand(PlayVideo);
            ShowDebuggerCommand = new RelayCommand(o => ShowDebugger());
            LogoutCommand = new RelayCommand(async o => await LogOut());
            ShowFilePickerCommand = new RelayCommand(o => ShowFilePicker());
            Initialize();
        }

        private bool _isFilePickerVisible;
        public bool IsFilePickerVisible
        {
            get => _isFilePickerVisible;
            set
            {
                _isFilePickerVisible = value;
                OnPropertyChanged();
            }
        }

        private void ShowFilePicker()
        {
            IsFilePickerVisible = !IsFilePickerVisible;
        }

        private async Task LogOut()
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                await DisconnectAsync();
                _mainViewModel.NavigateToConnection();
            }
        }

        private void ShowDebugger()
        {
            IsDebugConsoleVisible = !IsDebugConsoleVisible;
        }

        public void PlayVideo(object obj)
        {
            try
            {
                if (obj is MessageModel message && !string.IsNullOrEmpty(message.FilePath) && File.Exists(message.FilePath))
                {
                    var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = message.FilePath;
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                    process.Dispose();
                }
            }
            catch (Exception ex)
            {
            }
        }

        public ICommand SendMessageCommand { get; }
        public ICommand OpenImageFolderCommand { get; }
        public ICommand SendImageCommand { get; }
        public ICommand CancelImageCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand SendVideoCommand { get; }
        public ICommand PlayVideoCommand { get; }
        public ICommand ShowDebuggerCommand { get; }
        public ICommand ShowFilePickerCommand { get; }

        public ICommand LogoutCommand { get; }
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

        private bool _isTextFiledVisible = true;
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
                InitializeTheme();
                StartReceiveLoop();
            }
            catch (Exception ex)
            {
                HandleDisconnect();
            }
        }

        public async Task SendMessage()
        {
            try
            {
                if (!_isRunning) return;

                if (string.IsNullOrEmpty(Message))
                    return;

                var data = Encoding.UTF8.GetBytes(Message);
                await _chatService.SendMessageAsync((byte)MessageType.Text, data);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageList.Add(new MessageModel(MessageType.Text, MessageDirection.Sent)
                    {
                        Content = Message
                    });
                });

                AddDebugLog(MessageDirection.Sent, MessageType.Text, data);
                Message = string.Empty;
            }
            catch (Exception ex)
            {
                HandleDisconnect();
            }
        }

        private DateTime _lastTypingSent = DateTime.MinValue;

        private async void SendTypingIndicator()
        {
            if ((DateTime.Now - _lastTypingSent).TotalMilliseconds < 500)
                return;

            _lastTypingSent = DateTime.Now;

            try
            {
                await _chatService.SendMessageAsync((byte)MessageType.Typing, Array.Empty<byte>());
                _isTyping = true;
                _typingTimer.Change(1500, Timeout.Infinite);
            }
            catch { }
        }

        private async void StopTyping(object state)
        {
            if (!_isTyping) return;

            _isTyping = false;

            try
            {
                await _chatService.SendMessageAsync((byte)MessageType.Typing, new byte[] { 0 });
            }
            catch { }
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
                        if (!_isRunning || !_client.Connected)
                        {
                            HandleDisconnect();
                            break;
                        }

                        MessageFrameModel frame = await _chatService.ReceiveMessageAsync();

                        if (!_isRunning) break;

                        if (frame != null && frame.Payload != null)
                        {
                            AddDebugLog(MessageDirection.Received, (MessageType)frame.Type, frame.Payload);
                        }

                        if (frame == null || frame.Payload == null) continue;

                        switch ((MessageType)frame.Type)
                        {
                            case MessageType.Text:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    TypingStatus = null;
                                    string message = Encoding.UTF8.GetString(frame.Payload);
                                    MessageList.Add(new MessageModel(MessageType.Text, MessageDirection.Received)
                                    {
                                        Content = message
                                    });
                                });
                                break;

                            case MessageType.Typing:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (frame.Payload.Length == 0)
                                        TypingStatus = $"{PeerName} is typing...";
                                    else
                                        TypingStatus = null;
                                });
                                break;

                            case MessageType.Image:
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
                                break;

                            case MessageType.Disconnect:
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageList.Add(new MessageModel(MessageType.Text, MessageDirection.Received)
                                    {
                                        Content = "*** Peer has disconnected ***"
                                    });
                                });
                                HandleDisconnect();
                                break;

                            case MessageType.Video:
                                await HandleVideoChunk(frame.Payload);
                                break;
                        }
                    }
                    catch (IOException)
                    {
                        HandleDisconnect();
                        break;
                    }
                    catch (Exception ex)
                    {
                        AddDebugLog(MessageDirection.Received, MessageType.Text, Encoding.UTF8.GetBytes($"Error: {ex.Message}"));
                        HandleDisconnect();
                        break;
                    }
                }
            }, _cts.Token);
        }

        private void HandleDisconnect()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts.Cancel();

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                IsTextFieldVisible = false;
                PreviewImage = null;
                TypingStatus = null;
                UserStatus = "Disconnected";
            });

            if (_sendMessageModel != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MessageList.Contains(_sendMessageModel))
                        MessageList.Remove(_sendMessageModel);
                    _sendMessageModel = null;
                });
            }

            if (_reciveMessageModel != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (MessageList.Contains(_reciveMessageModel))
                        MessageList.Remove(_reciveMessageModel);
                    _reciveMessageModel = null;
                });
            }

            if (IsRecivingVideo)
            {
                ResetVideoReceivingState();
            }
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

            }
        }

        public async Task SendImage()
        {
            try
            {
                if (_selectedImageBytes == null) return;

                byte[] payload = FileHelper.EncodeImagePayload(_selectedImageName, _selectedImageBytes);
                await _chatService.SendMessageAsync((byte)MessageType.Image, payload);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageList.Add(new MessageModel(MessageType.Image, MessageDirection.Sent)
                    {
                        FileName = _selectedImageName,
                        ImageData = FileHelper.ConvertToImage(_selectedImageBytes)
                    });
                });

                AddDebugLog(MessageDirection.Sent, MessageType.Image, payload);
                ClearPreview();
                IsTextFieldVisible = true;
            }
            catch (Exception ex)
            {

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

        private void InitializeTheme()
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var themeDictionary = dictionaries[0];

            string windowsTheme = ThemeHelper.GetWindowsTheme();
            _isDarkMode = windowsTheme == "Dark Theme";

            if (_isDarkMode)
            {
                themeDictionary.Source = new Uri("themes/DarkTheme.xaml", UriKind.Relative);
                ThemeText = "🌙";
            }
            else
            {
                themeDictionary.Source = new Uri("themes/LightTheme.xaml", UriKind.Relative);
                ThemeText = "☀️";
            }
        }

        private void ToggleTheme()
        {
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            var themeDictionary = dictionaries[0];

            _isDarkMode = !_isDarkMode;

            if (_isDarkMode)
            {
                themeDictionary.Source = new Uri("themes/DarkTheme.xaml", UriKind.Relative);
                ThemeText = "🌙";
            }
            else
            {
                themeDictionary.Source = new Uri("themes/LightTheme.xaml", UriKind.Relative);
                ThemeText = "☀️";
            }
        }

        private void AddDebugLog(MessageDirection direction, MessageType type, byte[] payload)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugLogList.Add(new DebugLogModel(direction, type, payload.Length));
                if (DebugLogList.Count > 100)
                    DebugLogList.RemoveAt(0);
            });
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_isRunning)
                {
                    await _chatService.SendMessageAsync((byte)MessageType.Disconnect, Array.Empty<byte>());
                }
            }
            catch { }

            try
            {
                _client?.Close();
                _cts.Cancel();
            }
            catch { }

            try
            {
                DeleteVideoFolder();
            }
            catch { }
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

        private long _totalVideoSize;
        private string _fileName;
        private long _recivedSize;
        private MemoryStream _videoStream;
        private MessageModel _sendMessageModel;
        private MessageModel _reciveMessageModel;
        private long _sentSize;

        private bool _isSending;
        public bool IsSending
        {
            get => _isSending;
            set
            {
                _isSending = value;
                OnPropertyChanged();
            }
        }

        public async Task SendVideo()
        {
            if (IsRecivingVideo)
            {
                ResetVideoReceivingState();
            }

            CancellationTokenSource sendCts = null;
            try
            {
                var dialog = new OpenFileDialog()
                {
                    Filter = "Video Files(*.mp4;*.avi;*.mkv) | *.mp4;*.avi;*.mkv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var path = dialog.FileName;

                    if (!File.Exists(path))
                        throw new FileNotFoundException("File not found");

                    string videoName = Path.GetFileName(path);
                    long videoSize = new FileInfo(path).Length;

                    var result = MessageBox.Show($"Do you want to send this video?\n\n" + $"File: {videoName}\n" + $"Size: {videoSize}\n\n" + $"Click Yes to send, No to cancel.", "Confirm Video Send", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                        return;

                    BitmapImage thumbnail;
                    try
                    {
                        thumbnail = await FileHelper.GenerateVideoThumbnailAsync(path);
                    }
                    catch
                    {
                        thumbnail = null;
                    }

                    MessageModel sendingModel = null;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        sendingModel = new MessageModel(MessageType.Video, MessageDirection.Sent)
                        {
                            IsSending = true,
                            FileName = videoName,
                            FilePath = path,
                            Progress = 0,
                            VideoThumbnail = thumbnail
                        };
                        MessageList.Add(sendingModel);
                        _sendMessageModel = sendingModel;
                    });

                    sendCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

                    await Task.Run(async () =>
                    {
                        bool isFirstChunk = true;
                        int bytesRead;
                        byte[] buffer = new byte[1024 * 32];
                        long sentSize = 0;

                        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, buffer.Length, true))
                        {
                            IsSending = true;

                            while (_isRunning && (bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, sendCts.Token)) > 0)
                            {
                                if (!_isRunning || sendCts.Token.IsCancellationRequested)
                                    break;

                                byte[] payLoad;

                                if (isFirstChunk)
                                {
                                    isFirstChunk = false;

                                    byte[] nameBytes = new byte[260];
                                    var fileNameBytes = Encoding.UTF8.GetBytes(videoName);
                                    Array.Copy(fileNameBytes, nameBytes, Math.Min(fileNameBytes.Length, 260));

                                    byte[] videoLength = new byte[8];
                                    videoLength[0] = (byte)(videoSize >> 56);
                                    videoLength[1] = (byte)(videoSize >> 48);
                                    videoLength[2] = (byte)(videoSize >> 40);
                                    videoLength[3] = (byte)(videoSize >> 32);
                                    videoLength[4] = (byte)(videoSize >> 24);
                                    videoLength[5] = (byte)(videoSize >> 16);
                                    videoLength[6] = (byte)(videoSize >> 8);
                                    videoLength[7] = (byte)(videoSize);

                                    payLoad = new byte[260 + 8 + bytesRead];
                                    Buffer.BlockCopy(nameBytes, 0, payLoad, 0, 260);
                                    Buffer.BlockCopy(videoLength, 0, payLoad, 260, 8);
                                    Buffer.BlockCopy(buffer, 0, payLoad, 268, bytesRead);
                                    sentSize += bytesRead;
                                }
                                else
                                {
                                    payLoad = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, payLoad, 0, bytesRead);
                                    sentSize += bytesRead;
                                }

                                await _chatService.SendMessageAsync((byte)MessageType.Video, payLoad);
                                await Task.Delay(10);

                                double progress = (double)sentSize / videoSize * 100;
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (_sendMessageModel != null)
                                        _sendMessageModel.Progress = progress;
                                });

                                if (sentSize >= videoSize)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        if (_sendMessageModel != null)
                                        {
                                            _sendMessageModel.IsSending = false;
                                            _sendMessageModel.Progress = 100;
                                        }
                                    });
                                    IsSending = false;
                                }

                                AddDebugLog(MessageDirection.Sent, MessageType.Video, payLoad);
                            }
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_sendMessageModel != null)
                            {
                                _sendMessageModel.IsSending = false;
                                _sendMessageModel.Progress = 100;
                            }
                            _sendMessageModel = null;
                            _isSending = false;
                        });
                    }, sendCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_sendMessageModel != null && MessageList.Contains(_sendMessageModel))
                        MessageList.Remove(_sendMessageModel);
                    _sendMessageModel = null;
                });
                IsSending = false;
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_sendMessageModel != null && MessageList.Contains(_sendMessageModel))
                        MessageList.Remove(_sendMessageModel);
                    _sendMessageModel = null;
                });
                await DisconnectAsync();
            }
            finally
            {
                sendCts?.Dispose();
            }
        }

        private async Task HandleVideoChunk(byte[] payload)
        {
            if (payload == null || payload.Length < 268)
                return;

            try
            {
                if (!_isRunning)
                {
                    ResetVideoReceivingState();
                    return;
                }

                if (!IsRecivingVideo)
                {
                    byte[] nameByte = new byte[260];
                    Array.Copy(payload, nameByte, 260);
                    string fileName = Encoding.UTF8.GetString(nameByte).TrimEnd('\0');

                    byte[] videoLength = new byte[8];
                    Array.Copy(payload, 260, videoLength, 0, 8);

                    long totalVideoSize = ((long)videoLength[0] << 56) | ((long)videoLength[1] << 48) |((long)videoLength[2] << 40) | ((long)videoLength[3] << 32) |((long)videoLength[4] << 24) | ((long)videoLength[5] << 16) |((long)videoLength[6] << 8) | videoLength[7];

                    if (totalVideoSize <= 0 || totalVideoSize > 2147483648)
                    {
                        ResetVideoReceivingState();
                        MessageBox.Show("Video size less than 2Gb");
                        return;
                    }

                    IsRecivingVideo = true;
                    _fileName = fileName;
                    _totalVideoSize = totalVideoSize;
                    _videoStream = new MemoryStream();

                    int dataStart = 268;
                    int dataLength = payload.Length - dataStart;
                    if (dataLength > 0)
                    {
                        _videoStream.Write(payload, dataStart, dataLength);
                        _recivedSize = dataLength;
                    }

                    _reciveMessageModel = new MessageModel(MessageType.Video, MessageDirection.Received)
                    {
                        FileName = _fileName,
                        IsReceiving = true,
                        Progress = _totalVideoSize > 0 ? (double)_recivedSize / _totalVideoSize * 100 : 0
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageList.Add(_reciveMessageModel);
                    });
                }
                else
                {
                    _videoStream.Write(payload, 0, payload.Length);
                    _recivedSize += payload.Length;

                    double progress = _totalVideoSize > 0 ? (double)_recivedSize / _totalVideoSize * 100 : 0;
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_reciveMessageModel != null)
                            _reciveMessageModel.Progress = progress;
                    });

                    if (_recivedSize >= _totalVideoSize)
                    {
                        byte[] completeVideo = _videoStream.ToArray();

                        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string extension = Path.GetExtension(_fileName);
                        string finalFileName = $"PeerChat_{timeStamp}{extension}";

                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                        string folderPath = Path.Combine(desktopPath, "peer chat");

                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }
                        string finalPath = Path.Combine(folderPath, finalFileName);
                        File.WriteAllBytes(finalPath, completeVideo);

                        BitmapImage thumbnail = null;
                        try
                        {
                            //thumbnail = FileHelper.GenerateVideoThumbnailAsync(finalPath);
                        }
                        catch (Exception ex)
                        {
                            thumbnail = null;
                        }

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (_reciveMessageModel != null)
                            {
                                _reciveMessageModel.VideoThumbnail = thumbnail;
                                _reciveMessageModel.FilePath = finalPath;
                                _reciveMessageModel.IsReceiving = false;
                                _reciveMessageModel.Progress = 100;
                            }
                        });

                        ResetVideoReceivingState();
                    }
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_reciveMessageModel != null && MessageList.Contains(_reciveMessageModel))
                        MessageList.Remove(_reciveMessageModel);

                    if (_isRunning)
                    {
                        MessageBox.Show($"Error receiving video: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });

                ResetVideoReceivingState();
            }
        }

        private void ResetVideoReceivingState()
        {
            if (_videoStream != null)
            {
                _videoStream.Dispose();
                _videoStream = null;
            }
            _videoStream = new MemoryStream();
            IsRecivingVideo = false;
            _totalVideoSize = 0;
            _fileName = null;
            _recivedSize = 0;
            _reciveMessageModel = null;
        }

        private void DeleteVideoFolder()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string folderPath = Path.Combine(desktopPath, "peer chat");

                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, true);
                }
            }
            catch { }
        }
    }
}