using System;
using UnityEngine;

public class DebugExtra {
    private static readonly Vector4[] s_UnitSphere = MakeUnitSphere(len: 16);

    public static void DrawSphere(Vector4 pos, float radius, Color color, float duration = 0.01f) {
        var v = s_UnitSphere;
        var len = s_UnitSphere.Length / 3;
        for (var i = 0; i < len; i++) {
            var sX = pos + radius * v[0 * len + i];
            var eX = pos + radius * v[0 * len + (i + 1) % len];
            var sY = pos + radius * v[1 * len + i];
            var eY = pos + radius * v[1 * len + (i + 1) % len];
            var sZ = pos + radius * v[2 * len + i];
            var eZ = pos + radius * v[2 * len + (i + 1) % len];
            Debug.DrawLine(sX, eX, color, duration);
            Debug.DrawLine(sY, eY, color, duration);
            Debug.DrawLine(sZ, eZ, color, duration);
        }
    }

    private static Vector4[] MakeUnitSphere(int len) {
        Debug.Assert(len > 2);
        var v = new Vector4[len * 3];
        for (var i = 0; i < len; i++) {
            var f = i / (float)len;
            var c = Mathf.Cos(f * (float)(Math.PI * 2.0));
            var s = Mathf.Sin(f * (float)(Math.PI * 2.0));
            v[0 * len + i] = new Vector4(c, s, z: 0, w: 1);
            v[1 * len + i] = new Vector4(x: 0, c, s, w: 1);
            v[2 * len + i] = new Vector4(s, y: 0, c, w: 1);
        }

        return v;
    }
}