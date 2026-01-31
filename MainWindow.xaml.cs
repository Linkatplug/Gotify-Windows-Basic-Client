using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Threading;

namespace GotifyClient
{
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private ObservableCollection<GotifyMessage> messages;
        private string serverUrl;
        private string clientToken;
        private const string ConfigFile = "gotify_config.json";
        private bool soundNotificationsEnabled = true;
        private bool windowsNotificationsEnabled = true;
        private Dictionary<int, string> applicationNames = new Dictionary<int, string>();

        public MainWindow()
        {
            InitializeComponent();
            InitializeSystemTray();
            messages = new ObservableCollection<GotifyMessage>();
            MessagesItemsControl.ItemsSource = messages;
            LoadConfiguration();
            UpdateEmptyState();
            
            // Si config existe, afficher les paramètres pour permettre la connexion
            if (!string.IsNullOrEmpty(serverUrl) && !string.IsNullOrEmpty(clientToken))
            {
                ServerUrlDisplay.Text = $"• {serverUrl}";
            }
            
            // Timer pour mettre à jour les temps relatifs toutes les minutes
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += (s, e) => RefreshRelativeTimes();
            timer.Start();
        }

        private void RefreshRelativeTimes()
        {
            foreach (var message in messages)
            {
                message.UpdateRelativeTime();
            }
        }

        private void UpdateEmptyState()
        {
            EmptyState.Visibility = messages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void InitializeSystemTray()
        {
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = System.Drawing.SystemIcons.Information;
            notifyIcon.Text = "Gotify Client";
            notifyIcon.Visible = false;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Ouvrir", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Quitter", null, (s, e) => QuitApplication());
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => ShowWindow();
        }

