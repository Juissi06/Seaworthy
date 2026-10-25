using UnityEngine;

namespace Seaworthy
{
    // While aboard a ship: warnings when it takes damage or drops below set levels, and a small hull readout in the
    // helm panel's corner when you aren't steering (at the helm the panel shows the hull itself).
    internal class Hull : MonoBehaviour
    {
        const float CheckInterval = 0.25f;
        const float DamageMessageCooldown = 5f;

        internal static readonly Color HealthyColor = new Color(0.55f, 0.85f, 0.45f);
        internal static readonly Color DamagedColor = new Color(0.94f, 0.69f, 0.29f);
        internal static readonly Color CriticalColor = new Color(1f, 0.38f, 0.28f);

        Ship _ship;
        WearNTear _hull;
        Sprite _shipIcon;
        float _lastPercent = -1f;
        float _lowestWarned = 2f;
        float _timer;
        float _lastDamageMessage = -999f;
        float _flashUntil;
        GUIStyle _rightStyle;

        // Norse Sense 0.9.0 and earlier had this readout too; with one of those installed, leave it to Norse Sense.
        static bool? _norseSense;
        static bool NorseSenseHull => _norseSense ?? (_norseSense =
            BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("NorseSense", out var info) && info.Instance != null &&
            info.Instance.GetType().Assembly.GetType("NorseSense.HullWarning") != null).Value;

        internal static Color ColorFor(float percent) =>
            percent <= Plugin.HullCriticalLevel.Value ? CriticalColor : percent <= Plugin.HullWarnLevel.Value ? DamagedColor : HealthyColor;

        void Update()
        {
            var player = Player.m_localPlayer;
            Ship ship = player != null && !NorseSenseHull ? Ship.GetLocalShip() : null;
            if (ship != _ship)
            {
                _ship = ship;
                _hull = ship != null ? ship.GetComponent<WearNTear>() : null;
                _shipIcon = ship != null ? ship.GetComponent<Piece>()?.m_icon : null;
                _lastPercent = -1f;
                _lowestWarned = 2f;
            }
            if (_hull == null) return;

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = CheckInterval;

            float percent = _hull.GetHealthPercentage();
            if (_lastPercent >= 0f && percent < _lastPercent - 0.001f)
                OnDamaged(player, percent);
            else if (percent > _lastPercent)
                _lowestWarned = 2f; // repaired: allow the threshold warnings again
            _lastPercent = percent;
        }

        void OnDamaged(Player player, float percent)
        {
            _flashUntil = Time.time + 1.5f;
            if (!Plugin.HullWarnings.Value) return;

            // Threshold crossings always warn; plain damage is rate-limited (Ashlands water damages constantly).
            float threshold = percent <= Plugin.HullCriticalLevel.Value ? Plugin.HullCriticalLevel.Value
                : percent <= Plugin.HullWarnLevel.Value ? Plugin.HullWarnLevel.Value
                : 2f;
            if (threshold < _lowestWarned)
            {
                _lowestWarned = threshold;
                bool critical = threshold <= Plugin.HullCriticalLevel.Value;
                player.Message(MessageHud.MessageType.Center,
                    critical ? $"<color={Ui.Bad}>Hull health critical: {percent * 100f:0}%</color>"
                             : $"<color={Ui.Warn}>Hull damaged: {percent * 100f:0}%</color>");
                _lastDamageMessage = Time.time;
            }
            else if (Time.time - _lastDamageMessage > DamageMessageCooldown)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"Hull damaged: {percent * 100f:0}%");
                _lastDamageMessage = Time.time;
            }
        }

        void OnGUI()
        {
            if (!Plugin.ShowHullReadout.Value || _hull == null || _lastPercent < 0f || Player.m_localPlayer == null) return;
            // At the helm the panel has its own hull row.
            if (Plugin.ShowHelmPanel.Value && Player.m_localPlayer.GetControlledShip() != null) return;
            if (Hud.IsUserHidden() || Menu.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen()) return;
            Ui.PrepareArt();

            Matrix4x4 oldMatrix = Ui.BeginScaled(out float sw, out float sh);

            // Compact: one row of text with a thin bar underneath, in the helm panel's corner.
            const float w = 132f, h = 32f, pad = 9f;
            Rect r = Helm.Place(sw, sh, w, h);
            float x = r.x, y = r.y;

            bool critical = _lastPercent <= Plugin.HullCriticalLevel.Value;
            Color color = ColorFor(_lastPercent);
            // Flash brighter when hit, and pulse while critical.
            if (Time.time < _flashUntil) color = Color.Lerp(color, Color.white, 0.5f * (_flashUntil - Time.time) / 1.5f);
            if (critical) color.a = 0.65f + 0.35f * Mathf.Abs(Mathf.Sin(Time.time * 4f));

            Ui.Panel(r);
            float labelX = x + pad;
            if (_shipIcon != null)
            {
                Ui.Icon(new Rect(labelX, y + 4f, 15f, 15f), _shipIcon);
                labelX += 19f;
            }
            Ui.Label(new Rect(labelX, y + 3f, w - pad - (labelX - x), 17f), "Hull", Ui.Small);
            Ui.Label(new Rect(x + pad, y + 3f, w - pad * 2, 17f),
                $"<b><color=#{ColorUtility.ToHtmlStringRGB(color)}>{_lastPercent * 100f:0}%</color></b>",
                _rightStyle ?? (_rightStyle = new GUIStyle(Ui.Small) { alignment = TextAnchor.MiddleRight }));
            Ui.Bar(new Rect(x + pad, y + 22f, w - pad * 2, 6f), _lastPercent, color);

            GUI.matrix = oldMatrix;
        }
    }
}
