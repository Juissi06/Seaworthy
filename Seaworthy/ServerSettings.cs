using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // The server's sailing rules win, so nobody sails faster with their own config. Clients ask the server for its
    // settings when they join; the server answers and re-sends whenever its config changes. On a server without
    // Seaworthy, or in single player, your own config applies. Keys and UI stay personal.
    internal static class ServerSettings
    {
        const string GetRpc = "Seaworthy_GetSettings";
        const string SendRpc = "Seaworthy_Settings";
        const int Format = 5;

        static bool _fromServer, _requested;
        static bool _manualTrim, _anchorEnabled, _skillEnabled, _pushOff, _gusts;
        static float _trimSpeed, _bonus, _tolerance, _anchorHold, _lookout, _skillBonus, _gustAngle;

        internal static bool ManualTrim => _fromServer ? _manualTrim : Plugin.ManualTrim.Value;
        internal static float TrimSpeed => _fromServer ? _trimSpeed : Plugin.TrimSpeed.Value;
        internal static float PerfectTrimBonus => _fromServer ? _bonus : Plugin.PerfectTrimBonus.Value;
        internal static float TrimTolerance => _fromServer ? _tolerance : Plugin.TrimTolerance.Value;
        internal static bool AnchorEnabled => _fromServer ? _anchorEnabled : Plugin.AnchorEnabled.Value;
        internal static float AnchorHold => _fromServer ? _anchorHold : Plugin.AnchorHold.Value;
        internal static float LookoutBonus => _fromServer ? _lookout : Plugin.LookoutBonus.Value;
        internal static bool SkillEnabled => _fromServer ? _skillEnabled : Plugin.SkillEnabled.Value;
        internal static float SkillBonus => _fromServer ? _skillBonus : Plugin.SkillBonus.Value;
        internal static bool PushOffEnabled => _fromServer ? _pushOff : Plugin.PushOffEnabled.Value;
        internal static bool GustsEnabled => _fromServer ? _gusts : Plugin.GustsEnabled.Value;
        internal static float GustAngle => _fromServer ? _gustAngle : Plugin.GustAngle.Value;
        internal static bool FromServer => _fromServer;

        // New session: ZRoutedRpc is recreated with each ZNet.
        internal static void Register()
        {
            _fromServer = _requested = false;
            var rpc = ZRoutedRpc.instance;
            if (rpc == null) return;
            rpc.Register(GetRpc, OnRequest);
            rpc.Register<ZPackage>(SendRpc, OnSettings);
        }

        // Client: once in game, ask the server.
        internal static void RequestIfNeeded()
        {
            if (_requested || ZNet.instance == null || ZNet.instance.IsServer() || ZRoutedRpc.instance == null) return;
            _requested = true;
            ZRoutedRpc.instance.InvokeRoutedRPC(GetRpc); // no target = the server
        }

        // Server: config changed while running, tell everyone.
        internal static void Broadcast()
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null || !Plugin.EnforceOnClients.Value) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, SendRpc, Pack());
        }

        static void OnRequest(long sender)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || !Plugin.EnforceOnClients.Value) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, SendRpc, Pack());
        }

        static ZPackage Pack()
        {
            var pkg = new ZPackage();
            pkg.Write(Format);
            pkg.Write(Plugin.ManualTrim.Value);
            pkg.Write(Plugin.TrimSpeed.Value);
            pkg.Write(Plugin.PerfectTrimBonus.Value);
            pkg.Write(Plugin.TrimTolerance.Value);
            pkg.Write(Plugin.AnchorEnabled.Value);
            pkg.Write(Plugin.AnchorHold.Value);
            pkg.Write(Plugin.LookoutBonus.Value);
            pkg.Write(Plugin.SkillEnabled.Value);
            pkg.Write(Plugin.SkillBonus.Value);
            pkg.Write(Plugin.PushOffEnabled.Value);
            pkg.Write(Plugin.GustsEnabled.Value);
            pkg.Write(Plugin.GustAngle.Value);
            return pkg;
        }

        static void OnSettings(long sender, ZPackage pkg)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer()) return; // the host uses its own config
            if (pkg.ReadInt() != Format)
            {
                Debug.LogWarning("[Seaworthy] The server runs a different Seaworthy version; using your own settings.");
                return;
            }
            _manualTrim = pkg.ReadBool();
            _trimSpeed = pkg.ReadSingle();
            _bonus = Mathf.Clamp(pkg.ReadSingle(), 0f, 0.5f);
            _tolerance = Mathf.Clamp(pkg.ReadSingle(), 0.5f, 2f);
            _anchorEnabled = pkg.ReadBool();
            _anchorHold = Mathf.Clamp(pkg.ReadSingle(), 0.1f, 3f);
            _lookout = Mathf.Clamp(pkg.ReadSingle(), 0f, 3f);
            _skillEnabled = pkg.ReadBool();
            _skillBonus = Mathf.Clamp(pkg.ReadSingle(), 0f, 0.5f);
            _pushOff = pkg.ReadBool();
            _gusts = pkg.ReadBool();
            _gustAngle = Mathf.Clamp(pkg.ReadSingle(), 0f, 30f);
            bool first = !_fromServer;
            _fromServer = true;
            if (first)
                Debug.Log($"[Seaworthy] Using the server's settings: trim {(_manualTrim ? "on" : "off")}, bonus {_bonus:P0}, " +
                          $"anchor {(_anchorEnabled ? "on" : "off")}.");
        }

        internal static void Reset() => _fromServer = _requested = false;
    }

    [HarmonyPatch(typeof(Game), "Start")]
    static class GameStartPatch
    {
        static void Postfix() => ServerSettings.Register();
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    static class ZNetDestroyPatch
    {
        static void Postfix()
        {
            ServerSettings.Reset();
            Course.Clear(); // a course belongs to the world you set it in
        }
    }
}
