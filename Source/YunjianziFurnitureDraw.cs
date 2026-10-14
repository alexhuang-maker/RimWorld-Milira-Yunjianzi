using UnityEngine;
using Verse;

namespace Yunjianzi
{
    /// <summary>
    /// Applies the large artwork's map offset explicitly. This avoids relying
    /// on inherited GraphicData merging and makes the adjustment visible for
    /// existing buildings as soon as the assembly is reloaded.
    /// </summary>
    public sealed class Building_CelestialDaoGate : Building
    {
        // In this realtime draw path, reducing Z moves the artwork visually
        // backward. This value is intentionally easy to tune from screenshots.
        private static readonly Vector3 ArtworkOffset = new Vector3(0f, 0f, 1.00f);

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc + ArtworkOffset, flip);
        }
    }
}
