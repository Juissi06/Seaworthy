using System;
using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // A course to steer for: Left Alt + click a pin (or any spot) on the large map. The helm panel then shows it
    // on the compass with distance, bearing and an ETA at your current speed. Personal, not shared.
    internal static class Course
    {
        const float ArriveDistance = 40f;

        internal static bool Active;
        internal static Vector3 Target;
        internal static string Name;

        static readonly Func<Minimap, Vector3, Vector3> ScreenToWorld = Create<Func<Minimap, Vector3, Vector3>>("ScreenToWorldPoint");
        static readonly Func<Minimap, Minimap.PinData> ClosestPinToCursor = Create<Func<Minimap, Minimap.PinData>>("GetClosestPinToCursor");

        static T Create<T>(string method) where T : Delegate
        {
            try
            {
                return AccessTools.MethodDelegate<T>(AccessTools.Method(typeof(Minimap), method));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Seaworthy] Course plotting unavailable ({method}): {e.Message}");
                return null;
            }
        }

        // Large map, Left Alt + click: set or clear the course. Returns true when handled.
        internal static bool OnMapClick(Minimap map)
        {
            var modifier = Plugin.CourseModifier.Value;
            if (modifier == KeyCode.None || !BepInEx.UnityInput.Current.GetKey(modifier) || ScreenToWorld == null || ClosestPinToCursor == null) return false;

            var pin = ClosestPinToCursor(map);
            Vector3 point = pin != null ? pin.m_pos : ScreenToWorld(map, ZInput.pointerPosition);
            var player = Player.m_localPlayer;

            if (Active && Utils.DistanceXZ(point, Target) < 30f)
            {
                Clear();
                player?.Message(MessageHud.MessageType.TopLeft, $"<color={Ui.Gold}>Course</color>: cleared");
                return true;
            }

            Active = true;
            Target = point;
            Name = pin != null && !string.IsNullOrEmpty(pin.m_name) ? Localization.instance.Localize(pin.m_name) : "marked spot";
            player?.Message(MessageHud.MessageType.TopLeft, $"<color={Ui.Gold}>Course set</color>: {Name}");
            return true;
        }

        internal static void Clear()
        {
            Active = false;
            Name = null;
        }

        // Called from the helm: arriving ends the course.
        internal static void CheckArrival(Vector3 shipPos)
        {
            if (!Active || Utils.DistanceXZ(shipPos, Target) > ArriveDistance) return;
            Player.m_localPlayer?.Message(MessageHud.MessageType.Center, $"<color={Ui.Gold}>Arrived</color>: {Name}");
            Clear();
        }

        internal static float Bearing(Vector3 from)
        {
            Vector3 d = Target - from;
            return Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360f);
        }

        internal static string FormatDistance(float metres) =>
            metres >= 1000f ? $"{metres / 1000f:0.0} km" : $"{metres:0} m";

        // ETA at the speed you're making towards the target.
        internal static string FormatEta(float metres, float speedTowards)
        {
            if (speedTowards < 0.5f) return "--:--";
            int seconds = Mathf.RoundToInt(metres / speedTowards);
            return seconds >= 3600 ? $"{seconds / 3600}:{seconds / 60 % 60:00}:{seconds % 60:00}" : $"{seconds / 60}:{seconds % 60:00}";
        }
    }

    [HarmonyPatch(typeof(Minimap), nameof(Minimap.OnMapLeftClick))]
    static class MapClickPatch
    {
        static bool Prefix(Minimap __instance) => !Course.OnMapClick(__instance);
    }
}
