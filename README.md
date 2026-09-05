# TwitchChat for Derail Valley

A mod that seamlessly integrates Twitch chat into your Derail Valley gameplay experience, providing real-time message display and chat interaction capabilities - especially useful for VR users!

## Features

### Core Functionality

- **Real-Time Chat Display**: View Twitch chat messages through in-game display panels (and popup notifications)
- **Authentication You Can Audit**: OAuth handled directly between you and Twitch, asking for only the four permissions the mod actually uses, revocable from inside the game
- **Message Logging**: Detailed chat logs for post-stream review
- **Automated Messages**: Schedule periodic announcements to keep your chat informed
- **Displays You Size Yourself**: Up to 5 panels per locomotive, each dragged to whatever size suits it
- **Color Customization**: Ability to customize panel, section, and button coloring

### Upcoming Features (In Development)

- Subscriber/Follower alerts and notifications
- Message throttling and combining for busy chats
- User list management (VIP, ignore, etc.)
- Integration with "Remote Dispatch" mod
- Colored announcement system for timed messages
- Display panel scrolling in VR

## Installation

1. Download mod zip from NexusMods: [TwitchChat](https://www.nexusmods.com/derailvalley/mods/1069)
2. Extract the mod to your Derail Valley mods folder
3. Launch the game and open Unity Mod Manager (default: Ctrl + F10)
4. Connect your Twitch account from the in-game Authentication panel, and adjust any of the 'pre canned messages'
    Note: you can also directly edit the settings.xml file in the TwitchChat mod folder

## Dependencies

1. Download and install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)
2. Follow the installation instructions and prompts

## Setup & Configuration

### Display and Menu Panel Setup

The menus and chat displays live on world-space panels. There are two kinds of panel host:

- **Cab displays** - up to 5 panels per locomotive type, parented to the locomotive you are in so they ride along with the cab
-- Press the place key (default F7), or use the "Place Display" button on any Main panel, and another display appears where you are looking
-- Each display keeps its own position, size, and chosen panel. The whole set is restored automatically the next time you board that locomotive type
-- The toggle key (default F8), or the "Toggle Display" button, hides and shows them all without moving anything
-- In VR, faint grab bars run along all four edges of every display:
--- Squeeze the **grip** on a bar to carry the whole display around, exactly like holding the old license papers
--- Squeeze the **trigger** on a bar to drag that one edge in or out and resize the display. The opposite edge stays where it is, and chat panels simply show more or fewer lines rather than stretching the text
--- Bars light up blue when a hand is in reach, green while carrying, amber while resizing, and go dim grey when both locks are on
-- Outside VR, drag any bar with the mouse to resize the display the same way. Placement is still the place key
-- Panels have no size of their own: a display stays the size you dragged it to whichever panel it shows, and new displays start at a sensible default
-- The **Displays** panel lists every display in the current locomotive, with a Move and a Size lock for each, and a Close button
-- The **x** button in the top right corner of any display closes that display and forgets its saved slot
-- Keys, placement distance and scale are set in the Unity Mod Manager menu, where the grab bars can also be turned off
- **Wrist panel** (VR only) - a smaller copy of the menus attached to a controller, glance at it like a watch
-- Choose the hand, size, offset and rotation in the Unity Mod Manager menu; changes apply live
-- Its "Place Display" button is the VR way to summon the cab display without a keyboard

- Every host shows the same panels and remembers which panel it last showed
- Chat Panel
-- Shows the incoming Twitch chat. How much of it you can read is decided by how big you have dragged the display, so there are no fixed Wide/Large/Medium/Small variants to pick between any more
- Config Panels
-- Config1 - Customize background panel and section coloring
-- Config2 - Customize button coloring and reset color customizations
- Menu Panels
-- Main - Access all other panels from here, plus the Place Display and Toggle Display buttons
-- Authentication - Connect or disconnect your Twitch account, and see exactly what is being authorized
-- Notifications - Enable/Disable the notification popups when new messages are received and set duration
-- Standard Messages - Enable/Disable your automatic Connect/Disconnect messages
-- Command Messages - Enable/Disable the !info and !command ...commands
-- Timed Messages - Enable/Disable the timed messages system
-- Displays - List the displays in this locomotive, lock each one's position or size, and close the ones you are done with
-- Debug - Set debug level, several 'debug and testing' related buttons

- Buttons can be interacted with in both VR and non-VR modes
- Top left panel buttons will 'minimize' the displayed panel
- Top right panel buttons return to the 'Main' panel (does nothing 'on' the Main panel), and close the display on cab displays
- Each 'click' of the Notification duration slider in VR mode will advance approx 10%, then reset after max

### Twitch Authentication

There is nothing to type and no username to get wrong. Open the **Authentication** panel from the
Main panel and follow it:

