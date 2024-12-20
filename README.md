<h1 align="center">Derail Valley Twitch Chat</h1>

<p align="center">
A way to view Twitch chat messages from in game (especially useful for VR users).
</p>
<p align="center">
<a href="https://github.com/Nightwind416/derail-valley-twitch-chat-mod/issues">Report Bug or Request Feature</a>
</p>

## Table of Contents

- [About The Mod](#about-the-mod)
- [Dependencies](#dependencies)
- [Features](#features)
- [Twitch Credentials](#twitch-credentials)
- [Settings](#settings)
- [Messages](#messages)
- [Conflicts](#conflicts)
- [VR Note](#vr-note)
- [Future Plans](#future-plans)

## About The Mod

This mod allows streamers to receive chat in real-time through notifications and popups directly in the game. After downloading and unzipping to the Derail Valley Mods folder, begin a game and open the Unity Mod Manager window (default is Ctrl + F10).
TODO: Instructions on how to configure for mod users.

## Dependencies

### Unity Mod Manager

- Nexus link
- Author name

## Features

- Seamless Twitch Integration: Automatic login and virtually hands free operation, once a token is aquired and saved.
- Real-Time Chat Display: Incoming chat messages are displayed directly in the game.
  - Top left corner for 'flat screen' players and hovering a few feet out around eye level for VR players.
- Standard Messages: Welcome, Info, and Commands messages can be individually toggled and edited.
- Timed Messages: Edit and automatically send up to 10 unique announcements tat pre-determined intervals.
- Follower & Subscriber Alerts: Get notified when someone follows or subscribes.
- Message Logging: Keeps a detailed log of all chat interactions for post-stream review.

## Twitch Credentials

Use the "Request 

## Settings

- Display Duration: This will set how long each message stays visible, in seconds, before fading out.

## Automatic Messages (Temporarily disabled)

- Welcome Message: Will be sent to the channel
- Info Message: You can save up to 10 pre typed messages to send at pre-determined intervals
- Command Message: Each message can have a separate timer
- Set a timer to 0 to disable that specific message

## Conflicts

- Note if you are also using [Dispatcher (Continued)](https://www.nexusmods.com/derailvalley/mods/743)

## VR Note

- Message notifications display hovering 'roughly centered and straight out a short distance', similar to the tutorial popup notifications.
- Additional messages continue stacking below. This can be disruptive on active chat channels.

## Future Plans

- Add throttling and/or combining when many messages are coming in at once (mostly for VR disruption)
- Add ability to add usernames to a local 'ignore' list
- Re-add automated replies and timed messages
- Add various command responses and triggers
- See if the career manager can be utilized for deeper Twitch channel settings acces from 'in-game'
- See about creating own menu/ui, leveraging off the DerailValley UI

## Version History

## 2.0.0 20 Dec 2024

- Complete code refactor
-- Removed and replaced TwitchLib with direct coded WebSocket and HTTP clients
-- Added automatic OAuth Token request/validating
-- Temporarily disabled automated and timed messages during base code refactoring

## 1.2.0 6 Dec 2024 (internal only)

- Major refactor and expansion of settings and credentials info

### 1.1.0 29 Nov 2024

- Changed hard coded chat username to read from credentials file
- Push project to public repository

### 1.0.0 29 Nov 2024

- Initial Release
