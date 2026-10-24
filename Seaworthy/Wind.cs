using UnityEngine;

namespace Seaworthy
{
    // Gusts: the wind's direction wanders a few degrees and its strength surges and drops, more so in strong wind
    // and storms. They only affect a sail you trim yourself (a ship on auto sails exactly like vanilla), so a long
    // voyage keeps you watching the trim. Driven by the shared server clock, so every client sees the same gusts.
    internal static class Wind
    {
        static float Now => ZNet.instance != null ? (float)(ZNet.instance.GetTimeSeconds() % 100000.0) : Time.time;

        // How strongly gusts play right now: calm air barely, a storm fully.
        static float Gustiness => ServerSettings.GustsEnabled ? Mathf.Lerp(0.3f, 1f, EnvMan.instance.GetWindIntensity()) : 0f;

        // Degrees the gust turns the wind (positive = clockwise from above).
        internal static float GustAngle =>
            (Mathf.PerlinNoise(Now * 0.025f, 0.37f) - 0.5f) * 2f * ServerSettings.GustAngle * Gustiness;

        // Multiplier on the wind's push: 1 on average, up to ±15% in a storm.
        internal static float GustStrength =>
            1f + (Mathf.PerlinNoise(Now * 0.06f, 7.13f) - 0.5f) * 2f * 0.15f * Gustiness;

        // Where the wind blows to, including the gust.
        internal static Vector3 Direction => Quaternion.Euler(0f, GustAngle, 0f) * EnvMan.instance.GetWindDir();
    }
}
