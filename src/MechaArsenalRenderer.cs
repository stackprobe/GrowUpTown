using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class MechaArsenalRenderer
{
    private static void Mecha(float x, float z, Color c, int model = 0)
    {
        Disk(x, .105f, z, .6f, .025f, Steel);
        foreach (int side in new[] { -1, 1 })
        {
            Box(x + side * .17f, .13f, z, .14f, .5f, .18f, White);
            Box(x + side * .17f, .13f, z + .08f, .18f, .09f, .3f, Steel);
            Box(x + side * .17f, .4f, z + .11f, .16f, .13f, .035f, c);
            Box(x + side * .36f, .67f, z, .15f, .4f, .17f, Steel);
            Box(x + side * .36f, .97f, z, .2f, .18f, .22f, c);
        }
        Box(x, .62f, z, .4f, .13f, .25f, Steel);
        Box(x, .76f, z, model == 1 ? .55f : .45f, .37f, .29f, c);
        Box(x, .82f, z + .16f, .25f, .16f, .025f, Glass);
        Box(x, 1.15f, z, .22f, .2f, .22f, White);
        Box(x, 1.21f, z + .12f, .17f, .055f, .015f, Glass);
        if (model == 2) Box(x, .8f, z - .25f, .35f, .4f, .2f, White);
        if (model == 1) for (int side = -1; side <= 1; side += 2) Box(x + side * .1f, 1.35f, z, .02f, .15f, .02f, c);
    }
    private static void Assembly(float x, float z, float w, float d, float h, Color c)
    {
        Gable(x, z, w, d, h, c, .35f);
        // A distinct equipment bay alongside the standard pedestrian entrance.
        Box(x + w * .27f, .1f, z + d / 2 + .03f, .45f, .6f, .025f, Steel);
        for (int i = 0; i < 7; i++) Box(x + w * .27f, .13f + i * .075f, z + d / 2 + .05f, .42f, .012f, .012f, White);
    }
    private static void Gantry(float x, float z, float w, Color c)
    {
        foreach (int side in new[] { -1, 1 })
        {
            Box(x + side * w / 2, .1f, z, .075f, 1.6f, .10f, Steel);
            Box(x + side * w / 2, .1f, z, .17f, .07f, 1.2f, Steel);
        }
        Box(x, 1.7f, z, w + .25f, .14f, .18f, c);
        Beam(new(x, 1.7f, z), new(x, 1.2f, z), .012f, Steel);
    }
    private static void Equipment(float x, float z, int count, Color c)
    {
        for (int i = 0; i < count; i++)
        {
            float px = x + i % 3 * .48f, pz = z + i / 3 * .65f;
            Box(px, .1f, pz, .35f, .3f, .5f, White); Box(px, .4f, pz, .36f, .035f, .51f, c);
            Box(px, .15f, pz + .26f, .22f, .12f, .015f, Steel);
        }
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Assembly(-1.9f, -1.5f, 2.8f, 4.0f, 1.1f, c); Mecha(1.9f, -.9f, c); Gantry(1.9f, -.9f, 1.8f, c); Pool(1.9f, 1.7f, .65f); break;
            case 1: Assembly(0, -2.5f, 6.6f, 1.8f, 1.05f, c); Mecha(-1.75f, .7f, c); Mecha(1.75f, .7f, c, 1); Gantry(0, .7f, 5.0f, c); break;
            case 2: Assembly(-2.4f, -1.5f, 1.8f, 3.9f, 1.2f, c); Mecha(.2f, -.8f, c, 2); Mecha(2.4f, -.8f, c); Equipment(-.1f, 1.5f, 6, c); break;
            case 3: Assembly(0, -2.5f, 6.5f, 1.8f, 1.4f, c); Mecha(0, .6f, c, 1); Pool(-2.5f, .9f, .6f); Pool(2.5f, .9f, .6f); break;
            case 4: Assembly(0, -2.4f, 6.5f, 1.8f, .95f, c); Mecha(-1.7f, .7f, c); Mecha(1.7f, .7f, c); Tree(-3.2f, 1.6f); Tree(3.2f, 1.6f); break;
            case 5: Hall(-1.8f, -1.5f, 3.0f, 3.9f, .9f, c); Box(-1.8f, 1.055f, -1.5f, 2.7f, .04f, 3.6f, Green); Mecha(1.9f, -.8f, c, 2); Equipment(1.2f, 1.2f, 6, c); break;
            case 6: for (int i = -1; i <= 1; i++) { Assembly(i * 2.3f, -2.2f, 1.9f, 2.2f, .9f, c); Mecha(i * 2.3f, .8f, c, i + 1); } break;
            case 7: Assembly(-1.9f, -1.4f, 2.9f, 4.0f, 1.0f, c); Solar(-3.05f, 1.52f, -1.4f, 7); Mecha(1.85f, -.8f, c); Gantry(1.85f, -.8f, 1.8f, c); break;
            case 8: Assembly(-1.85f, -1.5f, 2.9f, 4.0f, 1.7f, c); Mecha(1.8f, -.7f, c, 1); Hall(1.8f, 1.7f, 2.5f, .8f, .6f, c); break;
            case 9: Assembly(-2.4f, -1.5f, 1.8f, 4.0f, 1.15f, c); Assembly(2.4f, -1.5f, 1.8f, 4.0f, 1.15f, c); Mecha(0, -.6f, c, 2); Atrium(0, -2.5f, 2.1f, .75f, .8f); break;
            case 10: for (int i = -1; i <= 1; i++) Assembly(i * 2.3f, -2.3f, 1.9f, 2.0f, .9f + (i + 1) * .3f, c); Mecha(-1.6f, .9f, c); Mecha(1.6f, .9f, c, 1); break;
            case 11: Dome(-1.9f, -1.7f, 2.7f, 3.0f, 1.0f, c); Mecha(1.75f, -.5f, c, 2); Gantry(1.75f, -.5f, 2.1f, c); Equipment(-2.9f, 1.5f, 6, c); break;
            case 12: Assembly(-2.1f, -1.5f, 2.4f, 4.0f, 1.4f, c); Mecha(1.2f, -.9f, c, 1); Mecha(3.0f, 1.0f, c, 2); Gantry(1.2f, -.9f, 2.0f, c); break;
            case 13: Assembly(0, -2.4f, 6.5f, 1.9f, 1.0f, c); Solar(-2.8f, 1.52f, -2.4f, 18); Mecha(-2.1f, .9f, c); Mecha(0, .9f, c, 1); Mecha(2.1f, .9f, c, 2); break;
            case 14: Assembly(-2.4f, -1.6f, 1.9f, 3.7f, 1.3f, c); Mecha(.3f, -.6f, c); Mecha(2.55f, -.6f, c, 2); Gantry(1.4f, -.6f, 3.6f, c); break;
            case 15: Assembly(-1.9f, -1.5f, 2.8f, 4.0f, 1.25f, c); Hall(1.9f, -2.3f, 2.5f, 1.5f, .75f, c); Mecha(1.9f, .7f, c, 1); Gantry(1.9f, .7f, 2.1f, c); break;
        }
        Hall(-3.0f, 3.65f, .65f, .55f, .35f, c); Parking(3.65f, c);
        // Mecha and gantries are fixed equipment; pedestrian doors and cars remain small.
        Rlgl.PopMatrix();
    }
}
