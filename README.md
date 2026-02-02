# Gotify Windows Client

Native Windows client in C# WPF to receive notifications from a Gotify server.

## Features

- ✅ WebSocket connection to Gotify
- ✅ Real-time notification reception
- ✅ Native Windows notifications (balloon tips)
- ✅ System tray icon
- ✅ Automatic minimize-to-tray
- ✅ Received message list
- ✅ Automatic configuration persistence

## Requirements

- Windows 10/11
- .NET 6.0 Runtime or SDK
- A Gotify server with a client token

## Installation

### Option 1: Build from source

1. Install the [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
2. Open a terminal in the project directory
3. Build the project:

```bash
dotnet build -c Release
```

The executable will be in: `bin/Release/net6.0-windows/GotifyClient.exe`

### Option 2: Publish a self-contained build

To create a standalone executable that does not require .NET to be installed:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable will be in: `bin/Release/net6.0-windows/win-x64/publish/GotifyClient.exe`

## Usage

1. Launch `GotifyClient.exe`
2. Configure the connection:
   - **Server URL**: The URL of your Gotify server (e.g. `https://gotify.example.com`)
   - **Client Token**: Your client application token (create it in the Gotify UI)
3. Click "Connect"
4. The app connects and starts receiving notifications

### Get a client token

1. Sign in to your Gotify web UI
2. Go to **Applications**
3. Create a new application
4. Copy the generated **token**
5. Paste it into the "Client Token" field in the app

## Configuration

The configuration (server URL and token) is saved automatically to `gotify_config.json` in the same folder as the executable.

## Behavior

- **Minimize to tray**: Enable this option to minimize the app to the system tray instead of the taskbar
- **Double-click the tray icon**: Restores the window
- **Right-click the tray icon**: Shows the context menu (Open/Quit)
- **Clear messages**: Removes the displayed message history (does not delete messages on the server)

## Troubleshooting

### The app cannot connect

- Ensure the server URL is correct (including `https://` or `http://`)
- Verify the token is valid
- Confirm the Gotify server is reachable from your network
- Ensure the WebSocket port is open (usually the same as HTTP/HTTPS)

### Notifications do not appear

- Confirm Windows notifications are enabled in system settings
- Test by sending a message from the Gotify web UI

### SSL certificate error

If you use a self-signed certificate, you may need to update the code to accept invalid certificates (not recommended for production).

## Technologies used

- C# / .NET 6
- WPF (Windows Presentation Foundation)
- WebSocket for real-time connectivity
- System.Windows.Forms for the tray icon

## License

MIT License - Free to use and modify

## Support

To report a bug or request a feature, open an issue on the GitHub repository.
