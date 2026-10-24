using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // From a ship you see further: while aboard, the map reveals a larger radius around you. Bigger ships have
    // taller masts, so the bonus grows with hull length (raft a little, longship the most).
    [HarmonyPatch(typeof(Minimap), "UpdateExplore")]
    static class LookoutPatch
    {
        const float LongshipLength = 20f; // hull length that gets the full bonus

        static void Prefix(Minimap __instance, out float __state)
        {
            __state = __instance.m_exploreRadius;
            float bonus = ServerSettings.LookoutBonus;
            var ship = Ship.GetLocalShip();
            if (bonus <= 0f || ship == null || Player.m_localPlayer == null || !ship.IsPlayerInBoat(Player.m_localPlayer)) return;
            __instance.m_exploreRadius = __state * Multiplier(ship, bonus);
        }

        static void Finalizer(Minimap __instance, float __state) => __instance.m_exploreRadius = __state;

        internal static float Multiplier(Ship ship, float bonus)
        {
            float length = ship.m_floatCollider != null ? ship.m_floatCollider.size.z : LongshipLength * 0.5f;
            return 1f + bonus * Mathf.Clamp01(length / LongshipLength);
        }
    }
}