        private void ShowWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            notifyIcon.Visible = false;
        }

        private void QuitApplication()
        {
            DisconnectWebSocket();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && MinimizeToTrayMenuItem.IsChecked == true)
            {
                Hide();
                notifyIcon.Visible = true;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (MinimizeToTrayMenuItem.IsChecked == true)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            else
            {
                DisconnectWebSocket();
                notifyIcon.Dispose();
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(ConfigFile))
                {
                    var json = File.ReadAllText(ConfigFile);
                    var config = JsonSerializer.Deserialize<ConfigData>(json);
                    serverUrl = config.ServerUrl;
                    clientToken = config.Token;
                    ServerUrlTextBox.Text = serverUrl;
                    TokenPasswordBox.Password = clientToken;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur de chargement de la configuration: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveConfiguration()
        {
            try
            {
                var config = new ConfigData
                {
                    ServerUrl = serverUrl,
                    Token = clientToken
                };
                var json = JsonSerializer.Serialize(config);
                File.WriteAllText(ConfigFile, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erreur de sauvegarde de la configuration: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ShowSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Visible;
            SettingsStatusText.Text = "";
        }

        private void ShowOptionsMenu_Click(object sender, RoutedEventArgs e)
        {
            OptionsMenuPopup.IsOpen = !OptionsMenuPopup.IsOpen;
        }

        private void CancelSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            serverUrl = ServerUrlTextBox.Text.Trim().TrimEnd('/');
            clientToken = TokenPasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(clientToken))
            {
                SettingsStatusText.Text = "Veuillez entrer l'URL du serveur et le token.";
                return;
            }

            try
            {
                SaveSettingsButton.IsEnabled = false;
                SettingsStatusText.Text = "Connexion en cours...";
                SettingsStatusText.Foreground = System.Windows.Media.Brushes.Blue;
                
                await TestConnection();
                SaveConfiguration();
                await ConnectWebSocket();
                
                SettingsOverlay.Visibility = Visibility.Collapsed;
                ServerUrlDisplay.Text = $"• {serverUrl}";
            }
            catch (Exception ex)
            {
                SettingsStatusText.Text = $"Erreur: {ex.Message}";
                SettingsStatusText.Foreground = System.Windows.Media.Brushes.Red;
                UpdateConnectionStatus(false);
            }
            finally
            {
                SaveSettingsButton.IsEnabled = true;
            }
        }

        private async Task TestConnection()
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("X-Gotify-Key", clientToken);
                var response = await client.GetAsync($"{serverUrl}/current/user");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erreur HTTP {(int)response.StatusCode} ({response.StatusCode})\n\nURL: {serverUrl}/current/user\n\nDétails: {errorContent}");
                }
            }
        }

        private async Task ConnectWebSocket()
        {
            DisconnectWebSocket();

            cancellationTokenSource = new CancellationTokenSource();
            webSocket = new ClientWebSocket();

            var wsUrl = serverUrl.Replace("https://", "wss://").Replace("http://", "ws://");
            var uri = new Uri($"{wsUrl}/stream?token={clientToken}");

            try
            {
                await webSocket.ConnectAsync(uri, cancellationTokenSource.Token);
                await LoadApplicationNames();
                UpdateConnectionStatus(true);

                _ = Task.Run(async () => await ReceiveMessages());
            }
            catch (Exception ex)
            {
                throw new Exception($"Erreur de connexion WebSocket\n\nURL: {uri}\n\nDétails: {ex.Message}");
            }
        }

        private async Task LoadApplicationNames()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("X-Gotify-Key", clientToken);
                    var response = await client.GetAsync($"{serverUrl}/application");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var apps = JsonSerializer.Deserialize<List<GotifyApplication>>(json);
                        
                        applicationNames.Clear();
                        foreach (var app in apps)
                        {
                            applicationNames[app.id] = app.name;
                        }
                    }
                }
            }
            catch
            {
                // Si on ne peut pas charger les noms, on continuera avec les IDs
            }
        }

        private void DisconnectWebSocket()
        {
            try
            {
                cancellationTokenSource?.Cancel();
                webSocket?.Dispose();
                UpdateConnectionStatus(false);
            }
            catch { }
        }

        private async Task ReceiveMessages()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket.State == WebSocketState.Open && !cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var messageJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var message = JsonSerializer.Deserialize<GotifyMessage>(messageJson);
                        
                        // Injecter le nom de l'application si disponible
                        if (applicationNames.ContainsKey(message.appid))
                        {
                            message.ApplicationName = applicationNames[message.appid];
                        }

                        Dispatcher.Invoke(() =>
                        {
                            messages.Insert(0, message);
                            UpdateMessageCount();
                            ShowNotification(message);
                            PlayNotificationSound();
                        });
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"Erreur de réception: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    UpdateConnectionStatus(false);
                });
            }
        }

        private void ShowNotification(GotifyMessage message)
        {
            if (windowsNotificationsEnabled)
            {
                notifyIcon.BalloonTipTitle = message.Title ?? "Gotify";
                notifyIcon.BalloonTipText = message.Message ?? "";
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(5000);
            }
        }

        private void PlayNotificationSound()
        {
            if (soundNotificationsEnabled)
            {
                try
                {
                    SystemSounds.Beep.Play();
                }
                catch { }
            }
        }

        private void UpdateConnectionStatus(bool connected)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = connected ? "Connecté" : "Déconnecté";
                StatusIndicator.Fill = connected
                    ? new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#10B981"))
                    : new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString("#EF4444"));
            });
        }

        private void UpdateMessageCount()
        {
            MessageCountTextBlock.Text = messages.Count == 0 ? "Aucune notification" : 
                                        $"{messages.Count} notification{(messages.Count > 1 ? "s" : "")}";
            UpdateEmptyState();
        }

        private void ClearMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            messages.Clear();
            UpdateMessageCount();
        }

        private void ToggleSoundNotification_Click(object sender, RoutedEventArgs e)
        {
            soundNotificationsEnabled = SoundNotificationMenuItem.IsChecked == true;
        }

        private void ToggleWindowsNotification_Click(object sender, RoutedEventArgs e)
        {
            windowsNotificationsEnabled = WindowsNotificationMenuItem.IsChecked == true;
        }

        private void QuitApp_Click(object sender, RoutedEventArgs e)
        {
            QuitApplication();
        }

        private void ShowAbout_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.MessageBox.Show(
                "Gotify Client v1.0\n\n" +
                "Client Windows natif pour Gotify\n\n" +
                "Développé avec C# WPF\n" +
                "Licence MIT",
                "À propos",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }

    public class GotifyMessage : INotifyPropertyChanged
    {
        public int id { get; set; }
        public int appid { get; set; }
        public string message { get; set; }
        public string title { get; set; }
        public int priority { get; set; }
        public DateTime date { get; set; }
        
        public string ApplicationName { get; set; }
        
        private string _relativeTime;
        public string RelativeTime
        {
            get => _relativeTime;
            set
            {
                _relativeTime = value;
                OnPropertyChanged(nameof(RelativeTime));
            }
        }

        public string AppId => !string.IsNullOrEmpty(ApplicationName) ? ApplicationName : $"App #{appid}";
        public string Title => title ?? "Sans titre";
        public string Message => message ?? "";
        public string DateFormatted => date.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        
        public string PriorityColor
        {
            get
            {
                return priority switch
                {
                    >= 8 => "#EF4444", // Rouge - Urgent
                    >= 5 => "#F59E0B", // Orange - Important
                    >= 2 => "#3B82F6", // Bleu - Normal
                    _ => "#6B7280"     // Gris - Faible
                };
            }
        }
        
        public string PriorityIcon
        {
            get
            {
                return priority switch
                {
                    >= 8 => "❗",
                    >= 5 => "⚠️",
                    >= 2 => "ℹ️",
                    _ => "📌"
                };
            }
        }

        public GotifyMessage()
        {
            UpdateRelativeTime();
        }

        public void UpdateRelativeTime()
        {
            var now = DateTime.Now;
            var localDate = date.ToLocalTime();
            var diff = now - localDate;

            if (diff.TotalMinutes < 1)
                RelativeTime = "À l'instant";
            else if (diff.TotalMinutes < 60)
                RelativeTime = $"Il y a {(int)diff.TotalMinutes} min";
            else if (diff.TotalHours < 24)
                RelativeTime = $"Il y a {(int)diff.TotalHours}h";
            else if (diff.TotalDays < 7)
                RelativeTime = $"Il y a {(int)diff.TotalDays}j";
            else
                RelativeTime = localDate.ToString("dd/MM");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class GotifyApplication
    {
        public int id { get; set; }
        public string token { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }

    public class ConfigData
    {
        public string ServerUrl { get; set; }
        public string Token { get; set; }
    }
}
