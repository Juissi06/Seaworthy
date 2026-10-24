using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // Name your ship: Left Shift + E at the helm opens the game's text box. The name is stored on the ship, so
    // everyone sees it on the helm's hover text and at the top of the helm panel.
    internal static class ShipNames
    {
        const string NameRpc = "Seaworthy_Name";
        const int MaxLength = 24;
        static readonly int NameKey = "seaworthy_name".GetStableHashCode();

        internal static void Register(ZNetView nview)
        {
            nview.Register<string>(NameRpc, (sender, name) =>
            {
                if (nview.IsOwner()) nview.GetZDO().Set(NameKey, Clean(name));
            });
        }

        internal static string Get(Ship ship)
        {
            var nview = ship != null ? SailState.NView(ship) : null;
            return nview != null && nview.IsValid() ? nview.GetZDO().GetString(NameKey) : "";
        }

        // Plain text only: no rich-text tags sneaking into everyone's HUD.
        static string Clean(string name)
        {
            name = (name ?? "").Replace("<", "").Replace(">", "").Trim();
            return name.Length > MaxLength ? name.Substring(0, MaxLength) : name;
        }

        internal static bool ModifierHeld()
        {
            var key = Plugin.RenameModifier.Value;
            return key != KeyCode.None && BepInEx.UnityInput.Current.GetKey(key);
        }

        internal static void Rename(Ship ship)
        {
            if (TextInput.instance == null) return;
            TextInput.instance.RequestText(new Receiver(ship), "Name your ship", MaxLength);
        }

        class Receiver : TextReceiver
        {
            readonly Ship _ship;
            internal Receiver(Ship ship) => _ship = ship;
            public string GetText() => Get(_ship);
            public void SetText(string text)
            {
                if (_ship != null) SailState.NView(_ship)?.InvokeRPC(NameRpc, Clean(text));
            }
        }
    }

    // Left Shift + E at the helm renames instead of taking the helm.
    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.Interact))]
    static class RenameInteractPatch
    {
        static bool Prefix(ShipControlls __instance, bool repeat, ref bool __result)
        {
            if (repeat || !Plugin.ShipNamesEnabled.Value || !ShipNames.ModifierHeld() || __instance.m_ship == null) return true;
            ShipNames.Rename(__instance.m_ship);
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(ShipControlls), nameof(ShipControlls.GetHoverText))]
    static class RenameHoverPatch
    {
        static void Postfix(ShipControlls __instance, ref string __result)
        {
            if (!Plugin.ShipNamesEnabled.Value || __instance.m_ship == null || string.IsNullOrEmpty(__result)) return;
            string name = ShipNames.Get(__instance.m_ship);
            string hint = Localization.instance.Localize(
                $"[<color=yellow><b>{Plugin.RenameModifier.Value} + $KEY_Use</b></color>] {(name.Length > 0 ? "Rename" : "Name this ship")}");
            __result = (name.Length > 0 ? $"<color={Ui.Gold}><b>{name}</b></color>\n" : "") + __result + "\n" + hint;
        }
    }
}
