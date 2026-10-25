# Changelog

## Unreleased
- Helm panel moved to the bottom right corner so it doesn't block the view ahead. New Panel X / Panel Y settings move it anywhere.
- Hull readout and hull warnings moved here from Norse Sense: a small hull bar while you're aboard (in the helm panel when steering), a warning when the ship is hit and loud ones at 50% and 25%.
- Sailing skill levels a bit slower (one raise per 15 m).
- Wave damage setting: scale the hull damage from slamming into waves (1 = vanilla).
- Helm panel is smaller by default (UI scale 0.85) and the trim key hint disappears once you've trimmed.
- Crew rowing: each extra player aboard makes rowing 10% faster (up to 3 extra).
- Cargo weight (optional, off by default): a heavily loaded hold makes the ship slower; the helm panel shows how full the hold is.
- Compatibility: with another mod that names ships (Shipwright's Touch), reveals the map at sea, adds a sailing skill or anchors (BoatAdditions), Seaworthy turns its own version off so they don't fight. Mods that change the sail force or wind are reported in the log.
- Ship names: Left Shift + E at the helm to name your ship. Everyone sees it on the helm and at the top of the helm panel.
- Ship lanterns: after dark, ships glow with a warm lantern at the stern, so you can find yours at night.
- Gamepad: hold LT at the helm and use the stick to trim the sail.
- Pointing higher: with your own trim, Sailing skill lets you sail closer to the wind (from about 41° to 34° off the wind at skill 100). The helm panel says "Head to wind" when you're too close for the sail to draw.
- The first time you take a helm each session, a tip lists the controls (can be turned off).
- Course: Left Alt + click a pin (or any spot) on the large map to set a course. The helm panel marks it on the compass and shows the distance, which way to steer and an ETA. A course to a spot gets a temporary map pin. You get a message when you arrive.
- Gusts: the wind wanders a few degrees and surges, more in storms, so a sail you trim yourself needs attention on long voyages. Ships on auto trim sail like vanilla. Everyone sees the same gusts.
- Push off: at the helm of a beached ship, hold B to shove it towards the deepest water nearby. The helm panel tells you when you're beached.
- Sailing skill: a new skill in your skills list. It levels while you steer under sail (rowing counts half) and adds up to 20% sail force and rowing speed at level 100. Works for whoever is steering, even when another player runs the ship.
- Lookout: while aboard, the map reveals a larger area around you. Bigger ships see further (longship twice the normal radius by default).
- Helm panel: at the helm, one panel with a compass strip (heading and where the wind comes from), speed in knots (or km/h, m/s), sail mode, wind strength, the trim gauge and the anchor state.
- Anchor rope from the bow into the water while anchored, and a splash when the anchor drops. Everyone nearby sees it.
- Server-enforced settings: with Seaworthy on the server, its sail trim and anchor settings apply to every player. Works on dedicated servers and hosted games. Your own config applies in single player and on servers without the mod.
- Config file is now `BepInEx/config/Seaworthy.cfg`.

## 0.1.0
- First version: manual sail trim with a trim gauge, perfect-trim speed bonus, and an anchor.
