using UnityEngine;

namespace Seaworthy
{
    // At the helm: the anchor key, and the helm panel: a compass strip with heading and wind, speed, sail mode,
    // wind strength, the trim gauge and the anchor state.
    internal class Helm : MonoBehaviour
    {
        const float CompassRange = 75f; // degrees either side of the heading shown on the strip
        static readonly string[] Points = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
        static readonly Color TrackGood = new Color(0.55f, 0.85f, 0.45f, 0.45f);
        static readonly Color NeedleColor = new Color(1f, 0.97f, 0.9f, 1f);
        static readonly Color WindColor = new Color(0.56f, 0.79f, 1f, 1f);
        static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);

        static bool _namesAdded, _tipShown;
        Ship _ship;
        float _optimal, _efficiency, _speed, _heading, _windFrom, _wind;
        float? _trim;
        bool _anchored, _beached, _headToWind;
        float _courseDistance, _courseBearing, _courseSpeed;
        float _pushProgress;
        GUIStyle _right, _point, _tiny, _big, _textCenter, _textRight;

        void Update()
        {
            var player = Player.m_localPlayer;
            if (player != null)
            {
                ServerSettings.RequestIfNeeded();
                if (!_namesAdded)
                {
                    _namesAdded = true;
                    SkillNamePatch.AddNow(); // the language loaded before Seaworthy could hook it
                }
            }
            _ship = player != null ? player.GetControlledShip() : null;
            if (_ship == null)
            {
                SailState.LocalShip = null; // next time at a helm, start from that ship's state
                return;
            }
            Ui.PrepareArt();
            SailingSkill.Update(player, _ship);
            if (!_tipShown && Plugin.ShowTips.Value) ShowTip(player);

            if (ServerSettings.AnchorEnabled && !Plugin.IsTyping() && Plugin.AnchorKey.Value.IsDown())
            {
                bool down = !SailState.IsAnchored(_ship);
                SailState.SendAnchor(_ship, down);
                player.Message(MessageHud.MessageType.Center, down
                    ? $"<color={Ui.Gold}>Anchor dropped</color>"
                    : $"<color={Ui.Gold}>Anchor raised</color>");
            }

            _anchored = ServerSettings.AnchorEnabled && SailState.IsAnchored(_ship);
            _trim = ServerSettings.ManualTrim ? SailState.GetTrim(_ship) : null;
            _optimal = SailState.OptimalTrim(_ship);
            _efficiency = _trim.HasValue ? SailState.ForceFactor(_trim.Value, _optimal) : 1f;

            _beached = PushOff.IsBeached(_ship, out Vector3 towardsDeep);
            _pushProgress = PushOff.Update(_ship, _beached, towardsDeep);

            _speed = Mathf.Abs(_ship.GetSpeed());
            _heading = Compass(_ship.transform.forward);
            // GetWindDir is where the wind blows to. Gusts only matter (and only show) when you trim yourself.
            _windFrom = Compass(-(_trim.HasValue ? Wind.Direction : EnvMan.instance.GetWindDir()));
            _wind = EnvMan.instance.GetWindIntensity();
            Vector3 windTo = _trim.HasValue ? Wind.Direction : EnvMan.instance.GetWindDir();
            float pointing = _trim.HasValue ? SailingSkill.Level01(_ship) : 0f;
            _headToWind = _ship.IsSailUp() && SailForcePatch.WindAngleFactor(windTo, _ship.transform.forward, pointing) < 0.05f;

            Vector3 pos = _ship.transform.position;
            Course.CheckArrival(pos);
            if (Course.Active)
            {
                _courseDistance = Utils.DistanceXZ(pos, Course.Target);
                _courseBearing = Course.Bearing(pos);
                Vector3 toTarget = Course.Target - pos;
                toTarget.y = 0f;
                // Speed made good towards the target, for the ETA.
                _courseSpeed = Vector3.Dot(SailState.Body(_ship)?.linearVelocity ?? Vector3.zero, toTarget.normalized);
            }
        }

        static void ShowTip(Player player)
        {
            _tipShown = true;
            var tips = new System.Collections.Generic.List<string>();
            if (ServerSettings.ManualTrim) tips.Add($"{Plugin.TrimModifier.Value} + A/D trims the sail");
            if (ServerSettings.AnchorEnabled) tips.Add($"{Plugin.AnchorKey.Value} drops the anchor");
            if (Plugin.CourseModifier.Value != KeyCode.None) tips.Add($"{Plugin.CourseModifier.Value} + click on the map sets a course");
            if (tips.Count > 0)
                player.Message(MessageHud.MessageType.TopLeft, $"<color={Ui.Gold}>Seaworthy</color>: " + string.Join(", ", tips));
        }

        // 0 = north (+z), clockwise.
        static float Compass(Vector3 dir) => Mathf.Repeat(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 360f);

