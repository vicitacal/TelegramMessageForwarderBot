# Telegram Message Forwarder

A Telegram bot that forwards messages from configured chats to you via the Bot API, with optional whitelist/blacklist filtering by keywords.

## How It Works

- **Your user account** (MTProto) receives messages from the groups/channels you add as sources.
- **Your bot** (Bot API) is the only destination: you talk to the bot to configure forwarding and receive forwarded messages.
- Only messages from **configured source chats** are considered. Within those, filtering uses **whitelist** (always forward if any word matches) and **blacklist** (never forward if any word matches). Whitelist overrides blacklist; if neither matches, the message is forwarded (default forward).
- **Access control**: only allowed user IDs can send commands to the bot and receive forwarded messages. By default the allowed list contains only the **bot owner** (set via `Telegram.BotOwnerId` or the first user who sends `/start`). The owner manages users with `/users list`, `/users add <user_id>`, `/users remove <user_id>`.

## Requirements

- .NET 8 SDK
- Telegram Bot Token ([@BotFather](https://t.me/BotFather))
- Telegram API credentials for your user account ([my.telegram.org](https://my.telegram.org))

## Environment Setup

All secrets are read from environment variables. Copy `env.example` to `.env` (or set variables another way) and fill in real values.

| Variable | Required | Description |
|----------|----------|-------------|
| `Telegram.BotToken` | Yes | Bot token from @BotFather |
| `Telegram.ApiId` | Yes | API ID from my.telegram.org |
| `Telegram.ApiHash` | Yes | API hash from my.telegram.org |
| `Telegram.PhoneNumber` | Yes | Your account phone (e.g. `+1234567890`) |
| `Telegram.VerificationCode` | First login only | Code sent to Telegram on first run |
| `Telegram.Password` | If 2FA | Your cloud password |
| `Telegram.SessionPathname` | No | Session file path (optional) |
| `Telegram.BotOwnerId` | No | Your Telegram user ID; only this user (and users added via `/users add`) can use the bot. If unset, the first user who sends `/start` becomes the owner. |

**Windows (PowerShell):**

```powershell
$env:Telegram.BotToken = "YOUR_BOT_TOKEN"
$env:Telegram.ApiId = "YOUR_API_ID"
$env:Telegram.ApiHash = "YOUR_API_HASH"
$env:Telegram.PhoneNumber = "+1234567890"
```

**Linux / macOS:**

```bash
export Telegram.BotToken="YOUR_BOT_TOKEN"
export Telegram.ApiId="YOUR_API_ID"
export Telegram.ApiHash="YOUR_API_HASH"
export Telegram.PhoneNumber="+1234567890"
```

## Bot Commands

Send these to your bot in a private chat:

| Command | Description |
|---------|-------------|
| `/start` | Register this chat for receiving forwarded messages (and become owner if none set) |
| `/help` | Show all commands |
| `/users list` | List allowed user IDs |
| `/users add <user_id>` | Allow a user to use the bot (owner only) |
| `/users remove <user_id>` | Revoke a user's access; owner cannot be removed |
| `/sources list` | List configured source chats |
| `/sources add <chat_id>` | Add a source chat to forward from |
| `/sources remove <chat_id>` | Remove a source chat |
| `/whitelist list/add/remove <chat_id> [words...]` | Whitelist words (always forward when any match) |
| `/blacklist list/add/remove <chat_id> [words...]` | Blacklist words (never forward when any match) |

Source `chat_id` values are the numeric IDs of the groups/channels you want to monitor (e.g. from the MTProto client or logs when the app receives messages).

## Build and Run

```bash
dotnet restore
dotnet build
dotnet run
```

Or publish and run the executable:

```bash
dotnet publish -c Release -o ./publish
./publish/TelegramMessageForwarder
```

On first run, log in with your Telegram account when prompted (set `Telegram.VerificationCode` and, if needed, `Telegram.Password` for that run). The session is saved so you usually do not need them again.

## Architecture

The solution follows Clean Architecture. See [ARCHITECTURE.md](ARCHITECTURE.md) for layers and responsibilities.

## Logging

Logging uses NLog. Console and file targets are configured in `NLog.config`. Log files are written under the `logs/` directory.
