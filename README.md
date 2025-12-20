# MorePlayers Mod for MIMESIS

Remove the 4-player limit in MIMESIS multiplayer sessions.

![Version](https://img.shields.io/badge/version-1.0.5-blue)
![Game](https://img.shields.io/badge/game-MIMESIS-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.6.1+-green)
![Status](https://img.shields.io/badge/status-working-brightgreen)

You can join the Discord server dedicated to the development of mods for the game Mimesis: https://discord.gg/gNVPrR2YyH

## 📖 Description

This mod patches the multiplayer player limit in MIMESIS, allowing more than 4 players to join a single session. The mod uses HarmonyX patches to modify server-side validation checks.

**Default limit:** 4 players  
**Modified limit:** 999 players (effectively unlimited)

### How It Works

The mod patches multiple validation points:
1. **Network Layer:** `FishySteamworks.Server.ServerSocket` - Steam networking limits
2. **Room Validation:** `VRoomManager.EnterWaitingRoom` - Server-side room entry checks  
3. **Member Count:** `VWaitingRoom.GetMemberCount()` - Player count validation

> ⚠️ **Important:** While the mod removes the technical limit, the actual number of players depends on:
> - Host's network bandwidth and latency
> - Steam P2P connection capabilities
> - Game performance (more players = more resource usage)

## 🎯 Who Needs This Mod?

### ✅ **ONLY THE HOST** needs to install this mod!

The mod patches **server-side validation** that happens on the host's game instance. Players joining the lobby **do NOT need** to install the mod.

**Installation:**
- **Host (lobby creator):** ✅ Must install mod
- **Joining players:** ❌ No mod needed

This makes it easy to play with friends - only the person hosting needs the mod!

---

## 🚀 Quick Start

```
1. Download MorePlayers.dll
2. Place in: <MIMESIS>/Mods/MorePlayers.dll
3. HOST creates lobby (mod installed)
4. Friends join (NO mod needed)
5. Enjoy 5+ player sessions! 🎉
```

**📌 Remember:** Only the HOST (lobby creator) needs the mod installed!

---

## ✨ Features

- ✅ Removes 4-player limit
- ✅ Patches server-side player count validation
- ✅ Logging for debugging
- ✅ No game file modifications required
- ✅ Easy to install and uninstall

## 📋 Requirements

- **MIMESIS** (Steam version)
- **[MelonLoader](https://github.com/LavaGang/MelonLoader/releases)** v0.6.1 or higher
- Windows OS
- .NET Framework 4.7.2 or higher

## 🤝 Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## ⚠️ Disclaimer

- This mod is not affiliated with or endorsed by the developers of MIMESIS
- Use at your own risk
- Online multiplayer modifications may violate terms of service
- The mod author is not responsible for any issues, bans, or data loss
- Always backup your save files before using mods

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.md) file for details.

## 🙏 Credits

- **Harmony** - [Harmony Patching Library](https://github.com/pardeike/Harmony)
- **MelonLoader** - [MelonLoader Mod Loader](https://github.com/LavaGang/MelonLoader)
- **MIMESIS** - Game by ReLUGames
- **FishySteamworks** - Steam integration for FishNet

## 📞 Support

- 🐛 [Report Issues](../../issues)
- 💬 [Discussions](../../discussions)
- 📧 Contact: andy@0c.md

---

**Enjoy playing with more friends! 🎮**
