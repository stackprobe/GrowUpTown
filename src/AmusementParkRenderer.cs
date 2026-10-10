using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class AmusementParkRenderer
{
    private static void Carousel(float x, float z, Color c)
    {
        Disk(x, .1f, z, .65f, .07f, White);
        Disk(x, .17f, z, .06f, .65f, Steel);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8, px = x + MathF.Cos(a) * .46f, pz = z + MathF.Sin(a) * .46f;
            Box(px, .17f, pz, .018f, .62f, .018f, White);
            Box(px, .30f, pz, .10f, .09f, .20f, c);
        }
        DrawCylinder(new(x, .80f, z), 0, .72f, .3f, 16, c);
    }
    private static void Coaster(float x, float z, float w, float d, Color c, bool eight = false)
    {
        Vector3 Point(float a, float offset = 0) => new(x + MathF.Cos(a) * w / 2,
            .42f + (1 + MathF.Sin(a * 2)) * .38f + offset, z + MathF.Sin(a * (eight ? 2 : 1)) * d / 2);
        for (int i = 0; i < 48; i++)
        {
            float a = i * MathF.Tau / 48, b = (i + 1) * MathF.Tau / 48;
            Beam(Point(a), Point(b), .035f, c); Beam(Point(a, -.09f), Point(b, -.09f), .025f, White);
            if (i % 4 == 0) { var p = Point(a); Beam(new(p.X, .1f, p.Z), p, .024f, Steel); }
            if (i is 5 or 7 or 9) { var p = Point(a); Box(p.X, p.Y + .03f, p.Z, .16f, .13f, .23f, c); }
        }
    }
    private static void Tower(float x, float z, Color c)
    {
        Disk(x, .1f, z, .42f, .06f, White); Box(x, .16f, z, .13f, 2.0f, .13f, Steel);
        Disk(x, 1.2f, z, .32f, .1f, c); Disk(x, 2.16f, z, .24f, .08f, c);
        for (int i = -1; i <= 1; i++) { Box(x + i * .19f, 1.3f, z + .27f, .12f, .13f, .1f, White); Box(x + i * .19f, 1.3f, z - .27f, .12f, .13f, .1f, White); }
    }
    private static void Wheel(float x, float z, Color c) => FerrisWheelRenderer.Wheel(x, z, .75f, 8, c, 0);
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Coaster(0, -1.35f, 4.8f, 2.2f, c); Pool(-1.7f, 1.0f, .55f); Carousel(.15f, .9f, c); Wheel(1.9f, .7f, c); break;
            case 1: Wheel(-1.55f, -1.35f, c); Wheel(1.55f, -1.35f, c); Pool(0, .2f, .6f); Carousel(-1.5f, 1.25f, c); Tower(1.65f, 1.15f, c); break;
            case 2: Coaster(-1.35f, -.5f, 2.1f, 3.8f, c); Pool(1.25f, -1.55f, .8f); Carousel(1.25f, .75f, c); break;
            case 3: Wheel(0, -1.65f, c); Coaster(0, .55f, 4.5f, 1.5f, c, true); Pool(-2.2f, -1.65f, .35f); Pool(2.2f, -1.65f, .35f); break;
            case 4: Coaster(0, -1.25f, 4.6f, 2.3f, c, true); Carousel(-1.35f, 1.0f, c); Carousel(1.35f, 1.0f, c); break;
            case 5: Wheel(-1.65f, -.8f, c); Carousel(.2f, -1.7f, c); Tower(1.95f, -.9f, c); for (int i = -1; i <= 1; i++) Tree(i * 1.5f, 1.2f); break;
            case 6: Carousel(-1.65f, -1.4f, c); Carousel(0, -1.4f, Wood); Carousel(1.65f, -1.4f, c); Coaster(0, .8f, 4.5f, 1.3f, c); break;
            case 7: Coaster(-1.25f, -.45f, 2.2f, 3.9f, c, true); Wheel(1.35f, -1.6f, c); Tree(1.1f, .7f); Tree(2.1f, 1.1f); break;
            case 8: Tower(-1.9f, -1.6f, c); Wheel(0, -1.6f, c); Tower(1.9f, -1.6f, c); Coaster(0, .7f, 4.8f, 1.4f, c); break;
            case 9: Dome(-1.25f, -1.2f, 2.4f, 2.6f, .6f, c); Coaster(1.6f, -.65f, 1.3f, 3.5f, c); Carousel(-1.2f, 1.2f, c); break;
            case 10: Coaster(0, -1.6f, 4.8f, 1.5f, c); Coaster(0, .65f, 4.8f, 1.5f, Wood, true); break;
            case 11: Tower(0, -1.6f, c); Carousel(-1.8f, -.9f, c); Carousel(1.8f, -.9f, c); Wheel(0, .8f, c); break;
            case 12: Gable(-1.5f, -1.6f, 1.8f, 1.6f, .6f, c); Coaster(1.35f, -.4f, 2.0f, 3.8f, c, true); Tower(-1.45f, .75f, c); break;
            case 13: Coaster(0, -1.2f, 4.8f, 2.5f, c); Hall(-1.6f, 1.1f, 1.3f, .7f, .4f, c); Solar(-2.1f, .56f, 1.1f, 4); Carousel(.8f, 1.1f, c); break;
            case 14: Wheel(-1.75f, -1.5f, c); Coaster(1.0f, -1.25f, 2.6f, 1.7f, c, true); Tower(-1.7f, 1.0f, c); Carousel(.2f, 1.0f, c); Carousel(1.9f, 1.0f, c); break;
            case 15: Coaster(0, -1.5f, 4.8f, 1.9f, c, true); Wheel(-1.8f, .7f, c); Carousel(0, 1.0f, c); Tower(1.9f, 1.0f, c); break;
        }
        Hall(-1.65f, 2.35f, .65f, .45f, .35f, c); Hall(1.65f, 2.35f, .65f, .45f, .35f, c);
        Arcade(0, 2.5f, 1.25f, c);
        Rlgl.PopMatrix();
    }
}
