# Seaworthy

THIS MOD IS STILL IN ALPHA
Seaworthy makes sailing a skill instead of holding W. Trim the sail yourself to catch the wind properly and you'll sail faster than vanilla, trim it badly and you'll crawl. Drop anchor to stop and hold your ship anywhere. Leave the sail alone and it sails exactly like vanilla.

Installation:
- Mod managers; automatic install
- Manual: Put the DLL file to \Steam\steamapps\common\Valheim\BepInEx\plugins
- Everyone on the ship should have it: the player who runs the ship's physics decides how it sails

Main features
- Sail trim: at the helm hold Left Shift and use A/D to turn the sail instead of the rudder
- Trim gauge shows your sail angle, the best angle for the current wind (green) and how much speed you're getting
- A perfectly trimmed sail gives up to 15% more speed than vanilla (configurable, 0 = never faster than vanilla)
- Furl the sail and trim goes back to automatic, so you're never stuck with a bad angle
- Anchor: press G at the helm to drop or raise it. The ship slows to a stop and stays put, even when you leave the helm
- Turn the whole mod off in the config and it's vanilla sailing again, no restart needed

Requirements:
-BepInEx
-Valheim Configuration Manager (optional, but highly recommended for config)

## Building from source
Requires the .NET SDK. The project references the game's DLLs from your Valheim install:

```
dotnet build Seaworthy/Seaworthy.csproj -c Release -p:ValheimDir="D:\Games\Steam\steamapps\common\Valheim"
```

Every build copies the DLL into `BepInEx/plugins`. Release builds also write upload-ready zips for Thunderstore and Nexus to `dist/`.
