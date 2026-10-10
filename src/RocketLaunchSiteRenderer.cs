using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class RocketLaunchSiteRenderer
{
    private static void Pad(float x, float z, float h, Color c, int gantry = 0)
    {
        Disk(x, .103f, z, .95f, .045f, Steel);
        Disk(x, .15f, z, .65f, .025f, White);
        Box(x, .11f, z + .75f, .32f, .025f, .75f, Steel);
        Disk(x, .18f, z, .14f, h, White);
        Disk(x, .18f + h * .66f, z, .142f, .12f, c);
        DrawCylinder(new(x, .18f + h, z), 0, .14f, .36f, 16, White);
        foreach (int side in new[] { -1, 1 })
        {
            Disk(x + side * .2f, .18f, z, .065f, h * .48f, White);
            DrawCylinder(new(x + side * .2f, .18f + h * .48f, z), 0, .065f, .16f, 8, c);
        }
        float tx = x + .5f;
        foreach (int side in new[] { -1, 1 })
        {
            Box(tx + side * .13f, .15f, z - .25f, .045f, h + .2f, .045f, Steel);
            for (float y = .2f; y < h; y += .3f)
            {
                Beam(new(tx - .13f, y, z - .25f), new(tx + .13f, y + .3f, z - .25f), .012f, c);
                Beam(new(tx + .13f, y, z - .25f), new(tx - .13f, y + .3f, z - .25f), .012f, White);
            }
        }
        for (float y = .5f; y < h; y += .6f) Box(x + .25f, y, z, .45f, .035f, .12f, Steel);
        if (gantry == 1) { Box(x - .55f, .1f, z - .25f, .09f, h + .2f, .09f, Steel); Box(x, h + .3f, z - .25f, 1.2f, .08f, .12f, c); }
    }
    private static void Tanks(float x, float z, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Disk(x + i * .48f, .105f, z, .18f, .45f, White);
            DrawSphereEx(new(x + i * .48f, .555f, z), .18f, 6, 10, Steel);
            Box(x + i * .48f, .13f, z + .26f, .035f, .035f, .25f, Steel);
        }
    }
    private static void Assembly(float x, float z, float w, float d, Color c)
    {
        Gable(x, z, w, d, .85f, c, .25f);
        Box(x + w * .25f, .1f, z + d / 2 + .02f, .35f, .52f, .02f, Steel);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Pad(.85f, -1.1f, 2.4f, c); Assembly(-1.85f, -.9f, 1.3f, 2.8f, c); Tanks(.2f, 1.2f, 4); break;
            case 1: Pad(-1.35f, -1.15f, 2.1f, c); Pad(1.35f, -1.15f, 1.65f, c); Hall(0, 1.45f, 3.8f, .8f, .5f, c); break;
            case 2: Pad(0, -1.4f, 2.7f, c, 1); Pool(-2.0f, .6f, .5f); Tanks(1.1f, .7f, 3); Hall(0, 1.75f, 2.3f, .6f, .45f, c); break;
            case 3: Assembly(-1.5f, -1.6f, 1.7f, 1.6f, c); Pad(1.35f, -.55f, 2.4f, c); Pool(-1.5f, .55f, .65f); break;
            case 4: Pad(0, -1.4f, 2.2f, c); Hall(-1.75f, .75f, 1.2f, 1.3f, .5f, c); Hall(1.75f, .75f, 1.2f, 1.3f, .5f, c); Tree(0, 1.4f); break;
            case 5: Pad(1.15f, -1.15f, 2.0f, c); Assembly(-1.55f, -.6f, 1.7f, 3.1f, c); Tanks(.7f, 1.2f, 3); break;
            case 6: Pad(-1.35f, -1.4f, 1.65f, c); Pad(1.35f, -1.4f, 1.65f, c); for (int i = -1; i <= 1; i++) Hall(i * 1.7f, 1.15f, 1.2f, .9f, .45f, c); break;
            case 7: Pad(0, -1.25f, 2.35f, c, 1); Hall(0, 1.35f, 4.2f, .8f, .55f, c); Solar(-1.85f, .71f, 1.35f, 12); break;
            case 8: Pad(1.0f, -1.0f, 2.65f, c, 1); Hall(-1.7f, -1.2f, 1.4f, 2.2f, 1.45f, c); Tanks(-1.9f, 1.3f, 3); break;
            case 9: Pad(0, -1.6f, 2.2f, c); Hall(-1.6f, .65f, 1.4f, 1.6f, .8f, c); Hall(1.6f, .65f, 1.4f, 1.6f, .8f, c); Atrium(0, 1.0f, 1.1f, .8f, .6f); break;
            case 10: Pad(-1.4f, -.85f, 2.5f, c); Assembly(1.4f, -1.3f, 1.65f, 2.1f, c); Hall(1.4f, 1.0f, 1.65f, .8f, .65f, c); break;
            case 11: Pad(0, -1.4f, 2.8f, c, 1); Dome(-1.7f, .85f, 1.4f, 1.4f, .5f, c); Dome(1.7f, .85f, 1.4f, 1.4f, .5f, c); break;
            case 12: Pad(.75f, -1.3f, 2.7f, c, 1); Assembly(-1.85f, -.65f, 1.15f, 3.5f, c); Tanks(.1f, 1.5f, 5); break;
            case 13: Pad(0, -1.55f, 2.1f, c); Assembly(-1.6f, .75f, 1.3f, 1.65f, c); Assembly(1.6f, .75f, 1.3f, 1.65f, c); break;
            case 14: Pad(-1.35f, -1.5f, 1.8f, c, 1); Pad(1.35f, -1.5f, 2.3f, c); Tanks(-2.05f, 1.0f, 4); Hall(1.5f, 1.15f, 1.6f, .8f, .55f, c); break;
            case 15: Pad(1.15f, -.6f, 2.6f, c, 1); Assembly(-1.6f, -.8f, 1.7f, 3.2f, c); Tanks(.2f, 1.65f, 4); break;
        }
        Parking(2.45f, c); Hall(-2.4f, 2.45f, .45f, .45f, .32f, c);
        // A fixed launch-ready display: no time-dependent launch or design replacement.
        Rlgl.PopMatrix();
    }
}
