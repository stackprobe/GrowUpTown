using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// World-size details shared by special facilities. Doors and street furniture
// keep the same dimensions regardless of the size of the main building.
internal static class LandmarkDrawing
{
    internal static readonly Color White = new(229, 236, 226, 255), Glass = new(73, 158, 187, 255);
    internal static readonly Color Steel = new(107, 136, 153, 255), Green = new(101, 157, 116, 255);
    internal static readonly Color Path = new(214, 218, 197, 255), Wood = new(162, 118, 79, 255);
    internal static void Box(float x, float y, float z, float w, float h, float d, Color c) =>
        DrawCube(new(x, y + h / 2, z), w, h, d, c);
    internal static void Beam(Vector3 from, Vector3 to, float r, Color c) => DrawCylinderEx(from, to, r, r, 6, c);
    internal static void Disk(float x, float y, float z, float radius, float h, Color c) =>
        DrawCylinder(new(x, y, z), radius, radius, h, 24, c);
    internal static Color Accent(int style) => (style / 4) switch
    {
        0 => new(55, 157, 183, 255), 1 => new(96, 157, 112, 255),
        2 => new(178, 111, 109, 255), _ => new(112, 126, 176, 255)
    };
    internal static void Begin(float x, float z, Tile tile)
    {
        float size = City.Footprint(tile.Kind), extra = (size - 5) / 2;
        Rlgl.PushMatrix(); Rlgl.Translatef(x + (size - 1) / 2, 0, z + (size - 1) / 2); Rlgl.Rotatef(tile.LandmarkRotation * 90, 0, 1, 0);
        SpecialBuildingDetails.Style = tile.LandmarkStyle;
        Box(0, .02f, 0, size - .02f, .06f, size - .02f, Green);
        Box(0, .081f, 0, size - .36f, .02f, size - .36f, Path);
        SpecialBuildingDetails.Paving(0, .102f, 0, size - .36f, size - .36f);
        // Broad circulation reads at the normal zoom; buildings and exhibits
        // cover these shallow inlays wherever they occupy the plaza.
        Box(0, .1025f, 0, .36f, .001f, size - .45f, White);
        for (int side = -1; side <= 1; side += 2)
            Box(side * (size / 2 - .62f), .1025f, 0, .22f, .001f, size - .65f, White);
        Box(0, .1025f, size / 2 - .55f, size - .9f, .001f, .32f, White);
        // Broad circulation reads at the normal zoom; buildings and exhibits
        // cover these shallow inlays wherever they occupy the plaza.
        Box(0, .1025f, 0, .36f, .001f, size - .45f, White);
        for (int side = -1; side <= 1; side += 2)
            Box(side * (size / 2 - .62f), .1025f, 0, .22f, .001f, size - .65f, White);
        Box(0, .1025f, size / 2 - .55f, size - .9f, .001f, .32f, White);
        // A planted edge frames the campus without invading its exhibit areas.
        float edge = size / 2 - .24f;
        Box(0, .102f, -edge, size - .6f, .015f, .16f, White);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(side * edge, .102f, 0, .16f, .015f, size - .6f, White);
            for (int i = -1; i <= 1; i++)
            {
                float p = i * (size / 2 - .9f);
                Box(side * edge, .117f, p, .14f, .055f, .62f, Wood);
                Box(side * edge, .172f, p, .12f, .09f, .58f, Green);
                Box(side * edge, .264f, p + .18f, .09f, .018f, .10f, Accent(tile.LandmarkStyle));
            }
            Box(side * (edge - .65f), .102f, edge - .10f, .78f, .018f, .32f, White);
            Box(side * (edge - .65f), .12f, edge - .10f, .70f, .07f, .24f, Green);
        }
        Box(0, .103f, 1.9f + extra, .32f, .008f, 1.15f, White);
        foreach (int side in new[] { -1, 1 })
        {
            Tree(side * (2.12f + extra), -2.10f - extra); Tree(side * (2.12f + extra), .72f);
            Bench(side * .85f, 2.05f + extra); Bench(side * 1.35f, 2.05f + extra);
            Box(side * (2.1f + extra), .10f, 1.75f + extra, .025f, .43f, .025f, Steel);
            DrawSphereEx(new(side * (2.1f + extra), .55f, 1.75f + extra), .043f, 4, 6, White);
        }
        Box(.38f, .10f, 2.15f + extra, .025f, .31f, .025f, Steel);
        Box(.38f, .32f, 2.15f + extra, .24f, .13f, .025f, Accent(tile.LandmarkStyle));
    }
    internal static void Tree(float x, float z)
    {
        Box(x, .10f, z, .04f, .33f, .04f, Wood);
        DrawSphereEx(new(x, .52f, z), .19f, 4, 7, Green);
        DrawSphereEx(new(x + .025f, .65f, z), .13f, 4, 7, new(144, 180, 116, 255));
    }
    internal static void Bench(float x, float z)
    {
        foreach (int side in new[] { -1, 1 }) Box(x + side * .075f, .10f, z, .025f, .04f, .08f, Steel);
        Box(x, .14f, z, .24f, .045f, .10f, Wood);
        Box(x, .185f, z + .045f, .24f, .10f, .025f, Wood);
    }
    internal static void Hall(float x, float z, float w, float d, float h, Color c, bool roofDetails = true)
    {
        Box(x, .10f, z, w, h, d, White);
        Box(x, .10f + h, z, w + .07f, .055f, d + .07f, c);
        SpecialBuildingDetails.Walls(x, z, w, d, h);
        if (roofDetails)
        {
            SpecialBuildingDetails.Roof(x, h + .156f, z, w - .08f, d - .08f, c);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(x, h + .155f, z + side * (d / 2 - .02f), w, .045f, .035f, White);
                Box(x + side * (w / 2 - .02f), h + .155f, z, .035f, .045f, d, White);
            }
            if (w > 1.4f && d > 1 && h > .5f)
            {
                // Fixed-size roof plant; never enlarged to fit a larger facility.
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(x + side * w * .25f, h + .158f, z - d * .2f, .28f, .07f, .36f, Steel);
                    Box(x + side * w * .25f, h + .228f, z - d * .2f, .23f, .012f, .30f, Glass);
                }
            }
        }
        Box(x, .10f, z, w + .025f, .055f, d + .025f, Steel);
        for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
                Box(x + side * (w / 2 - .025f), .155f, z + end * (d / 2 - .025f), .055f, h - .055f, .055f, White);
        Box(x, .10f, z + d / 2 + .02f, .22f, .28f, .025f, Steel);
        Box(x, .12f, z + d / 2 + .04f, .18f, .24f, .02f, Glass);
        Box(x, .12f, z + d / 2 + .055f, .012f, .24f, .015f, White);
        Box(x, .41f, z + d / 2 + .08f, .40f, .035f, .21f, c);
        Box(x, .105f, z + d / 2 + .09f, .32f, .015f, .18f, Path);
    }
    internal static void Pool(float x, float z, float r)
    {
        Disk(x, .10f, z, r, .035f, White); Disk(x, .136f, z, r - .04f, .006f, Glass);
    }
    internal static void Solar(float x, float y, float z, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Box(x + i * .32f, y, z, .27f, .025f, .40f, Steel);
            Box(x + i * .32f, y + .025f, z, .23f, .009f, .36f, new(48, 81, 121, 255));
        }
    }
}
