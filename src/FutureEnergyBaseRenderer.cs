using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class FutureEnergyBaseRenderer
{
    private static void Core(float x, float z, float r, Color c)
    {
        Disk(x, .1f, z, r + .15f, .08f, Steel); Disk(x, .18f, z, r, .55f, White);
        Disk(x, .73f, z, r + .04f, .08f, c);
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16, b = (i + 1) * MathF.Tau / 16;
            Beam(new(x + MathF.Cos(a) * r * .8f, 1.04f, z + MathF.Sin(a) * r * .8f), new(x + MathF.Cos(b) * r * .8f, 1.04f, z + MathF.Sin(b) * r * .8f), .045f, c);
            if (i % 4 == 0) Beam(new(x + MathF.Cos(a) * r * .8f, .8f, z + MathF.Sin(a) * r * .8f), new(x + MathF.Cos(a) * r * .8f, 1.04f, z + MathF.Sin(a) * r * .8f), .03f, Steel);
        }
        DrawSphereEx(new(x, .85f, z), r * .45f, 8, 12, Glass);
        Box(x, .1f, z + r + .02f, .2f, .28f, .025f, Glass);
    }
    private static void Turbine(float x, float z, Color c, float phase = 0)
    {
        Disk(x, .1f, z, .18f, .06f, Steel); Beam(new(x, .16f, z), new(x, 1.95f, z), .035f, White);
        for (int i = 0; i < 3; i++)
        {
            float a = phase + i * MathF.Tau / 3;
            Beam(new(x, 1.95f, z + .08f), new(x + MathF.Cos(a) * .65f, 1.95f + MathF.Sin(a) * .65f, z + .08f), .024f, c);
        }
        DrawSphereEx(new(x, 1.95f, z + .08f), .07f, 4, 6, Steel);
    }
    private static void Batteries(float x, float z, int count, Color c)
    {
        for (int i = 0; i < count; i++)
        {
            float px = x + i % 3 * .48f, pz = z + i / 3 * .65f;
            Box(px, .1f, pz, .35f, .35f, .5f, White);
            Box(px, .45f, pz, .36f, .03f, .51f, c);
            Box(px, .2f, pz + .26f, .22f, .13f, .015f, Steel);
        }
    }
    private static void Array(float x, float z, int rows, int cols)
    {
        for (int i = 0; i < rows; i++) Solar(x, .2f, z + i * .62f, cols);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Core(-1.4f, -1.3f, 1.15f, c); Pool(1.8f, -1.5f, .85f); Array(.8f, .25f, 3, 7); Batteries(-2.4f, .7f, 6, c); break;
            case 1: Core(-1.7f, -1.5f, .95f, c); Core(1.7f, -1.5f, .95f, c); Pool(0, 1.0f, .7f); Batteries(-2.5f, .7f, 6, c); Batteries(1.7f, .7f, 6, c); break;
            case 2: Hall(-1.6f, -1.5f, 2.6f, 2.3f, .75f, c); Core(1.7f, -.9f, 1.1f, c); Pool(-1.5f, 1.15f, .85f); Turbine(2.8f, 1.6f, c); break;
            case 3: Core(0, -1.4f, 1.35f, c); Pool(-2.4f, .75f, .6f); Pool(2.4f, .75f, .6f); Array(-1.2f, 1.0f, 2, 8); break;
            case 4: Core(0, -1.5f, 1.0f, c); for (int i = -1; i <= 1; i++) Turbine(i * 2.5f, 1.15f, c, i * .3f); Tree(-2.7f, -1.7f); Tree(2.7f, -1.7f); break;
            case 5: Hall(-1.5f, -1.6f, 2.7f, 2.2f, .65f, c); Box(-1.5f, .81f, -1.6f, 2.4f, .04f, 1.9f, Green); Core(1.7f, -1.4f, 1.0f, c); Array(-2.6f, .65f, 3, 16); break;
            case 6: for (int i = -1; i <= 1; i++) Core(i * 2.2f, -1.4f, .8f, c); Batteries(-2.5f, .65f, 6, c); Array(.4f, .65f, 3, 8); break;
            case 7: Core(-1.65f, -.7f, 1.15f, c); Array(.3f, -2.3f, 6, 9); Turbine(-2.6f, 1.7f, c); break;
            case 8: Hall(-1.8f, -1.35f, 2.0f, 3.0f, 1.4f, c); Core(1.4f, -1.4f, 1.15f, c); Batteries(.5f, .75f, 9, c); break;
            case 9: Core(-1.6f, -1.4f, 1.0f, c); Core(1.6f, -1.4f, 1.0f, c); Hall(0, 1.2f, 5.7f, 1.0f, .65f, c); Atrium(0, 1.2f, 1.4f, .9f, 1.0f); break;
            case 10: Dome(0, -1.4f, 3.0f, 2.7f, .85f, c); Hall(-2.5f, -.4f, .9f, 3.1f, .6f, c); Hall(2.5f, -.4f, .9f, 3.1f, .9f, c); Batteries(-.5f, 1.2f, 3, c); break;
            case 11: Core(0, -1.3f, 1.5f, c); Batteries(-2.9f, -.8f, 9, c); Batteries(2.0f, -.8f, 9, c); Hall(0, 1.75f, 2.8f, .6f, .5f, c); break;
            case 12: Core(-1.55f, -1.3f, 1.1f, c); Hall(1.65f, -1.5f, 2.3f, 2.3f, 1.0f, c); Batteries(-2.5f, .7f, 9, c); Batteries(1.1f, .7f, 9, c); break;
            case 13: Core(0, -1.4f, 1.2f, c); Array(-3.0f, -.8f, 4, 5); Array(1.7f, -.8f, 4, 5); Turbine(0, 1.9f, c); break;
            case 14: Core(-1.6f, -1.5f, .9f, c); Core(1.6f, -1.5f, .9f, c); Turbine(-2.6f, 1.0f, c); Turbine(2.6f, 1.0f, c); Batteries(-.5f, .65f, 6, c); break;
            case 15: Core(-1.5f, -1.4f, 1.15f, c); Hall(1.8f, -1.5f, 2.0f, 2.3f, .8f, c); Solar(1.0f, .96f, -1.5f, 5); Array(-2.6f, .9f, 2, 9); Batteries(1.4f, .7f, 6, c); break;
        }
        Hall(-2.8f, 3.15f, .65f, .6f, .38f, c); Parking(3.2f, c);
        // Turbine angle and all equipment are static, with no time-dependent geometry.
        Rlgl.PopMatrix();
    }
}
