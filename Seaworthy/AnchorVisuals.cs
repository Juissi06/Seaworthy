using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace Seaworthy
{
    // On every client: a rope from the bow down into the water while a ship is anchored, and the game's own
    // water-impact splash when the anchor drops. Driven by the anchor state on the ship, so everyone sees it.
    internal static class AnchorVisuals
    {
        const float RopeDepth = 4f;
        static readonly Color RopeColor = new Color(0.30f, 0.22f, 0.14f, 1f);
        static readonly Dictionary<Ship, LineRenderer> Ropes = new Dictionary<Ship, LineRenderer>();
        static readonly HashSet<Ship> Anchored = new HashSet<Ship>();
        static Material _material;

        internal static void Update(Ship ship)
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated()) return; // nothing to see on a server

            bool anchored = ServerSettings.AnchorEnabled && SailState.IsAnchored(ship);
            bool was = Anchored.Contains(ship);
            if (anchored != was)
            {
                if (anchored)
                {
                    Anchored.Add(ship);
                    ship.m_waterImpactEffect.Create(Bow(ship), Quaternion.identity);
                }
                else
                {
                    Anchored.Remove(ship);
                }
            }

            Ropes.TryGetValue(ship, out var rope);
            if (!anchored)
            {
                if (rope != null && rope.gameObject.activeSelf) rope.gameObject.SetActive(false);
                return;
            }
            if (rope == null)
            {
                rope = CreateRope(ship);
                if (rope == null) return;
                Ropes[ship] = rope;
            }
            rope.gameObject.SetActive(true);
            Vector3 top = Bow(ship);
            float water = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
            Vector3 bottom = new Vector3(top.x, water - RopeDepth, top.z) + ship.transform.forward * 1.5f;
            rope.SetPosition(0, top);
            rope.SetPosition(1, Vector3.Lerp(top, bottom, 0.5f) + ship.transform.forward * 0.4f);
            rope.SetPosition(2, bottom);
        }

        // Front of the hull, a little above the waterline.
        static Vector3 Bow(Ship ship)
        {
            var hull = ship.m_floatCollider;
            float half = hull != null ? hull.size.z * 0.5f : 3f;
            Transform t = hull != null ? hull.transform : ship.transform;
            return t.position + t.forward * (half * 0.9f) + Vector3.up * 0.8f;
        }

        static LineRenderer CreateRope(Ship ship)
        {
            if (_material == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) return null;
                _material = new Material(shader);
            }
            var go = new GameObject("Seaworthy_AnchorRope");
            go.transform.SetParent(ship.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = _material;
            line.useWorldSpace = true;
            line.positionCount = 3;
            line.startWidth = line.endWidth = 0.07f;
            line.startColor = line.endColor = RopeColor;
            line.shadowCastingMode = ShadowCastingMode.On;
            line.receiveShadows = true;
            return line;
        }

        // Mod switched off: take the ropes down.
        internal static void HideAll()
        {
            foreach (var rope in Ropes.Values)
                if (rope != null) rope.gameObject.SetActive(false);
            Anchored.Clear();
        }

        internal static void Forget(Ship ship)
        {
            Ropes.Remove(ship);
            Anchored.Remove(ship);
        }
    }

    // CustomFixedUpdate runs on every client (only the physics part is owner-only).
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    static class AnchorVisualsPatch
    {
        static void Postfix(Ship __instance) => AnchorVisuals.Update(__instance);
    }

    [HarmonyPatch(typeof(Ship), "OnDisable")]
    static class ShipDisablePatch
    {
        static void Postfix(Ship __instance) => AnchorVisuals.Forget(__instance);
    }
}
