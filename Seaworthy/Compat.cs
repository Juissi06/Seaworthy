using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;

namespace Seaworthy
{
    // Once in game: find other sailing mods that do the same job and step aside, so two mods never fight over a
    // ship. Known mods are matched by GUID/name; any other mod patching the sail force is reported in the log.
    internal static class Compat
    {
        internal static bool Checked;
        internal static bool NamesOff, LookoutOff, SkillOff, AnchorOff;

        static readonly string[] NameMods = { "shipwright", "shipname", "boatname" };
        static readonly string[] LookoutMods = { "explore", "reveal", "minimap" };
        static readonly string[] SkillMods = { "sailingskill", "sailing skill", "sailing_skill" };
        static readonly string[] AnchorMods = { "boatadditions" };

        internal static void Run(string ownId, ManualLogSource log)
        {
            Checked = true;
            if (!Plugin.AutoCompat.Value)
            {
                log.LogInfo("Compatibility auto-adjust is off.");
                return;
            }
            var notes = new List<string>();

            string mod = Find(NameMods);
            if (mod != null && Plugin.ShipNamesEnabled.Value)
            {
                NamesOff = true;
                notes.Add($"ship names are off, {mod} names ships");
            }
            // Only map-reveal mods that are about boats/sailing.
            mod = FindAll(LookoutMods, k => k.Contains("boat") || k.Contains("ship") || k.Contains("sail"));
            if (mod != null && ServerSettings.LookoutBonus > 0f)
            {
                LookoutOff = true;
                notes.Add($"lookout is off, {mod} reveals the map at sea");
            }
            mod = Find(SkillMods);
            if (mod != null && ServerSettings.SkillEnabled)
            {
                SkillOff = true;
                notes.Add($"the Sailing skill bonus is off, {mod} has its own");
            }
            mod = Find(AnchorMods);
            if (mod != null && ServerSettings.AnchorEnabled)
            {
                AnchorOff = true;
                notes.Add($"the anchor is off, {mod} has anchors");
            }

            string sailPatchers = PatchOwners(ownId,
                AccessTools.Method(typeof(Ship), "GetSailForce"),
                AccessTools.Method(typeof(Ship), nameof(Ship.GetWindAngleFactor)),
                AccessTools.Method(typeof(EnvMan), nameof(EnvMan.GetWindDir)));
            if (sailPatchers != null)
                log.LogWarning($"{sailPatchers} also changes sailing or wind; sail trim may not feel right with both.");

            foreach (var note in notes) log.LogWarning("Compatibility: " + note + ".");
            if (notes.Count == 0 && sailPatchers == null) log.LogInfo("No overlapping sailing mods found.");
            if (notes.Count > 0 && Player.m_localPlayer != null)
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft,
                    $"<color={Ui.Gold}>Seaworthy</color>: {string.Join("; ", notes)}");
        }

        static string Find(string[] keys) => FindAll(keys, _ => true);

        static string FindAll(string[] keys, Func<string, bool> also)
        {
            foreach (var info in Chainloader.PluginInfos.Values)
            {
                if (info.Metadata.GUID == "Seaworthy") continue;
                string key = (info.Metadata.GUID + " " + info.Metadata.Name).ToLowerInvariant();
                if (keys.Any(k => key.Contains(k)) && also(key)) return info.Metadata.Name;
            }
            return null;
        }

        static string PatchOwners(string ownId, params MethodBase[] methods)
        {
            var owners = new HashSet<string>();
            foreach (var method in methods)
            {
                if (method == null) continue;
                var info = Harmony.GetPatchInfo(method);
                if (info == null) continue;
                foreach (var owner in info.Owners)
                    if (owner != ownId) owners.Add(Chainloader.PluginInfos.TryGetValue(owner, out var p) ? p.Metadata.Name : owner);
            }
            return owners.Count > 0 ? string.Join(", ", owners) : null;
        }
    }
}
