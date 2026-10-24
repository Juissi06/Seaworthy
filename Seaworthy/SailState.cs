using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    // Per-ship sail trim and anchor. The helmsman sends changes to the ship's owner by RPC (like vanilla's rudder);
    // the owner, who runs the ship's physics, stores them in the ship's ZDO so every client sees the same sail.
    // A ship nobody has trimmed sails exactly like vanilla.
    internal static class SailState
    {
        internal const float MaxTrim = 90f;
        const string TrimRpc = "Seaworthy_Trim";
        const string AnchorRpc = "Seaworthy_Anchor";
        static readonly int TrimKey = "seaworthy_trim".GetStableHashCode();
        static readonly int ManualKey = "seaworthy_manual".GetStableHashCode();
        static readonly int AnchorKey = "seaworthy_anchor".GetStableHashCode();

        internal static readonly AccessTools.FieldRef<Ship, ZNetView> NView = AccessTools.FieldRefAccess<Ship, ZNetView>("m_nview");
        internal static readonly AccessTools.FieldRef<Ship, Rigidbody> Body = AccessTools.FieldRefAccess<Ship, Rigidbody>("m_body");
        internal static readonly AccessTools.FieldRef<Ship, Ship.Speed> SpeedSetting = AccessTools.FieldRefAccess<Ship, Ship.Speed>("m_speed");

        // The helmsman's own value, so the sail answers the keys at once instead of after an RPC round trip.
        internal static Ship LocalShip;
        internal static float LocalTrim;
        internal static bool LocalManual;
        internal static float LocalEditedAt = -99f;

        internal static void Register(Ship ship)
        {
            var nview = NView(ship);
            if (nview == null || !nview.IsValid()) return;
            nview.Register<float>(TrimRpc, (sender, trim) =>
            {
                if (!nview.IsOwner()) return;
                nview.GetZDO().Set(TrimKey, Mathf.Clamp(trim, -MaxTrim, MaxTrim));
                nview.GetZDO().Set(ManualKey, true);
            });
            nview.Register<bool>(AnchorRpc, (sender, down) =>
            {
                if (nview.IsOwner()) nview.GetZDO().Set(AnchorKey, down);
            });
            SailingSkill.Register(ship, nview);
            PushOff.Register(nview, ship);
        }

        // ---------- Trim ----------

        // Sail angle in degrees relative to the ship (0 = square across, positive = turned clockwise seen from above),
        // or null while the game trims the sail itself.
        internal static float? GetTrim(Ship ship)
        {
            if (ship == LocalShip && Time.time - LocalEditedAt < 2f) return LocalManual ? LocalTrim : (float?)null;
            var zdo = Zdo(ship);
            if (zdo == null || !zdo.GetBool(ManualKey)) return null;
            return zdo.GetFloat(TrimKey);
        }

        internal static void SendTrim(Ship ship, float trim) => NView(ship)?.InvokeRPC(TrimRpc, trim);

        // The angle the game itself would turn the sail to (Ship.UpdateSail), which is the best angle for this wind.
        internal static float OptimalTrim(Ship ship)
        {
            Transform t = ship.transform;
            Vector3 wind = Vector3.Cross(Vector3.Cross(Wind.Direction, t.up), t.up); // with gusts: only manual trim uses this
            float blend = 0.5f + Vector3.Dot(t.forward, wind) * 0.5f;
            Vector3 mast = -Vector3.Lerp(wind, Vector3.Normalize(wind - t.forward), blend);
            if (mast.sqrMagnitude < 1e-4f) return 0f;
            return Mathf.Clamp(Vector3.SignedAngle(-t.forward, mast, t.up), -MaxTrim, MaxTrim);
        }

        // Share of the sail's force kept at this trim: 1 when perfect, 0 when 90° off (scaled by tolerance).
        internal static float TrimEfficiency(float trim, float optimal)
        {
            float error = Mathf.Abs(Mathf.DeltaAngle(trim, optimal)) / ServerSettings.TrimTolerance;
            float c = Mathf.Cos(Mathf.Min(error, 90f) * Mathf.Deg2Rad);
            return c * c;
        }

        // Multiplier on vanilla's sail force: efficiency, plus the bonus for a well trimmed sail.
        internal static float ForceFactor(float trim, float optimal) =>
            TrimEfficiency(trim, optimal) * (1f + ServerSettings.PerfectTrimBonus);

        internal static Quaternion MastRotation(Ship ship, float trim)
        {
            Transform t = ship.transform;
            return Quaternion.LookRotation(Quaternion.AngleAxis(trim, t.up) * -t.forward, t.up);
        }

        // ---------- Anchor ----------

        internal static bool IsAnchored(Ship ship) => Zdo(ship)?.GetBool(AnchorKey) ?? false;

        internal static void SendAnchor(Ship ship, bool down) => NView(ship)?.InvokeRPC(AnchorRpc, down);

        static ZDO Zdo(Ship ship)
        {
            var nview = ship != null ? NView(ship) : null;
            return nview != null && nview.IsValid() ? nview.GetZDO() : null;
        }

        // Owner only: lowering the sail hands trim back to the game.
        internal static void ClearManual(Ship ship)
        {
            var zdo = Zdo(ship);
            if (zdo != null && zdo.GetBool(ManualKey)) zdo.Set(ManualKey, false);
        }
    }

    [HarmonyPatch(typeof(Ship), "Start")]
    static class ShipStartPatch
    {
        static void Postfix(Ship __instance) => SailState.Register(__instance);
    }

    // Helmsman: holding the trim modifier turns the sail with the steering keys instead of the rudder.
    [HarmonyPatch(typeof(Ship), nameof(Ship.ApplyControlls))]
    static class ApplyControllsPatch
    {
        static float _lastSent = -99f, _sentTrim = float.NaN, _anchorMessage = -99f;
        static bool _forwardHeld;

        static void Prefix(Ship __instance, ref Vector3 dir)
        {
            bool forward = dir.z > 0.5f;
            if (ServerSettings.AnchorEnabled && forward && !_forwardHeld && SailState.IsAnchored(__instance) &&
                Time.time - _anchorMessage > 2f && Player.m_localPlayer != null)
            {
                _anchorMessage = Time.time;
                Player.m_localPlayer.Message(MessageHud.MessageType.Center,
                    $"<color={Ui.Warn}>Anchored</color>: raise the anchor first ({Plugin.AnchorKey.Value})");
            }
            _forwardHeld = forward;

            if (!ServerSettings.ManualTrim) return;
            if (!__instance.IsSailUp())
            {
                SailState.LocalManual = false; // furling the sail hands trim back to the game
                return;
            }
            if (SailState.LocalShip != __instance)
            {
                SailState.LocalShip = __instance;
                float? current = SailState.GetTrim(__instance);
                SailState.LocalManual = current.HasValue;
                SailState.LocalTrim = current ?? SailState.OptimalTrim(__instance);
            }

            var modifier = Plugin.TrimModifier.Value;
            if (modifier == KeyCode.None || !BepInEx.UnityInput.Current.GetKey(modifier)) return;

            // First touch starts from where the game had the sail, so taking over never jolts it.
            if (!SailState.LocalManual)
            {
                SailState.LocalManual = true;
                SailState.LocalTrim = SailState.OptimalTrim(__instance);
            }
            SailState.LocalTrim = Mathf.Clamp(SailState.LocalTrim + dir.x * ServerSettings.TrimSpeed * Time.fixedDeltaTime,
                -SailState.MaxTrim, SailState.MaxTrim);
            SailState.LocalEditedAt = Time.time;
            dir.x = 0f; // the rudder holds while you trim

            if (Time.time - _lastSent > 0.15f && !Mathf.Approximately(_sentTrim, SailState.LocalTrim))
            {
                _lastSent = Time.time;
                _sentTrim = SailState.LocalTrim;
                SailState.SendTrim(__instance, SailState.LocalTrim);
            }
        }
    }

    // Owner: vanilla's sail force, scaled by how well the sail is trimmed and the helmsman's Sailing skill.
    [HarmonyPatch(typeof(Ship), "GetSailForce")]
    static class SailForcePatch
    {
        static readonly AccessTools.FieldRef<Ship, Vector3> SailForce = AccessTools.FieldRefAccess<Ship, Vector3>("m_sailForce");
        static readonly AccessTools.FieldRef<Ship, Vector3> WindChangeVelocity =
            AccessTools.FieldRefAccess<Ship, Vector3>("m_windChangeVelocity");

        static bool Prefix(Ship __instance, float sailSize, ref Vector3 __result)
        {
            float? trim = ServerSettings.ManualTrim ? SailState.GetTrim(__instance) : null;
            float skill = SailingSkill.ForceFactor(__instance);
            if (!trim.HasValue && skill == 1f) return true; // untouched ship: vanilla

            // Same as Ship.GetSailForce, times the trim and skill factors.
            // A sail you trim yourself feels the gusts; one on auto gets vanilla's steady wind.
            Vector3 windDir = trim.HasValue ? Wind.Direction : EnvMan.instance.GetWindDir();
            float intensity = Mathf.Lerp(0.25f, 1f, EnvMan.instance.GetWindIntensity()) * (trim.HasValue ? Wind.GustStrength : 1f);
            float trimFactor = trim.HasValue ? SailState.ForceFactor(trim.Value, SailState.OptimalTrim(__instance)) : 1f;
            float factor = WindAngleFactor(windDir, __instance.transform.forward) * intensity * trimFactor * skill;
            Vector3 target = Vector3.Normalize(windDir + __instance.transform.forward) * (factor * __instance.m_sailForceFactor * sailSize);
            ref Vector3 force = ref SailForce(__instance);
            force = Vector3.SmoothDamp(force, target, ref WindChangeVelocity(__instance), 1f, 99f);
            __result = force;
            return false;
        }

        // Ship.GetWindAngleFactor for a given wind: best across the wind, 70% straight downwind, none into it.
        static float WindAngleFactor(Vector3 windDir, Vector3 forward)
        {
            float d = Vector3.Dot(windDir, -forward);
            return Mathf.Lerp(0.7f, 1f, 1f - Mathf.Abs(d)) * (1f - Utils.LerpStep(0.75f, 0.8f, d));
        }
    }

    // Everyone: point the mast where the sail is trimmed, instead of the game's automatic angle.
    [HarmonyPatch(typeof(Ship), "UpdateSail")]
    static class UpdateSailPatch
    {
        static void Postfix(Ship __instance, float dt)
        {
            if (!ServerSettings.ManualTrim || !__instance.IsSailUp() || __instance.m_mastObject == null) return;
            float? trim = SailState.GetTrim(__instance);
            if (!trim.HasValue) return;
            Transform mast = __instance.m_mastObject.transform;
            mast.rotation = Quaternion.RotateTowards(mast.rotation, SailState.MastRotation(__instance, trim.Value), 90f * dt);
        }
    }

    // Owner: the anchor stops the ship and keeps the sail furled; lowering the sail ends manual trim.
    [HarmonyPatch(typeof(Ship), nameof(Ship.CustomFixedUpdate))]
    static class ShipFixedUpdatePatch
    {
        static void Postfix(Ship __instance, float fixedDeltaTime)
        {
            if (!__instance.IsOwner()) return;
            if (!__instance.IsSailUp()) SailState.ClearManual(__instance);
            if (!ServerSettings.AnchorEnabled || !SailState.IsAnchored(__instance)) return;

            SailState.SpeedSetting(__instance) = Ship.Speed.Stop;
            var body = SailState.Body(__instance);
            if (body == null) return;
            float hold = ServerSettings.AnchorHold;
            Vector3 v = body.linearVelocity;
            body.AddForce(new Vector3(-v.x, 0f, -v.z) * hold, ForceMode.Acceleration);
            body.AddTorque(new Vector3(0f, -body.angularVelocity.y * hold, 0f), ForceMode.Acceleration);
        }
    }
}