        void OnGUI()
        {
            if (_ship == null || !Plugin.ShowHelmPanel.Value) return;
            if (Hud.IsUserHidden() || Menu.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen()) return;
            InitStyles();

            bool showTrim = ServerSettings.ManualTrim && _ship.IsSailUp();
            Matrix4x4 oldMatrix = Ui.BeginScaled(out float sw, out float sh);

            const float w = 440f, pad = 14f, compassH = 30f, statsH = 30f, trimH = 52f, anchorH = 26f, beachedH = 34f, courseH = 26f;
            bool course = Course.Active;
            float h = pad + compassH + 6f + statsH + (course ? courseH : 0f) + (showTrim ? trimH : 0f) + (_anchored ? anchorH : 0f) +
                      (_beached ? beachedH : 0f) + 8f;
            float x = (sw - w) * 0.5f;
            float y = Plugin.PanelY.Value * (sh - h);
            float inner = w - pad * 2f;
            Ui.Panel(new Rect(x, y, w, h));
            float cy = y + pad - 4f;

            DrawCompass(new Rect(x + pad, cy, inner, compassH));
            cy += compassH + 6f;

            DrawStats(new Rect(x + pad, cy, inner, statsH));
            cy += statsH;

            if (course)
            {
                Ui.Divider(x + pad, cy, inner);
                float turn = Mathf.DeltaAngle(_heading, _courseBearing);
                string steer = Mathf.Abs(turn) < 5f ? $"<color={Ui.Good}>on course</color>"
                    : turn > 0f ? $"steer <b>{turn:0}°</b> right" : $"steer <b>{-turn:0}°</b> left";
                Ui.Label(new Rect(x + pad, cy + 2f, inner, 22f),
                    $"<color={Ui.Gold}>{Course.Name}</color>  {Course.FormatDistance(_courseDistance)}  <color={Ui.Muted}>|</color>  {steer}",
                    Ui.Text);
                GUI.Label(new Rect(x + pad, cy + 2f, inner, 22f), $"ETA {Course.FormatEta(_courseDistance, _courseSpeed)}", _right);
                cy += courseH;
            }

            if (showTrim)
            {
                Ui.Divider(x + pad, cy, inner);
                cy += 6f;
                if (_trim.HasValue)
                {
                    int percent = Mathf.RoundToInt(_efficiency * 100f);
                    string color = _efficiency >= 1f ? Ui.Good : _efficiency >= 0.75f ? Ui.Gold : _efficiency >= 0.4f ? Ui.Warn : Ui.Bad;
                    Ui.Label(new Rect(x + pad, cy, inner, 22f), $"Sail trim  <color={color}><b>{percent}%</b></color>", Ui.Text);
                }
                else
                {
                    Ui.Label(new Rect(x + pad, cy, inner, 22f), $"Sail trim  <color={Ui.Muted}>auto</color>", Ui.Text);
                }
                GUI.Label(new Rect(x + pad, cy, inner, 22f), $"{Plugin.TrimModifier.Value} + A/D to trim", _right);
                cy += 26f;
                DrawTrimBar(new Rect(x + pad, cy, inner, 14f));
                cy += trimH - 32f;
            }

            if (_anchored)
            {
                Ui.Divider(x + pad, cy, inner);
                Ui.Label(new Rect(x + pad, cy + 2f, inner, 24f),
                    $"<color={Ui.Warn}>Anchored</color>  <size=12><color={Ui.Muted}>({Plugin.AnchorKey.Value} to raise)</color></size>", Ui.Text);
                cy += anchorH;
            }

            if (_beached)
            {
                Ui.Divider(x + pad, cy, inner);
                Ui.Label(new Rect(x + pad, cy + 2f, inner, 22f),
                    $"<color={Ui.Warn}>Beached</color>  <size=12><color={Ui.Muted}>hold {Plugin.PushOffKey.Value} to push off</color></size>", Ui.Text);
                if (_pushProgress > 0f) Ui.Bar(new Rect(x + pad, cy + 25f, inner, 6f), _pushProgress, Ui.GoldColor);
            }

            GUI.matrix = oldMatrix;
        }

        void InitStyles()
        {
            if (_right != null) return;
            _right = new GUIStyle(Ui.Small) { alignment = TextAnchor.MiddleRight };
            _point = new GUIStyle(Ui.Small) { alignment = TextAnchor.MiddleCenter };
            _tiny = new GUIStyle(Ui.Small) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
            _big = new GUIStyle(Ui.Text) { fontSize = 20 };
            _textCenter = new GUIStyle(Ui.Text) { alignment = TextAnchor.MiddleCenter };
            _textRight = new GUIStyle(Ui.Text) { alignment = TextAnchor.MiddleRight };
        }

