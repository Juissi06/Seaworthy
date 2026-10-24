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
- [x] Manual sail trim (Left Shift + A/D at the helm) with a perfect-trim bonus
- [x] Anchor (G at the helm), rope and splash for everyone
- [x] Server-enforced settings, dedicated servers supported

## Now
1. [x] **Helm panel**: speed (knots), compass strip with heading and wind, sail mode, trim gauge, anchor state.
2. [x] **Lookout**: larger map reveal radius while aboard, scaled by ship (raft < karve < longship).
3. [x] **Sailing skill**: a real skill in the skills list. Levels with distance sailed; raises sail force and rowing speed a little (default +20% at 100).
4. [x] **Push off**: at the helm of a beached ship, hold a key to shove it back into deeper water.
5. [x] **Gusts and storm winds**: the wind wanders and surges, more in storms, shifting the best trim angle. Only affects a sail you trim yourself.
6. [ ] **Course to a pin**: pick a map pin, the helm panel shows bearing, distance and ETA.

## Later
- Tacking: a well trimmed sail can point a little higher into the wind than vanilla
- Gamepad trim controls
- Hull leaks you patch at sea, cargo weight that slows the ship
- Ship upgrades from biome materials

## Notes
- The ship's physics run on one client (the ship's owner, a player on board). Controls go helmsman → owner by RPC; the owner stores state on the ship so everyone sees the same thing.
- If the owner doesn't have Seaworthy, the ship sails like vanilla.
