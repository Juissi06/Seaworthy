using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // A real Sailing skill in the skills list. The helmsman earns it by covering distance under sail (rowing earns
    // half), and it adds sail force and rowing speed. The ship's physics run on its owner, so the helmsman's skill
    // is sent to the owner and stored on the ship, like the trim.
    internal static class SailingSkill
    {
        // A fixed id outside the game's own skill numbers. Saved in the character file, so it must never change.
        internal static readonly Skills.SkillType Type = (Skills.SkillType)(Mathf.Abs("Seaworthy.Sailing".GetStableHashCode()) % 100000 + 1000);
        const float MetresPerRaise = 10f;
        const string SkillRpc = "Seaworthy_Skill";
        static readonly int SkillKey = "seaworthy_skill".GetStableHashCode();

        static Skills.SkillDef _def;
        static Vector3 _lastPos;
        static Ship _lastShip;
        static float _distance, _lastSent = -99f, _sentFactor = -1f;

        internal static string LocalizationKey => "skill_" + Type.ToString().ToLower();

        internal static Skills.SkillDef Def
        {
            get
            {
                if (_def == null)
                {
                    _def = new Skills.SkillDef
                    {
                        m_skill = Type,
                        m_description = "Faster sailing and rowing. Earned at the helm.",
                        m_increseStep = 1f
                    };
                }
                // The icon comes from the game's ships, which load after this may first be asked for.
                if (_def.m_icon == null && ZNetScene.instance != null)
                    _def.m_icon = ZNetScene.instance.GetPrefab("Karve")?.GetComponent<Piece>()?.m_icon;
                return _def;
            }
        }

        internal static void AddTranslations(Dictionary<string, string> words)
        {
            if (words == null) return;
            words[LocalizationKey] = "Sailing";
        }

        internal static void Register(Ship ship, ZNetView nview)
        {
            nview.Register<float>(SkillRpc, (sender, factor) =>
            {
                if (nview.IsOwner()) nview.GetZDO().Set(SkillKey, Mathf.Clamp01(factor));
            });
        }

        // Helmsman, every frame at the helm: earn skill for distance under sail, and tell the owner your level.
        internal static void Update(Player player, Ship ship)
        {
            if (!ServerSettings.SkillEnabled) return;
            Vector3 pos = ship.transform.position;
            if (ship != _lastShip)
            {
                _lastShip = ship;
                _lastPos = pos;
                _sentFactor = -1f;
            }

            var speed = ship.GetSpeedSetting();
            float credit = speed == Ship.Speed.Half || speed == Ship.Speed.Full ? 1f
                : speed == Ship.Speed.Slow || speed == Ship.Speed.Back ? 0.5f : 0f;
            _distance += Utils.DistanceXZ(pos, _lastPos) * credit;
            _lastPos = pos;
            while (_distance >= MetresPerRaise)
            {
                _distance -= MetresPerRaise;
                player.RaiseSkill(Type);
            }

            float factor = player.GetSkillFactor(Type);
            if (Time.time - _lastSent > 5f || Mathf.Abs(factor - _sentFactor) > 0.005f)
            {
                _lastSent = Time.time;
                _sentFactor = factor;
                SailState.NView(ship)?.InvokeRPC(SkillRpc, factor);
            }
        }

        // Owner: extra force from the helmsman's skill, 1 when unskilled.
        internal static float ForceFactor(Ship ship)
        {
            if (!ServerSettings.SkillEnabled) return 1f;
            var nview = SailState.NView(ship);
            if (nview == null || !nview.IsValid()) return 1f;
            return 1f + ServerSettings.SkillBonus * nview.GetZDO().GetFloat(SkillKey);
        }
    }

    // These three stay patched even with Seaworthy switched off (see SkillPatches.Apply): otherwise saving the
    // character would drop the Sailing skill and its progress.
    internal static class SkillPatches
    {
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(Skills), "IsSkillValid"),
                postfix: new HarmonyMethod(typeof(SkillValidPatch), nameof(SkillValidPatch.Postfix)));
            harmony.Patch(AccessTools.Method(typeof(Skills), "GetSkillDef"),
                postfix: new HarmonyMethod(typeof(SkillDefPatch), nameof(SkillDefPatch.Postfix)));
            harmony.Patch(AccessTools.Method(typeof(Localization), nameof(Localization.SetupLanguage)),
                postfix: new HarmonyMethod(typeof(SkillNamePatch), nameof(SkillNamePatch.Postfix)));
        }
    }

    // Saved characters list the skill by number; let the game accept ours.
    static class SkillValidPatch
    {
        internal static void Postfix(Skills.SkillType type, ref bool __result)
        {
            if (type == SailingSkill.Type) __result = true;
        }
    }

    static class SkillDefPatch
    {
        internal static void Postfix(Skills.SkillType type, ref Skills.SkillDef __result)
        {
            if (__result == null && type == SailingSkill.Type) __result = SailingSkill.Def;
        }
    }

    // The skills list shows "$skill_<id>"; give it a name in every language load.
    static class SkillNamePatch
    {
        static readonly AccessTools.FieldRef<Localization, Dictionary<string, string>> Words =
            AccessTools.FieldRefAccess<Localization, Dictionary<string, string>>("m_translations");

        internal static void Postfix(Localization __instance) => SailingSkill.AddTranslations(Words(__instance));

        internal static void AddNow()
        {
            if (Localization.instance != null) SailingSkill.AddTranslations(Words(Localization.instance));
        }
    }

    // Owner: rowing is faster with skill. Scale the rowing force for this physics step only.
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    static class RowingSkillPatch
    {
        static void Prefix(Ship __instance, out float __state)
        {
            __state = __instance.m_backwardForce;
            if (__instance.IsOwner()) __instance.m_backwardForce = __state * SailingSkill.ForceFactor(__instance);
        }

        static void Finalizer(Ship __instance, float __state) => __instance.m_backwardForce = __state;
    }
}
