using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal readonly record struct ParkLayout(int Connections, int Style, int Rotation);

internal static class ParkRenderer
{
    internal const int StyleCount = 6;
    private static readonly Color Grass = new(107, 166, 115, 255), Path = new(223, 210, 170, 255);
    private static readonly Color Wood = new(159, 117, 73, 255), Stone = new(185, 195, 180, 255);

    // Derived appearance needs no new save fields. Only the four park neighbors can
    // change an existing layout; every single addition/removal changes its style.
    internal static ParkLayout Layout(City city, int x, int z)
    {
        int mask = (city[x, z + 1].Kind == TileKind.Park ? 1 : 0)
            | (city[x + 1, z].Kind == TileKind.Park ? 2 : 0)
            | (city[x, z - 1].Kind == TileKind.Park ? 4 : 0)
            | (city[x - 1, z].Kind == TileKind.Park ? 8 : 0);
        uint seed = unchecked((uint)x * 73856093u ^ (uint)z * 19349663u ^ (uint)city.AppearanceSeed);
        seed ^= seed >> 16;
        return new(mask, (int)(seed % StyleCount + (uint)BitOperations.PopCount((uint)mask)) % StyleCount,
            (int)((seed >> 8) % 4));
    }

    private static void Box(float x, float y, float z, float w, float h, float d, Color color) =>
        DrawCube(new(x, y + h / 2, z), w, h, d, color);

    internal static void Draw(City city, int worldX, int worldZ, int x, int z)
    {
        var layout = Layout(city, worldX, worldZ);
        // Full-width turf and edge-to-edge paths have no seams between park tiles.
        Box(x, .02f, z, 1, .045f, 1, Grass);
        Box(x, .066f, z, .23f, .012f, .23f, Path);
        int paths = layout.Connections == 0 ? 5 : layout.Connections;
        float length = layout.Connections == 0 ? .38f : .5f;
        for (int arm = 0; arm < 4; arm++)
        {
            bool alongX = arm % 2 == 1;
            int sign = arm < 2 ? 1 : -1;
            if ((paths & (1 << arm)) != 0)
                Box(x + (alongX ? sign * length / 2 : 0), .066f, z + (alongX ? 0 : sign * length / 2),
                    alongX ? length : .18f, .012f, alongX ? .18f : length, Path);
            if ((layout.Connections & (1 << arm)) != 0) continue;
            // Low edging only on the outside, leaving a central entrance gap.
            for (int side = -1; side <= 1; side += 2)
                Box(x + (alongX ? sign * .48f : side * .30f), .065f,
                    z + (alongX ? side * .30f : sign * .48f), alongX ? .035f : .32f,
                    .035f, alongX ? .32f : .035f, Stone);
        }
        // Decorations stay in the four corners, clear of every possible path arm.
        Rlgl.PushMatrix();
        Rlgl.Translatef(x, .065f, z);
        Rlgl.Rotatef(layout.Rotation * 90, 0, 1, 0);
        switch (layout.Style)
        {
            case 0: // Grove
                Tree(-.29f, -.29f); Tree(.29f, .29f); Tree(-.29f, .29f, .75f); Bench(.29f, -.29f);
                break;
            case 1: // Flower garden
                Flowers(-.29f, -.29f, false); Flowers(.29f, .29f, true);
                Flowers(-.29f, .29f, true); Bench(.29f, -.29f);
                break;
            case 2: // Fountain court
                DrawCylinder(new(-.29f, .01f, -.29f), .16f, .16f, .055f, 8, Stone);
                DrawCylinder(new(-.29f, .068f, -.29f), .125f, .125f, .008f, 8, new(87, 176, 199, 255));
                DrawCylinder(new(-.29f, .075f, -.29f), .025f, .04f, .16f, 6, Stone);
                DrawSphereEx(new(-.29f, .25f, -.29f), .045f, 4, 6, new(172, 222, 223, 255));
                Bench(.29f, -.29f); Bench(-.29f, .29f); Flowers(.29f, .29f, false);
                break;
            case 3: // Playground: slide and sandpit
                Box(-.29f, .01f, -.29f, .30f, .035f, .30f, Wood);
                Box(-.29f, .046f, -.29f, .25f, .008f, .25f, Path);
                Box(.29f, .01f, .29f, .035f, .25f, .16f, Wood);
                Box(.29f, .26f, .29f, .12f, .035f, .17f, new(230, 164, 83, 255));
                Rlgl.PushMatrix(); Rlgl.Translatef(.22f, .145f, .29f); Rlgl.Rotatef(55, 0, 0, 1);
                DrawCube(Vector3.Zero, .28f, .025f, .13f, new(104, 189, 218, 255)); Rlgl.PopMatrix();
                Tree(-.29f, .29f, .7f); Bench(.29f, -.29f);
                break;
            case 4: // Picnic lawn
                Picnic(-.29f, -.29f); Picnic(.29f, .29f);
                Tree(-.29f, .29f); Flowers(.29f, -.29f, false);
                break;
            case 5: // Formal topiary garden
                Hedge(-.29f, -.29f); Hedge(.29f, .29f); Hedge(-.29f, .29f);
                Box(.29f, .01f, -.29f, .18f, .06f, .18f, Stone);
                Box(.29f, .07f, -.29f, .075f, .19f, .075f, Stone);
                DrawSphereEx(new(.29f, .29f, -.29f), .085f, 4, 6, new(218, 222, 201, 255));
                break;
        }
        Rlgl.PopMatrix();
    }

    private static void Tree(float x, float z, float size = 1)
    {
        Box(x, 0, z, .05f, .32f * size, .05f, Wood);
        DrawSphereEx(new(x, .39f * size, z), .17f * size, 4, 6, new(73, 135, 99, 255));
        DrawSphereEx(new(x, .54f * size, z), .13f * size, 4, 6, new(125, 178, 112, 255));
    }
    private static void Bench(float x, float z)
    {
        Box(x, .04f, z, .24f, .045f, .10f, Wood);
        Box(x, .085f, z + .045f, .24f, .10f, .025f, Wood);
        Box(x - .075f, 0, z, .025f, .04f, .08f, Stone);
        Box(x + .075f, 0, z, .025f, .04f, .08f, Stone);
    }
    private static void Flowers(float x, float z, bool gold)
    {
        Box(x, .005f, z, .29f, .025f, .27f, Wood);
        Box(x, .03f, z, .25f, .025f, .23f, new(71, 133, 92, 255));
        Color color = gold ? new(240, 204, 101, 255) : new(223, 135, 162, 255);
        for (int i = 0; i < 3; i++)
            Box(x - .08f + i * .08f, .055f, z, .045f, .035f, .17f, color);
    }
    private static void Picnic(float x, float z)
    {
        Box(x, 0, z, .05f, .13f, .10f, Wood);
        Box(x, .13f, z, .25f, .035f, .14f, Wood);
        Box(x, .065f, z - .12f, .25f, .035f, .05f, Wood);
        Box(x, .065f, z + .12f, .25f, .035f, .05f, Wood);
    }
    private static void Hedge(float x, float z)
    {
        Box(x, 0, z, .27f, .035f, .27f, Stone);
        Box(x, .035f, z, .22f, .13f, .22f, new(65, 132, 100, 255));
        Box(x, .165f, z, .15f, .07f, .15f, new(113, 172, 113, 255));
    }
}
