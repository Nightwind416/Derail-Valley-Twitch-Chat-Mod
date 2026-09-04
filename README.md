# TwitchChat for Derail Valley

A mod that seamlessly integrates Twitch chat into your Derail Valley gameplay experience, providing real-time message display and chat interaction capabilities - especially useful for VR users!

## Features

### Core Functionality

- **Real-Time Chat Display**: View Twitch chat messages through in-game display panels (and popup notifications)
- **Secure Authentication**: Automated OAuth token handling for secure Twitch integration
- **Message Logging**: Detailed chat logs for post-stream review
- **Automated Messages**: Schedule periodic announcements to keep your chat informed
- **Display Panels**: Wide, Large, Medium, and Small sized message display panels
- **Color Customization**: Ability to customize panel, section, and button coloring

### Upcoming Features (In Development)

- Subscriber/Follower alerts and notifications
- Message throttling and combining for busy chats
- User list management (VIP, ignore, etc.)
- Integration with "Remote Dispatch" mod
- Colored announcement system for timed messages
- Display panel scrolling in VR
- Choose which licenses to replace with TwitchChat mod menus (currently 'hard coded')

## Installation

1. Download mod zip from NexusMods: [TwitchChat](https://www.nexusmods.com/derailvalley/mods/1069)
2. Extract the mod to your Derail Valley mods folder
3. Launch the game and open Unity Mod Manager (default: Ctrl + F10)
4. Configure your Twitch username and adjust any of the 'pre canned messages'
    Note: you can also directly edit the settings.xml file in the TwitchChat mod folder

## Dependencies

1. Download and install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)
2. Follow the installation instructions and prompts

## Setup & Configuration

### Display and Menu Panel Setup

The menus and chat displays live on world-space panels. There are three kinds of panel host:

- **Cab display** - a panel parented to the locomotive you are in, so it rides along with the cab
-- Press the place key (default F7), or use the "Place Display" button on any Main panel, and the panel appears where you are looking
-- The position is remembered per locomotive type and restored automatically the next time you board that type
-- The toggle key (default F8), or the "Toggle Display" button, hides and shows it without moving it
-- Keys, placement distance and scale are set in the Unity Mod Manager menu
- **Wrist panel** (VR only) - a smaller copy of the menus attached to a controller, glance at it like a watch
-- Choose the hand, size, offset and rotation in the Unity Mod Manager menu; changes apply live
-- Its "Place Display" button is the VR way to summon the cab display without a keyboard
- **License papers** (legacy mode, can be turned off in the Unity Mod Manager menu) - menus replace the following licenses:
-- LicenseTrainDriver, LicenseShunting, LicenseLocomotiveDE2 - given at the start of a new game
-- LicenseMuseumCitySouth, LicenseFreightHaul, LicenseDispatcher1 - purchase/own the license
-- As these menus ride on licenses, they can be attached to sticky tape anywhere sticky tape can be placed

- Every host shows the same panels and remembers which panel it last showed
- Display Panels:
-- Wide - Little wider than the large, but about 1/3 in height
-- Large - Approx same size as the DE2 back window
-- Medium - About double the license size
-- Small - Same size as the license
- Config Panels
-- Config1 - Customize background panel and section coloring
-- Config2 - Customize button coloring and reset color customizations
- Menu Panels
-- Main - Access all other panels from here, plus the Place Display and Toggle Display buttons
-- Notifications - Enable/Disable the notification popups when new messages are received and set duration
-- Standard Messages - Enable/Disable your automatic Connect/Disconnect messages
-- Command Messages - Enable/Disable the !info and !command ...commands
-- Timed Messages - Enable/Disable the timed messages system
-- Debug - Set debug level, several 'debug and testing' related buttons

- Buttons can be interacted with in both VR and non-VR modes
- Top left panel buttons will 'minimize' the displayed panel
- Top right panel buttons will return to the 'Main' panel (does nothing 'on' the Main panel)
- Each 'click' of the Notification duration slider in VR mode will advance approx 10%, then reset after max

### Twitch Authentication

1. Enter your Twitch username using the UnityModManager menu, or edit the settings.xml
    Note: You have to run the game and enable the mod for the settings file to initially generate
2. Click "Request Authorization Token"
3. Complete the OAuth authentication in your default browser, outside the game
    Note: If playing in VR, you need to take your headset off or be able to 'alt tab' to your PC to authorize the connection
    Note: If you wait too long, the request will time out. If clicking the button does not work, restart the game and try again.
4. Token is securely saved and valid for ~30 days
5. If you are SURE your username is set correctly in the UMM menu, try restarting the game to 'hard reload' from the settings
TODO: Improve websocket error/bad authentication response

### UnityModManager Configurations

- Configure message duration and display preferences
- Set up to 5 timed messages with individual intervals
- Customize welcome messages and automated responses
- Debug options available in the expanded troubleshooting section

## VR Usage Notes

- Notification popups may be an annoyance. Use the Notifications panel to disable the popups at will.
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

### 3.3.0 (September 4, 2026)

- New cab display: a panel placed where you look that rides along with the locomotive and remembers its position per locomotive type
- New wrist panel for VR: the menus on your forearm, including a button to summon the cab display
- License paper menus are now an optional legacy mode. Licenses are found through the game's item system instead of by object name, so they work again on current game builds
- Unity Mod Manager menu gains a section for the hotkeys, placement distance, and wrist panel tuning

### 3.2.0 (September 4, 2026)

- Fixed the mod failing to enable on current game builds (the game's notification API gained new optional parameters; the mod now tolerates such changes)
- WebSocket messages are reassembled from all frames, so long chat messages are no longer truncated or dropped
- Twitch "session reconnect" requests are honoured, and lost connections retry with backoff instead of giving up
- Connect/disconnect chat announcements are only sent for manual connects and disconnects, not automatic reconnects
- The "Last Message Sent" status for timed messages now updates
- Settings changes made from the in-game panels are written to disk after a short delay instead of on every slider tick
- Received chat messages are now written to the Messages log

### 3.1.0 (January 16,2025)

- Color Customization settings added
-- 2 new 'Config' panels added with options to change the panel background, section, and button coloring
-- Reset buttons to 'reset' the colors back to 'default'

### 3.0.0 (January 11, 2025)

- Another major code overhaul to introduce:
-- Full in-game message panel displays
-- VR control of display and various menu panels
-- Menus 'replace' specific licenses and can be attached to locos with stick tape

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
