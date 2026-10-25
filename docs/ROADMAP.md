# Roadmap

Seaworthy is one sailing overhaul that brings together what the popular sailing mods do separately, in one consistent, vanilla-friendly package:

| Popular mod | What players like | Seaworthy's take |
|---|---|---|
| Sailing Skill | sailing gets better the more you sail | **Sailing skill**: levels while you sail, more sail force and rowing speed |
| Sailing reveals more map | see more of the coast from a ship | **Lookout**: bigger map reveal radius at sea, grows with ship size |
| Tailwind Sailing | less fighting the wind, push off beaches | **Trim** rewards skill instead of cheating the wind; **Push off** for beached ships |
| Sailing Overhauls | wind that feels alive | **Storm winds**: stronger wind and gusts in bad weather |
| Helm HUD mods | know what the ship is doing | **Helm panel**: speed, compass, wind, trim and anchor in one panel |
| BoatAdditions | anchors | **Anchor** with rope and splash |

Rules for every feature:
- A ship nobody touches sails like vanilla. Bonuses come from skill, not from a free speed multiplier.
- Every feature can be switched off. The server decides the rules (Server → Enforce on clients).
- One look: Valheim's own UI art and fonts, the same style as Norse Sense.

## Done
Sailing
- [x] Manual sail trim (Left Shift + A/D, gamepad LT + stick) with a perfect-trim bonus
- [x] Gusts and storm winds that shift the best trim angle (manual trim only)
- [x] Pointing higher into the wind with Sailing skill; "Head to wind" warning
- [x] Sailing skill: levels at the helm, more sail force and rowing speed
- [x] Crew rowing: +10% rowing per extra player aboard (up to 3)

At the helm
- [x] Helm panel: ship name, compass with heading, wind and course, speed, sail mode, wind, trim, anchor, hull, hold, beached
- [x] Course to a pin or spot (Left Alt + click on the map), with ETA and a temporary pin
- [x] First-helm tip listing the controls

Ship
- [x] Anchor (G), rope and splash for everyone
- [x] Push off a beached ship (hold B)
- [x] Ship names (Left Shift + E at the helm)
- [x] Ship lanterns after dark
- [x] Lookout: bigger map reveal at sea, scaled by ship size

Options (vanilla by default)
- [x] Wave damage multiplier
- [x] Cargo weight slows ships (off by default)

Under the hood
- [x] Server-enforced settings, dedicated servers supported
- [x] Compatibility: steps aside for other ship-name, map-reveal, skill and anchor mods
- [x] Whole mod can be switched off in game; the Sailing skill is kept in saves either way

## Next
- Tune from play: trim speed, bonus, anchor hold, gust size, push-off strength (skill gain slowed to 1 raise per 15 m after the first test)
- Screenshots and the Nexus page

## Later
- Hull leaks you patch at sea
- Ship upgrades from biome materials

## Notes
- The ship's physics run on one client (the ship's owner, a player on board). Controls go helmsman → owner by RPC; the owner stores state on the ship so everyone sees the same thing.
- If the owner doesn't have Seaworthy, the ship sails like vanilla.
