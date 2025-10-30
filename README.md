# MorePlayers Mod for MIMESIS

Remove the 4-player limit in MIMESIS multiplayer sessions.

![Version](https://img.shields.io/badge/version-1.0.0-blue)
![Game](https://img.shields.io/badge/game-MIMESIS-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.6.1+-green)

## 📖 Description

This mod patches the multiplayer player limit in MIMESIS, allowing more than 4 players to join a single session. The mod uses Harmony patches to override the `_maximumClients` field in `FishySteamworks.Server.ServerSocket` class.

**Default limit:** 4 players  
**Modified limit:** 999 players

> ⚠️ **Note:** While the mod removes the technical limit, the actual number of players your session can handle depends on your network configuration, server performance, and Steam P2P capabilities.

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

## 🔧 Installation

### Step 1: Install MelonLoader

1. Download the latest MelonLoader installer from [GitHub Releases](https://github.com/LavaGang/MelonLoader/releases)
2. Run the installer and select your MIMESIS installation folder:
   - Default Steam location: `C:\Program Files (x86)\Steam\steamapps\common\MIMESIS`
   - Or right-click MIMESIS in Steam → Manage → Browse local files
3. Click Install
4. Launch the game once to let MelonLoader initialize (game will close automatically)

### Step 2: Install the Mod

1. Download `MorePlayers.dll` from [Releases](../../releases)
2. Copy `MorePlayers.dll` to your MIMESIS Mods folder:
   ```
   <MIMESIS_Install_Folder>/Mods/MorePlayers.dll
   ```
3. Launch the game

### Verify Installation

Check if the mod loaded successfully:
1. Navigate to `<MIMESIS_Install_Folder>/MelonLoader/Latest.log`
2. Look for these lines:
   ```
   [MorePlayers] MorePlayers Mod Loaded!
   [MorePlayers] Applying Harmony patches...
   [MorePlayers] Harmony patches applied successfully!
   ```

## 🎮 Usage

Once installed, the mod works automatically:

1. **Host a game** - The player limit is now 999
2. **Check the log** - When creating a lobby, you'll see:
   ```
   [MorePlayers] SetMaximumClients(4) called, setting to 999 instead
   [MorePlayers] GetMaximumClients() called, returning 999
   ```
3. **Invite players** - You can now have more than 4 players in your session!

## 🔍 How It Works

The mod uses [HarmonyX](https://github.com/BepInEx/HarmonyX) to patch two internal methods:

### Patch 1: `GetMaximumClients()`
Intercepts calls to get the maximum client count and returns 999 instead of the default 4.

### Patch 2: `SetMaximumClients(int value)`
Prevents the game from setting a limit below 999 by directly modifying the private field `_maximumClients`.

**Target Class:** `FishySteamworks.Server.ServerSocket`

## 🐛 Troubleshooting

### Mod doesn't load (0 Mods loaded)

**Check:**
```powershell
# Verify the file exists
Test-Path "<MIMESIS_Folder>/Mods/MorePlayers.dll"
```

**Solutions:**
- Ensure MelonLoader is properly installed
- Unblock the DLL: Right-click → Properties → Check "Unblock" → Apply
- Make sure the file is in the correct `Mods` folder
- Restart the game

### Harmony patch errors in log

If you see errors like:
```
HarmonyLib.HarmonyException: Patching exception in method...
```

**Possible causes:**
- Game was updated and code structure changed
- Conflict with another mod
- Corrupted mod file

**Solutions:**
- Download the latest version of the mod
- Try disabling other mods temporarily
- Check the [Issues](../../issues) page

### Game crashes on startup

1. Remove the mod temporarily:
   ```powershell
   del "<MIMESIS_Folder>/Mods/MorePlayers.dll"
   ```
2. Check the last lines in `MelonLoader/Latest.log` before the crash
3. Report the issue with the log file

### Players still can't join after 4

**Possible reasons:**
- Steam P2P connection limits
- Host's network configuration (NAT, firewall)
- Additional client-side checks (not yet patched)
- Game server browser limitations

**Check the log** for messages like:
```
[MorePlayers] GetMaximumClients() called, returning 999
```
If you see this, the mod is working, but there might be other limitations.

## 🏗️ Building from Source

### Prerequisites
- Visual Studio 2019+ or MSBuild
- .NET Framework 4.7.2 SDK

### Build Steps

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/mimesis-moreplayers.git
   cd mimesis-moreplayers
   ```

2. Copy game assemblies to `Libs/` folder:
   ```
   Libs/
   ├── Assembly-CSharp.dll      (from MIMESIS_Data/Managed)
   ├── UnityEngine.dll
   ├── UnityEngine.CoreModule.dll
   ├── netstandard.dll
   ├── MelonLoader.dll          (from MelonLoader/net35)
   └── 0Harmony.dll
   ```

3. Build the project:
   ```powershell
   MSBuild.exe TestMod.csproj /p:Configuration=Release
   ```

4. Output will be in `Output/MorePlayers.dll`

## 📝 Changelog

### Version 1.0.0
- Initial release
- Patches `GetMaximumClients()` and `SetMaximumClients()`
- Sets player limit to 999
- Logging support for debugging

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
