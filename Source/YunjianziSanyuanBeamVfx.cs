using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Yunjianzi
{
    // Animated geometry rather than a static beam picture: all layers follow the
    // same clipped ray used by the sweep's hit detection.
    [StaticConstructorOnStartup]
    internal static class SanyuanBeamVfx
    {
        private static readonly Material Ribbon = CreateRibbon();
        private static readonly Material Flash = MaterialPool.MatFrom("Things/Mote/LightningGlow", ShaderDatabase.MoteGlow);
        private static readonly MaterialPropertyBlock Tint = new MaterialPropertyBlock();
        private static readonly Color Gold = new Color(1f, .76f, .32f);
        private static readonly Color Ivory = new Color(1f, .98f, .88f);

        private static Material CreateRibbon()
        {
            Texture2D texture = new Texture2D(64, 1, TextureFormat.RGBA32, false);
            texture.name = "YJZ_SanyuanBeamGradient";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                float distance = Mathf.Abs(i / 63f * 2f - 1f);
                pixels[i] = new Color(1f, 1f, 1f, Mathf.Pow(1f - distance, 1.7f));
            }
            texture.SetPixels(pixels); texture.Apply(false, true);
            return MaterialPool.MatFrom(texture, ShaderDatabase.MoteGlow, Color.white);
        }

        internal static void Line(Vector3 start, Vector3 end, float width, Color color, float opacity)
        {
            start.y = end.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Vector3 delta = end - start;
            if (delta.sqrMagnitude < .0001f) return;
            Tint.SetColor("_Color", new Color(color.r, color.g, color.b, opacity));
            Matrix4x4 matrix = Matrix4x4.TRS((start + end) * .5f, Quaternion.LookRotation(delta), new Vector3(width, 1f, delta.magnitude));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Ribbon, 0, null, 0, Tint);
        }

        private static void Spark(Vector3 position, float size, float opacity)
        {
            position.y = AltitudeLayer.MoteOverhead.AltitudeFor() + .002f;
            Tint.SetColor("_Color", new Color(1f, .88f, .56f, opacity));
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(position, Quaternion.identity, new Vector3(size, 1f, size)), Flash, 0, null, 0, Tint);
        }

        private static void Diamond(Vector3 position, Vector3 forward, Vector3 side, float size, float alpha)
        {
            Vector3 a = position + forward * size, b = position + side * size * .42f;
            Vector3 c = position - forward * size, d = position - side * size * .42f;
            Line(a, b, .045f, Gold, alpha); Line(b, c, .045f, Gold, alpha);
            Line(c, d, .045f, Gold, alpha); Line(d, a, .045f, Gold, alpha);
        }

        internal static void Draw(Vector3 start, Vector3 end, int pass, int age, IReadOnlyList<Vector3> trail)
        {
            Vector3 forward = end - start; forward.y = 0f;
            if (forward.sqrMagnitude < .01f) return;
            float length = forward.magnitude; forward /= length;
            Vector3 side = new Vector3(-forward.z, 0f, forward.x);
            float time = age / 60f;
            float pulse = 1f + .08f * Mathf.Sin(age * .8f);
            float power = pass == 2 ? 1.3f : 1f;
            // Recent ray positions form the fading fan behind the moving beam.
            for (int i = 0; i < trail.Count; i++)
                Line(start, trail[i], .65f, Gold, .035f + .10f * (i + 1) / trail.Count);
            Line(start, end, 1.05f * power, Gold, .22f);
            Line(start, end, .42f * power * pulse, Gold, .85f);
            Line(start, end, .19f * power, Ivory, 1f);
            // Fine animated gold filaments, clipped short of the impact surface.
            for (int strand = 0; strand < 2; strand++)
            {
                Vector3 previous = start;
                for (int i = 1; i <= 24; i++)
                {
                    float t = i / 24f;
                    float offset = Mathf.Sin(t * Mathf.PI * 4f - time * 12f + strand * Mathf.PI) * .30f * Mathf.Sin(t * Mathf.PI);
                    Vector3 current = Vector3.Lerp(start, end, t) + side * offset;
                    Line(previous, current, .035f, Gold, .65f); previous = current;
                }
            }
            Spark(start, .8f + .15f * pulse, .85f);
            Spark(end, .75f * power, .7f);
            Diamond(start, forward, side, .50f + .08f * pulse, .95f);
            // Travelling geometric motes communicate direction even in the straight pass.
            for (int i = 0; i < 4; i++)
            {
                float t = Mathf.Repeat(time * 1.7f + i * .25f, 1f);
                if (length * t < .6f || length * (1f - t) < .6f) continue;
                Vector3 p = Vector3.Lerp(start, end, t) + side * (i % 2 == 0 ? .48f : -.48f);
                Diamond(p, forward, side, .13f, .8f);
            }
            // An angular wavefront rather than an explosion at the far end.
            for (int i = -2; i <= 2; i++)
            {
                Vector3 p = end + side * i * .36f - forward * (Mathf.Abs(i) * .16f);
                Line(p - side * .13f - forward * .20f, p, .065f, Gold, .85f);
            }
        }
    }
}
