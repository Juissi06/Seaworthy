using UnityEngine;

namespace Seaworthy
{
    // Optional realism: a heavily loaded hold makes the ship slower under sail and oars. Off by default.
    // Capacity scales with the ship's storage (50 kg per slot), so a full longship and a full karve feel alike.
    internal static class Cargo
    {
        const float KgPerSlot = 50f;

        // Hold load 0..1 (can exceed 1 when overloaded), or 0 when the ship has no storage.
        internal static float Load(Ship ship)
        {
            var container = ship.GetComponentInChildren<Container>();
            var inventory = container != null ? container.GetInventory() : null;
            if (inventory == null) return 0f;
            float capacity = inventory.GetWidth() * inventory.GetHeight() * KgPerSlot;
            return capacity > 0f ? inventory.GetTotalWeight() / capacity : 0f;
        }

        // Owner: multiplier on sail and rowing force.
        internal static float SpeedFactor(Ship ship)
        {
            if (!ServerSettings.CargoWeight) return 1f;
            return 1f - ServerSettings.CargoPenalty * Mathf.Clamp01(Load(ship));
        }
    }
}
