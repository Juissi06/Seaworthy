# Roadmap

Seaworthy turns sailing into a skill while keeping vanilla balance as the baseline: a ship nobody trims sails like vanilla.

## 0.1 (now)
- [x] Manual sail trim (Left Shift + A/D at the helm), trim gauge, perfect-trim bonus
- [x] Anchor (G at the helm)

## Next
1. [x] **Server-enforced settings**: the server decides the trim and anchor rules (own small sync, no extra library).
2. [ ] **Anchor model and sound**: a visible anchor chain/rope and a splash when dropped.
3. [ ] **Upwind sailing (tacking)**: today vanilla's no-go zone (about 40° either side of the wind) stays. A trimmed sail could point a little higher, with less speed.
4. [ ] **Gamepad support** for trimming.
5. [ ] **Wind feel**: gusts that briefly change the best angle, so trim needs attention on long trips.

## Later ideas
- Hull leaks you patch at sea, cargo weight that slows the ship
- Ship upgrades (better sails, reinforced hull, bigger rudder) from biome materials
- Rougher water and currents in deep ocean

## Notes
- The ship's physics run on one client (the ship's owner, a player on board). The helmsman sends trim and anchor to the owner by RPC, the owner stores them on the ship so everyone sees the same sail.
- If the owner doesn't have Seaworthy, the ship sails like vanilla.
