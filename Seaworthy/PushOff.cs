using UnityEngine;

namespace Seaworthy
{
    // A beached ship can be pushed back into the water from the helm: hold the push-off key and the ship gets a
    // shove towards the deepest water nearby, like the crew jumping out and pushing.
    internal static class PushOff
    {
        const string PushRpc = "Seaworthy_PushOff";
        const float HoldTime = 1f;
        const float ProbeDistance = 9f;
        const float ShallowDepth = 1.2f; // water shallower than this under the hull counts as beached
        const float PushSpeed = 3.5f;    // m/s added by one push
        static float _held;

        internal static void Register(ZNetView nview, Ship ship)
        {
            nview.Register<Vector3>(PushRpc, (sender, dir) =>
            {
                if (!nview.IsOwner()) return;
                var body = SailState.Body(ship);
                if (body == null) return;
                dir.y = 0f;
                body.AddForce(dir.normalized * PushSpeed + Vector3.up * 0.8f, ForceMode.VelocityChange);
            });
        }

        // Water depth at a point, or 0 on land.
        static float Depth(Vector3 p)
        {
            var zs = ZoneSystem.instance;
            if (zs == null || !zs.GetGroundHeight(p, out float ground)) return 0f;
            return Mathf.Max(0f, zs.m_waterLevel - ground);
        }

        // Beached: hardly moving with shallow water under the hull. Returns the way to deeper water.
        internal static bool IsBeached(Ship ship, out Vector3 towardsDeep)
        {
            towardsDeep = Vector3.zero;
            if (!ServerSettings.PushOffEnabled || Mathf.Abs(ship.GetSpeed()) > 0.6f) return false;
            Vector3 pos = ship.transform.position;
            if (Depth(pos) > ShallowDepth) return false;

            float best = 0f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                float depth = Depth(pos + dir * ProbeDistance);
                if (depth > best)
                {
                    best = depth;
                    towardsDeep = dir;
                }
            }
            return best > Depth(pos) + 0.3f;
        }

        // Helmsman: hold the key to push. Returns hold progress 0..1 for the helm panel.
        internal static float Update(Ship ship, bool beached, Vector3 towardsDeep)
        {
            if (!beached || Plugin.IsTyping() || !Plugin.PushOffKey.Value.IsPressed())
            {
                _held = 0f;
                return 0f;
            }
            _held += Time.deltaTime;
            if (_held < HoldTime) return _held / HoldTime;
            _held = 0f;
            SailState.NView(ship)?.InvokeRPC(PushRpc, towardsDeep);
            ship.m_waterImpactEffect.Create(ship.transform.position, Quaternion.identity);
            return 1f;
        }
    }
}
