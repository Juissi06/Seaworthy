using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Seaworthy
{
    internal enum SpeedUnit { Knots, KilometresPerHour, MetresPerSecond }

    [BepInPlugin(Guid, "Seaworthy", ModInfo.Version)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim_server.exe")] // the server decides the sailing rules
    public class Plugin : BaseUnityPlugin
    {
        // Also names the config file: BepInEx/config/Seaworthy.cfg.
        const string Guid = "Seaworthy";

        // General
        internal static ConfigEntry<bool> ModEnabled;
        internal static ConfigEntry<float> UiScale;

        // Sail trim
        internal static ConfigEntry<bool> ManualTrim;
        internal static ConfigEntry<KeyCode> TrimModifier;
        internal static ConfigEntry<bool> GamepadTrim;
        internal static ConfigEntry<float> TrimSpeed;
        internal static ConfigEntry<float> PerfectTrimBonus;
        internal static ConfigEntry<float> TrimTolerance;

        // Helm panel
        internal static ConfigEntry<bool> ShowHelmPanel;
        internal static ConfigEntry<float> PanelY;
        internal static ConfigEntry<SpeedUnit> SpeedUnits;
        internal static ConfigEntry<bool> ShowTips;
        internal static ConfigEntry<bool> ShipLanterns;
        internal static ConfigEntry<bool> ShipNamesEnabled;
        internal static ConfigEntry<KeyCode> RenameModifier;

        // Anchor
        internal static ConfigEntry<bool> AnchorEnabled;
        internal static ConfigEntry<KeyboardShortcut> AnchorKey;
        internal static ConfigEntry<float> AnchorHold;

        // Push off
        internal static ConfigEntry<bool> PushOffEnabled;
        internal static ConfigEntry<KeyboardShortcut> PushOffKey;

        // Course
        internal static ConfigEntry<KeyCode> CourseModifier;

        // Wind
        internal static ConfigEntry<bool> GustsEnabled;
        internal static ConfigEntry<float> GustAngle;

        // Lookout
        internal static ConfigEntry<float> LookoutBonus;

        // Sailing skill
        internal static ConfigEntry<bool> SkillEnabled;
        internal static ConfigEntry<float> SkillBonus;

        // Server
        internal static ConfigEntry<bool> EnforceOnClients;

        Harmony _harmony, _skillHarmony;
        GameObject _root;

        void Awake()
        {
            ModEnabled = Config.Bind("General", "Enabled", true,
                "Turn Seaworthy on or off. Off means vanilla sailing. Everyone on a ship should use the same setting.");
            UiScale = Config.Bind("General", "UI scale", 1f,
                new ConfigDescription("Size of the helm panel.", new AcceptableValueRange<float>(0.5f, 2f)));

            const string trim = "Sail Trim";
            ManualTrim = Config.Bind(trim, "Manual sail trim", true,
                "Set the sail angle yourself instead of the game doing it. A well trimmed sail is faster than vanilla, a badly trimmed one is slower.");
            TrimModifier = Config.Bind(trim, "Trim modifier", KeyCode.LeftShift,
                "Hold this at the helm to turn the sail with the steering keys (A/D) instead of the rudder.");
            GamepadTrim = Config.Bind(trim, "Gamepad trim", true,
                "On a gamepad, hold the left trigger at the helm and use the stick to trim the sail.");
            TrimSpeed = Config.Bind(trim, "Trim speed", 45f,
                new ConfigDescription("How fast the sail turns, in degrees per second.", new AcceptableValueRange<float>(10f, 180f)));
            PerfectTrimBonus = Config.Bind(trim, "Perfect trim bonus", 0.15f,
                new ConfigDescription("Extra sail force with a perfectly trimmed sail, on top of vanilla. 0 = never faster than vanilla.",
                    new AcceptableValueRange<float>(0f, 0.5f)));
            TrimTolerance = Config.Bind(trim, "Trim tolerance", 1f,
                new ConfigDescription("How forgiving a badly trimmed sail is. Higher keeps more speed when the sail is off.",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            const string panel = "Helm Panel";
            ShowHelmPanel = Config.Bind(panel, "Show helm panel", true,
                "At the helm, show a panel with a compass, speed, wind, sail trim and anchor.");
            PanelY = Config.Bind(panel, "Position Y", 0.80f,
                new ConfigDescription("Vertical position of the helm panel. 0 = top edge, 1 = bottom edge.",
                    new AcceptableValueRange<float>(0f, 1f)));
            SpeedUnits = Config.Bind(panel, "Speed unit", SpeedUnit.Knots, "Unit for the ship's speed.");
            ShipLanterns = Config.Bind("General", "Ship lanterns", true,
                "After dark, ships carry a warm lantern glow at the stern. Visual only, on your screen.");
            ShipNamesEnabled = Config.Bind("General", "Ship names", true,
                "Name your ships: hold the rename modifier and press Use at the helm. Everyone sees the name.");
            RenameModifier = Config.Bind("General", "Rename modifier", KeyCode.LeftShift,
                "Hold this and press Use at a helm to name the ship.");
            ShowTips = Config.Bind(panel, "Show tips", true, "The first time you take a helm each session, list Seaworthy's controls.");

            const string anchor = "Anchor";
            AnchorEnabled = Config.Bind(anchor, "Enabled", true, "Let the helmsman drop an anchor that stops the ship and holds it in place.");
            AnchorKey = Config.Bind(anchor, "Anchor key", new KeyboardShortcut(KeyCode.G),
                "At the helm: drop or raise the anchor.");
            AnchorHold = Config.Bind(anchor, "Holding strength", 0.6f,
                new ConfigDescription("How quickly a dropped anchor stops the ship. Higher stops it faster.",
                    new AcceptableValueRange<float>(0.1f, 3f)));

            PushOffEnabled = Config.Bind("Push Off", "Enabled", true,
                "At the helm of a beached ship, hold the push-off key to shove it back towards deeper water.");
            PushOffKey = Config.Bind("Push Off", "Push-off key", new KeyboardShortcut(KeyCode.B),
                "Hold at the helm of a beached ship to push off.");

            CourseModifier = Config.Bind(panel, "Course modifier", KeyCode.LeftAlt,
                "Hold this and click a pin (or any spot) on the large map to set a course; click it again to clear it. None = off.");

            GustsEnabled = Config.Bind("Wind", "Gusts", true,
                "The wind wanders and surges, more in storms, so a sail you trim yourself needs attention. Ships on auto trim aren't affected.");
            GustAngle = Config.Bind("Wind", "Gust angle", 12f,
                new ConfigDescription("How far gusts turn the wind, in degrees, in a storm. Calm air turns about a third of this.",
                    new AcceptableValueRange<float>(0f, 30f)));

            LookoutBonus = Config.Bind("Lookout", "Map reveal bonus at sea", 1f,
                new ConfigDescription("While aboard, the map reveals this much more around you, scaled by ship size: " +
                    "1 = a longship reveals twice the normal radius, a raft about 20% more. 0 = vanilla.",
                    new AcceptableValueRange<float>(0f, 3f)));

            SkillEnabled = Config.Bind("Sailing Skill", "Enabled", true,
                "A Sailing skill that levels while you steer a ship under sail (rowing counts half).");
            SkillBonus = Config.Bind("Sailing Skill", "Speed bonus at level 100", 0.2f,
                new ConfigDescription("Extra sail force and rowing speed at skill 100, scaling up from 0 at skill 0.",
                    new AcceptableValueRange<float>(0f, 0.5f)));

            EnforceOnClients = Config.Bind("Server", "Enforce on clients", true,
                "Server/host only: players on your server use your sail trim and anchor settings instead of their own, " +
                "so nobody sails faster with their own config.");
            Config.SettingChanged += (s, e) => ServerSettings.Broadcast(); // server only, see Broadcast

            _root = new GameObject("Seaworthy");
            DontDestroyOnLoad(_root);
            _root.AddComponent<Helm>();

            _skillHarmony = new Harmony(Guid + ".skill");
            SkillPatches.Apply(_skillHarmony);
            _harmony = new Harmony(Guid);
            ModEnabled.SettingChanged += (s, e) => ApplyEnabled();
            ApplyEnabled();
        }

        bool _patched;

        // Off removes every patch, so the ship sails exactly like vanilla.
        void ApplyEnabled()
        {
            bool on = ModEnabled.Value;
            if (on && !_patched) _harmony.PatchAll(typeof(Plugin).Assembly);
            else if (!on && _patched)
            {
                _harmony.UnpatchSelf();
                AnchorVisuals.HideAll();
                ShipLights.HideAll();
            }
            _patched = on;
            _root.SetActive(on);
            Logger.LogInfo(on ? "Enabled." : "Disabled: vanilla sailing.");
        }

        void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            _skillHarmony?.UnpatchSelf();
            if (_root != null) Destroy(_root);
        }

        internal static bool IsTyping() =>
            (Chat.instance != null && Chat.instance.HasFocus()) ||
            global::Console.IsVisible() || TextInput.IsVisible() || Menu.IsVisible();
    }
}
