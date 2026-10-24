using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // A warm lantern glow at the stern of every ship after dark, so you can find your ship at night and the sea
    // feels less empty. Purely visual and local: no light objects are networked.
    internal static class ShipLights
    {
        static readonly Color LanternColor = new Color(1f, 0.72f, 0.42f);
        static readonly Dictionary<Ship, Light> Lights = new Dictionary<Ship, Light>();

        internal static void Update(Ship ship)
        {
            if (ZNet.instance == null || ZNet.instance.IsDedicated() || EnvMan.instance == null) return;
            Lights.TryGetValue(ship, out var light);
            bool on = Plugin.ShipLanterns.Value && EnvMan.IsNight();
            if (!on)
            {
                if (light != null && light.enabled) light.enabled = false;
                return;
            }
            if (light == null)
            {
                light = Create(ship);
                Lights[ship] = light;
            }
            light.enabled = true;
            // A slow, gentle flicker, different per ship.
            float seed = ship.GetInstanceID() * 0.137f;
            light.intensity = 1.1f + 0.15f * Mathf.PerlinNoise(Time.time * 2.5f, seed);
        }

        static Light Create(Ship ship)
        {
            var go = new GameObject("Seaworthy_Lantern");
            go.transform.SetParent(ship.transform, false);
            // Above the helm, at the stern.
            Vector3 helm = ship.m_shipControlls != null ? ship.m_shipControlls.transform.position : ship.transform.position;
            go.transform.position = helm + Vector3.up * 2.2f;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LanternColor;
            light.range = 14f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
            return light;
        }

        internal static void HideAll()
        {
            foreach (var light in Lights.Values)
                if (light != null) light.enabled = false;
        }

        internal static void Forget(Ship ship) => Lights.Remove(ship);
    }

    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    static class ShipLightsPatch
    {
        static void Postfix(Ship __instance) => ShipLights.Update(__instance);
    }
}
