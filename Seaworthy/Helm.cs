using UnityEngine;

namespace Seaworthy
{
    // At the helm: the anchor key, and a gauge showing the sail angle against the best angle for the wind.
    internal class Helm : MonoBehaviour
    {
        static readonly Color TrackGood = new Color(0.55f, 0.85f, 0.45f, 0.45f);
        static readonly Color NeedleColor = new Color(1f, 0.97f, 0.9f, 1f);

        Ship _ship;
        float _optimal, _efficiency;
        float? _trim;
        bool _anchored;
        GUIStyle _right;

        void Update()
        {
            var player = Player.m_localPlayer;
            _ship = player != null ? player.GetControlledShip() : null;
            if (_ship == null)
            {
                SailState.LocalShip = null; // next time at a helm, start from that ship's state
                return;
            }
            Ui.PrepareArt();

            if (Plugin.AnchorEnabled.Value && !Plugin.IsTyping() && Plugin.AnchorKey.Value.IsDown())
            {
                bool down = !SailState.IsAnchored(_ship);
                SailState.SendAnchor(_ship, down);
                player.Message(MessageHud.MessageType.Center, down
                    ? $"<color={Ui.Gold}>Anchor dropped</color>"
                    : $"<color={Ui.Gold}>Anchor raised</color>");
            }

            _anchored = SailState.IsAnchored(_ship);
            _trim = Plugin.ManualTrim.Value ? SailState.GetTrim(_ship) : null;
            _optimal = SailState.OptimalTrim(_ship);
            _efficiency = _trim.HasValue ? SailState.ForceFactor(_trim.Value, _optimal) : 1f;
        }

        void OnGUI()
        {
            if (_ship == null || !Plugin.ShowTrimGauge.Value) return;
            if (Hud.IsUserHidden() || Menu.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen()) return;
            bool sailUp = _ship.IsSailUp();
            bool showTrim = Plugin.ManualTrim.Value && sailUp;
            if (!showTrim && !_anchored) return;

            if (_right == null) _right = new GUIStyle(Ui.Small) { alignment = TextAnchor.MiddleRight };
            Matrix4x4 oldMatrix = Ui.BeginScaled(out float sw, out float sh);

            const float w = 380f, pad = 14f;
            float h = (showTrim ? 52f : 0f) + (_anchored ? 26f : 0f) + 14f;
            float x = (sw - w) * 0.5f;
            float y = Plugin.GaugeY.Value * (sh - h);
            float inner = w - pad * 2f;
            Ui.Panel(new Rect(x, y, w, h));
            float cy = y + 7f;

            if (showTrim)
            {
                string modifier = Plugin.TrimModifier.Value.ToString();
                if (_trim.HasValue)
                {
                    int percent = Mathf.RoundToInt(_efficiency * 100f);
                    string color = _efficiency >= 1f ? Ui.Good : _efficiency >= 0.75f ? Ui.Gold : _efficiency >= 0.4f ? Ui.Warn : Ui.Bad;
                    Ui.Label(new Rect(x + pad, cy, inner, 24f), $"Sail trim  <color={color}><b>{percent}%</b></color>", Ui.Text);
                }
                else
                {
                    Ui.Label(new Rect(x + pad, cy, inner, 24f), $"Sail trim  <color={Ui.Muted}>auto</color>", Ui.Text);
                }
                GUI.Label(new Rect(x + pad, cy, inner, 24f), $"{modifier} + A/D to trim", _right);
                cy += 28f;
                DrawTrimBar(new Rect(x + pad, cy, inner, 14f));
                cy += 24f;
            }

            if (_anchored)
            {
                if (showTrim) Ui.Divider(x + pad, cy, inner);
                Ui.Label(new Rect(x + pad, cy + 1f, inner, 24f),
                    $"<color={Ui.Warn}>Anchored</color>  <size=12><color={Ui.Muted}>({Plugin.AnchorKey.Value} to raise)</color></size>", Ui.Text);
            }

            GUI.matrix = oldMatrix;
        }

        // -90°..+90° track: the green zone is the best angle for the wind, the needle is the sail. A/D moves the needle.
        void DrawTrimBar(Rect r)
        {
            Ui.Bar(r, 0f, Color.clear);
            if (Event.current.type != EventType.Repaint) return;

            float Pos(float angle) => r.x + (angle + SailState.MaxTrim) / (SailState.MaxTrim * 2f) * r.width;
            // Zone where the sail still gives 85% or more (cos² ≥ 0.85 → about 22° each way, times tolerance).
            float half = 22.8f * Plugin.TrimTolerance.Value;
            float from = Pos(Mathf.Max(-SailState.MaxTrim, _optimal - half));
            float to = Pos(Mathf.Min(SailState.MaxTrim, _optimal + half));
            Color old = GUI.color;
            GUI.color = TrackGood;
            GUI.DrawTexture(new Rect(from, r.y + 2f, to - from, r.height - 4f), Texture2D.whiteTexture);
            GUI.color = new Color(TrackGood.r, TrackGood.g, TrackGood.b, 0.9f);
            GUI.DrawTexture(new Rect(Pos(_optimal) - 1f, r.y + 2f, 2f, r.height - 4f), Texture2D.whiteTexture);

            float needle = Pos(_trim ?? _optimal);
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(needle - 1f, r.y - 4f, 5f, r.height + 8f), Texture2D.whiteTexture);
            GUI.color = _trim.HasValue ? NeedleColor : new Color(NeedleColor.r, NeedleColor.g, NeedleColor.b, 0.5f);
            GUI.DrawTexture(new Rect(needle - 1.5f, r.y - 3f, 3f, r.height + 6f), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
