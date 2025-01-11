# TwitchChat for Derail Valley

A mod that seamlessly integrates Twitch chat into your Derail Valley gameplay experience, providing real-time message display and chat interaction capabilities - especially useful for VR users!

## Features

### Core Functionality

- **Real-Time Chat Display**: View Twitch chat messages through in-game notifications
- **Secure Authentication**: Automated OAuth token handling for secure Twitch integration
- **Message Logging**: Detailed chat logs for post-stream review
- **Automated Messages**: Schedule periodic announcements to keep your chat informed

### Upcoming Features (In Development)

- Subscriber/Follower alerts and notifications
- Message throttling and combining for busy chats
- User list management (VIP, ignore, etc.)
- Integration with "Remote Dispatch" mod
- Colored announcement system for timed messages

## Installation

1. Download mod zip from NexusMods: [TwitchChatMod](https://www.nexusmods.com/derailvalley/mods/1069)
2. Extract the mod to your Derail Valley mods folder
3. Launch the game and open Unity Mod Manager (default: Ctrl + F10)
4. Configure your settings in the mod's settings panel

## Dependencies

1. Download and install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)
2. Follow the installation instructions and prompts

## Setup & Configuration

### Twitch Authentication

1. Enter your Twitch username in the mod settings
2. Click "Request Authorization Token"
3. Complete the OAuth authentication in your browser
4. Token is securely saved and valid for ~30 days
TODO: Improve websocket error/bad authentication response

### Settings Configuration

- Configure message duration and display preferences
- Set up to 5 timed messages with individual intervals
- Customize welcome messages and automated responses
- Debug options available in the expanded troubleshooting section

## VR Usage Notes

- Messages appear in a comfortable viewing position
- Multiple messages stack vertically

## Debug & Testing

Access advanced options by expanding the "Debug and Troubleshooting" section in settings:

- Toggle processing of own messages (disabled by default)
- Adjust debug logging levels
- Test message display and channel connection

## Support & Links

- [Report Issues](https://github.com/Nightwind416/derail-valley-twitch-chat-mod/issues)
- [Nexus Mods Page](https://www.nexusmods.com/derailvalley/mods/1069)
- [GitHub Repository](https://github.com/Nightwind416/Derail-Valley-Twitch-Chat-Mod)

## Version History

### 2.0.0 (December 20, 2024)

- Complete code refactor
- Direct WebSocket and HTTP client implementation
- Automated OAuth Token handling
- Temporary disable of automated messages during refactor

### 1.1.0 (November 29, 2024)

- Changed hard coded chat username to read from credentials file
- Push project to public repository

### 1.0.0 (November 29, 2024)

- Initial Release

## Donations

If you find this mod useful and want to support its development, appreciate any donations that may be given:

- [PayPal.me Donation Link](https://paypal.me/Nightwind416?country.x=US&locale.x=en_US)
- [Buy Me a Coffee](https://www.buymeacoffee.com/christophe1xf)
- [Ko-fi Support](https://ko-fi.com/A0A217PWSY)