1. Click "Connect Twitch Account". A screen appears listing exactly what you are about to approve
2. Click "Continue to Twitch". The panel shows an eight-character code, for example `YLHWFTKX`
3. On any device — a phone works, so you can stay in VR — go to
   [twitch.tv/activate](https://www.twitch.tv/activate), sign in, and enter the code
4. The panel switches to "Connected as *yourname*" on its own within a few seconds

The code is good for 30 minutes. If it expires, press the button again for a new one.

Your account name is read back from Twitch once you are connected, so it is a result of connecting
rather than something you configure.

Connections last indefinitely. The mod holds a refresh token and renews its access quietly in the
background, so the roughly monthly re-authorization the old versions needed is gone. If a
connection ever does lapse, the Authentication panel says so and one button gets you a new code.

TODO: Improve websocket error/bad authentication response

#### What you are authorizing

You approve this on Twitch's own site, so the mod never sees your Twitch password, and the mod
never opens a browser or a local web server on your PC. The same list below is shown in game,
before anything is sent to Twitch. The mod asks for four permissions, and nothing else:

| Permission | Twitch scope | Used for |
| --- | --- | --- |
| Read the messages in your chat | `user:read:chat` | Showing chat on your in-game panels |
| Send chat messages as you | `user:write:chat` | The `!info` / `!commands` replies and any timed messages you set up |
| Send whispers as you | `user:manage:whispers` | Only command replies you have set to whisper |
| Post announcements in your chat | `moderator:manage:announcements` | Only timed messages you have coloured blue |

It does **not** ask to read your email address, see your subscribers, followers or revenue, change
your stream, or follow or subscribe to anything as you.

#### Where your access token lives

Your tokens are written to `Settings.xml` in the mod's folder, on your PC and nowhere else. They are
encrypted with Windows DPAPI under your own Windows account, so another user on the same machine
cannot read them out of the file. The Authentication panel says which storage is actually in use on
your system: if the game's runtime does not provide DPAPI, the mod says so plainly rather than
claiming encryption it did not perform.

Tokens are sent only to Twitch, over HTTPS. There is no server behind this mod, and the mod author
has no way to see your token, your chat, or your game session. Tokens are deliberately kept out of
the mod's log files, so sharing a log when reporting a bug does not hand over your account.

If you want to check any of this, the code that talks to Twitch is in
[`OAuthManager.cs`](OAuthManager.cs), [`TokenStore.cs`](TokenStore.cs) and
[`TwitchEventHandler.cs`](TwitchEventHandler.cs).

#### Withdrawing access

Use **Sign Out & Revoke Access** on the Authentication panel. That tells Twitch to invalidate the
token immediately and deletes the local copy. You can also revoke it from Twitch's side at any time
under [Twitch Connection Settings](https://www.twitch.tv/settings/connections) — find
*DerailValleyChatMod* under "Other Connections" and click Disconnect.

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

### 3.4.0 (September 4, 2026)

- A locomotive type can now carry up to 5 displays instead of one. Each keeps its own position, size and chosen panel, and the whole set comes back when you board that locomotive type again
- Displays are resized by hand in VR: squeeze the trigger on an edge bar and drag that edge. The grip still carries the whole display. Outside VR, drag the same bars with the mouse
- The four sized chat panels (Wide, Large, Medium, Small) are now a single Chat panel, since a display is whatever size you drag it to. Panel size presets are gone with them, and a chat message now costs one entry per display instead of four
- New Displays panel listing everything placed in the current locomotive, with a Move lock and a Size lock per display, plus a Close button. Every cab display also gets an x in its top right corner
- Connecting to Twitch is now done entirely from inside the game. The Authentication panel shows a short code to enter at twitch.tv/activate on any device, so VR players no longer have to remove the headset or alt-tab to a browser
- Connections no longer expire roughly monthly; the mod refreshes its own access in the background
- The mod now asks Twitch for four permissions instead of nine. It no longer requests access to your email address, the legacy IRC chat scopes, or the bot scopes, none of which it used
- Added a Sign Out & Revoke Access button, which revokes the token at Twitch and deletes the local copy
- The Authentication panel spells out what each permission allows, what the mod cannot do, and where your access token is kept, before anything is sent to Twitch
- Fixed the access token being written to the mod debug log on every authorization
- Tokens are now encrypted at rest with Windows DPAPI where the runtime supports it, instead of being merely base64 encoded
- The Twitch username setting is gone. Your account is identified from the token itself
- The license paper menus are gone. The cab display and wrist panel replace them, so the mod no longer touches your license items or the sticky tape they were stuck to
- The cab display can be grabbed and moved by hand in VR: grab bars along its four edges light up as a hand nears them, and squeezing the grip carries the display until you let go
- Fixed the panels interfering with locomotive physics. Their VR colliders had no body of their own, so the game folded them into the rigidbody of the locomotive the display was parented to, which could shift its mass and shove it around. All panel colliders are triggers now, and the grab bars use none at all

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
