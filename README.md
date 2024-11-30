<<<<<<< HEAD
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

This mod allows streamers to receive chat in real-time through notifications and popups directly in the game. After downloading and unzipping to the Derail Valley Mods folder, edit the credentials.json, twitchchat_settings.json, and twitch_messages.json as appropriate for your session.
TODO: Instructions on how to configure for mod users.

## Dependencies

### Unity Mod Manager

- Nexus link
- Author name

## Features

- Real-Time Twitch Chat Integration: Display messages from your Twitch chat directly in the game.
- Subscriber Alerts: Get notified when someone subscribes.
- Message Logging: Keeps a detailed log of all chat interactions for post-stream review.
- Automatic Chat Notifications: Display important announcements to chat periodically to keep your audience informed.
Seamless Twitch Connection: Easy setup using a credentials file for secure access.

## Twitch Credentials

Open and edit the credentials.json file:

- username - replace with your Twitch username
- token - replace with your Twitch token
- channel - replace with the Twitch channel name you want to join (typically the same as your username, but lowercase)

## Settings

Open and edit the settings.json file:

- message_duration - how long you want messages to display on screen (in seconds)

## Messages

Open and edit the twitch_messages.json file:

- The welcome message will display once the client logs into the channel
- You can save up to 10 pre typed messages to send at pre-determined intervals
- Each message can have a separate timer
- Set a timer to 0 to disable that specific message

## Conflicts

- Minor non-breaking conflict if you are also using [Dispatcher (Continued)](https://www.nexusmods.com/derailvalley/mods/743)
- If you hold or look at a Job, any current Twitch messages will be removed.
Any new Twitch messages that come in while holding or looking at a Job will only show for about 1-2 seconds.
- Twitch messages received while not looking at or holding a Job are unaffected.

## VR Note

- Message notifications display hovering 'roughly centered and straight out a short distance', similar to the tutorial popup notifications.
- Additional messages continue stacking below. This can be disruptive on active chat channels.

## Future Plans

- Leverage Unity Mod Manager in-game settings window
- Add throttling and/or combining when many messages are coming in at once (mostly for VR disruption)
- Add ability to add usernames to a local 'ignore' list

## Version History

### 1.1.0 29 Nov 2024

- Changed hard coded chat username to read from credentials file
- Push project to public repository

### 1.0.0 29 Nov 2024

- Initial Release
=======
# Derail-Valley-Twitch-Chat-Mod
This mod allows streamers to interact with their chat in real-time through notifications directly in the game.
>>>>>>> origin/main
