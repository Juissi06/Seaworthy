# Seaworthy

THIS MOD IS STILL IN ALPHA
Seaworthy is the sailing overhaul Valheim never got: everything the popular sailing mods do, in one mod that looks like part of the game. Trim the sail yourself for extra speed, level up a Sailing skill, drop anchor anywhere, push off beaches, see more of the coast and steer for a map pin with a proper helm panel. Leave it all alone and your ship sails exactly like vanilla.

Installation:
- Mod managers; automatic install
- Manual: Put the DLL file to \Steam\steamapps\common\Valheim\BepInEx\plugins
- Multiplayer: everyone on the ship should have it, and install it on the server too so the server's settings apply to everyone

Main features
- Helm panel: compass with your heading, the wind and your course, plus speed, sail mode, wind strength, sail trim and anchor. Built with Valheim's own UI style
- Sail trim: hold Left Shift and use A/D at the helm to turn the sail. Find the green zone and you sail up to 15% faster than vanilla, miss it and you're slower
- Gusts: the wind wanders and surges, more in storms, so trimming keeps you busy on long trips (only when you trim yourself)
- Sailing skill: a new skill that levels while you steer, up to 20% faster sailing and rowing at level 100, and skilled sailors trimming by hand can sail closer to the wind
- Anchor: press G at the helm. The ship stops and stays put, even when you leave it. Rope and splash included
- Crew rowing: bring friends, every extra player aboard rows 10% faster
- Push off: stuck on a beach? Hold B at the helm and the crew shoves you back towards deep water
- Lookout: see more of the map from a ship, longships see twice as far
- Ship lanterns: a warm glow at the stern after dark
- Ship names: name your ship, everyone sees it
- Course: Left Alt + click a pin on the map and the helm panel shows the way, the distance and when you'll get there
- Wave damage setting: 1 = vanilla, lower it if the waves annoy you
- Cargo weight (optional): turn it on and a full hold slows the ship down
- The server decides the rules, so nobody sails faster with their own config
- Every feature can be turned off, and the whole mod can be switched off in game

Keys (all changeable)
- Left Shift + A/D at the helm: trim the sail (gamepad: hold LT + stick)
- G at the helm: drop / raise anchor
- B (hold) at the helm of a beached ship: push off
- Left Alt + click on the large map: set / clear course
- Left Shift + E at the helm: name your ship

Works with other mods
- If you already use a mod that names ships, reveals more map at sea, adds a sailing skill or anchors, Seaworthy turns its own version off (you can turn that off in the config)
- Made to run alongside Norse Sense (same look, no overlapping features)

Requirements:
-BepInEx
-Valheim Configuration Manager (optional, but highly recommended for config)

Config file: BepInEx/config/Seaworthy.cfg

## Building from source
Requires the .NET SDK. The project references the game's DLLs from your Valheim install:

```
dotnet build Seaworthy/Seaworthy.csproj -c Release -p:ValheimDir="D:\Games\Steam\steamapps\common\Valheim"
```

Every build copies the DLL into `BepInEx/plugins`. Release builds also write upload-ready zips for Thunderstore and Nexus to `dist/`.
