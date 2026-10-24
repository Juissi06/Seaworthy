# Changelog

## Unreleased
- Sailing skill: a new skill in your skills list. It levels while you steer under sail (rowing counts half) and adds up to 20% sail force and rowing speed at level 100. Works for whoever is steering, even when another player runs the ship.
- Lookout: while aboard, the map reveals a larger area around you. Bigger ships see further (longship twice the normal radius by default).
- Helm panel: at the helm, one panel with a compass strip (heading and where the wind comes from), speed in knots (or km/h, m/s), sail mode, wind strength, the trim gauge and the anchor state.
- Anchor rope from the bow into the water while anchored, and a splash when the anchor drops. Everyone nearby sees it.
- Server-enforced settings: with Seaworthy on the server, its sail trim and anchor settings apply to every player. Works on dedicated servers and hosted games. Your own config applies in single player and on servers without the mod.
- Config file is now `BepInEx/config/Seaworthy.cfg`.

## 0.1.0
- First version: manual sail trim with a trim gauge, perfect-trim speed bonus, and an anchor.