        // A strip of the horizon centred on the heading: ticks every 15°, compass points, and where the wind comes from.
        void DrawCompass(Rect r)
        {
            float cx = r.x + r.width * 0.5f, half = r.width * 0.5f;
            float Pos(float bearing) => cx + Mathf.DeltaAngle(_heading, bearing) / CompassRange * half;
            bool repaint = Event.current.type == EventType.Repaint;

            Color old = GUI.color;
            if (repaint)
            {
                GUI.color = new Color(0f, 0f, 0f, 0.3f);
                GUI.DrawTexture(new Rect(r.x, r.y + 4f, r.width, r.height - 8f), Texture2D.whiteTexture);
            }

            for (int b = 0; b < 360; b += 15)
            {
                float d = Mathf.DeltaAngle(_heading, b);
                if (Mathf.Abs(d) > CompassRange) continue;
                float px = Pos(b);
                float fade = 1f - Mathf.Abs(d) / CompassRange * 0.7f; // fainter towards the ends
                if (b % 45 == 0)
                {
                    Color c = b == 0 ? Ui.GoldColor : Cream;
                    c.a = fade;
                    _point.normal.textColor = c;
                    GUI.Label(new Rect(px - 20f, r.y + 2f, 40f, r.height - 4f), Points[b / 45], _point);
                }
                else if (repaint)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.35f * fade);
                    GUI.DrawTexture(new Rect(px - 0.5f, r.y + 10f, 1f, r.height - 20f), Texture2D.whiteTexture);
                }
            }

            // Course: an outlined gold square at the target's bearing, pinned to the edge when out of view.
            if (Course.Active && repaint)
            {
                float cxp = Mathf.Clamp(Pos(_courseBearing), r.x + 6f, r.xMax - 6f);
                float cyp = r.y + r.height * 0.5f;
                GUI.color = new Color(0f, 0f, 0f, 0.7f);
                GUI.DrawTexture(new Rect(cxp - 6f, cyp - 6f, 12f, 12f), Texture2D.whiteTexture);
                GUI.color = Ui.GoldColor;
                GUI.DrawTexture(new Rect(cxp - 4.5f, cyp - 4.5f, 9f, 9f), Texture2D.whiteTexture);
                GUI.color = old;
            }

            // Wind: a blue marker where it comes from, pinned to the edge with a hint when it's behind you.
            float wd = Mathf.DeltaAngle(_heading, _windFrom);
            if (repaint)
            {
                float wx = Mathf.Clamp(Pos(_windFrom), r.x + 6f, r.xMax - 6f);
                GUI.color = new Color(WindColor.r, WindColor.g, WindColor.b, 0.9f);
                GUI.DrawTexture(new Rect(wx - 2f, r.y + 3f, 4f, r.height - 6f), Texture2D.whiteTexture);

                // Heading marker top and bottom.
                GUI.color = Ui.GoldColor;
                GUI.DrawTexture(new Rect(cx - 1f, r.y, 2f, 7f), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(cx - 1f, r.yMax - 7f, 2f, 7f), Texture2D.whiteTexture);
                GUI.color = old;
            }
            if (Mathf.Abs(wd) > CompassRange)
            {
                _tiny.normal.textColor = WindColor;
                GUI.Label(new Rect(wd < 0 ? r.x + 10f : r.xMax - 56f, r.y, 46f, r.height), wd < 0 ? "« wind" : "wind »", _tiny);
            }
            GUI.color = old;
        }

        void DrawStats(Rect r)
        {
            string speed;
            switch (Plugin.SpeedUnits.Value)
            {
                case SpeedUnit.KilometresPerHour: speed = $"<b>{_speed * 3.6f:0}</b> <size=13>km/h</size>"; break;
                case SpeedUnit.MetresPerSecond: speed = $"<b>{_speed:0.0}</b> <size=13>m/s</size>"; break;
                default: speed = $"<b>{_speed * 1.944f:0.0}</b> <size=13>kn</size>"; break;
            }
            Ui.Label(r, speed, _big);

            string mode;
            switch (_ship.GetSpeedSetting())
            {
                case Ship.Speed.Full: mode = "Full sail"; break;
                case Ship.Speed.Half: mode = "Half sail"; break;
                case Ship.Speed.Slow: mode = "Rowing"; break;
                case Ship.Speed.Back: mode = "Rowing back"; break;
                default: mode = _anchored ? "At anchor" : "Sail down"; break;
            }
            Ui.Label(r, $"{mode}  <color={Ui.Muted}>|</color>  {_heading:000}°", _textCenter);

            string strength = _wind < 0.25f ? "calm" : _wind < 0.5f ? "light" : _wind < 0.75f ? "fresh" : "strong";
            Ui.Label(r, _headToWind
                ? $"<color={Ui.Bad}>Head to wind</color>"  // the sail can't draw: turn away from the wind
                : $"Wind <color={Ui.Info}>{strength}</color>", _textRight);
        }

        // -90°..+90° track: the green zone is the best angle for the wind, the needle is the sail. A/D moves the needle.
        void DrawTrimBar(Rect r)
        {
            Ui.Bar(r, 0f, Color.clear);
            if (Event.current.type != EventType.Repaint) return;

            float Pos(float angle) => r.x + (angle + SailState.MaxTrim) / (SailState.MaxTrim * 2f) * r.width;
            // Zone where the sail still gives 85% or more (cos² ≥ 0.85 → about 22° each way, times tolerance).
            float half = 22.8f * ServerSettings.TrimTolerance;
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
