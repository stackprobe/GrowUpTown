using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class RailwayRenderer
{
    private static readonly Color Steel = new(173, 185, 191, 255), Wood = new(107, 83, 66, 255);
    private static readonly Color Stone = new(194, 191, 171, 255), Glass = new(99, 171, 192, 255);
    // A T junction has one through route and two curved turnouts. A four-way
    // junction is a diamond crossing: both straight routes continue across it.
    private static readonly int[][] Curves = [[], [], [], [3], [], [], [6], [3, 6],
        [], [9], [], [3, 9], [12], [9, 12], [6, 12], []];
    internal static int[] CurveRoutes(int mask) => Curves[mask];
    internal static int[] BufferStopArms(int mask) => mask switch
    {
        0 => [1, 3], 1 => [2], 2 => [3], 4 => [0], 8 => [1], _ => []
    };
    internal static int StraightArms(int mask) => mask switch
    {
        0 => 10,
        3 or 6 or 9 or 12 => 0,
        7 or 13 => 5,
        11 or 14 => 10,
        _ => mask
    };
    private static void Box(float x, float y, float z, float w, float h, float d, Color c)
        => DrawCube(new(x, y + h / 2, z), w, h, d, c);
    internal static void DrawTrack(City city, int wx, int wz, float x, float z, TrainTraffic? traffic = null, double time = 0)
    {
        int mask = city.RailMask(wx, wz), bridge = city.BridgeAxis(wx, wz);
        bool crossing = city[wx, wz].RailCrossing;
        if (!crossing) Box(x, .02f, z, 1, .055f, 1, new(116, 108, 94, 255));
        if (bridge != 0)
        {
            Box(x, -.28f, z, .24f, .30f, .24f, Stone);
            for (int side = -1; side <= 1; side += 2)
                Box(x + (bridge == 2 ? side * .46f : 0), .12f, z + (bridge == 1 ? side * .46f : 0),
                    bridge == 1 ? 1 : .04f, .10f, bridge == 1 ? .04f : 1, Steel);
        }
        foreach (int route in CurveRoutes(mask))
        {
            float sx = (route & 2) != 0 ? 1 : -1, sz = (route & 1) != 0 ? 1 : -1;
            Vector3 Point(float radius, float angle, float height) => new(
                x + sx * (.5f - radius * MathF.Cos(angle)), height,
                z + sz * (.5f - radius * MathF.Sin(angle)));
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathF.PI / 24, b = (i + 1) * MathF.PI / 24;
                for (int side = -1; side <= 1; side += 2)
                    DrawCylinderEx(Point(.5f + side * .145f, a, .133f), Point(.5f + side * .145f, b, .133f), .018f, .018f, 4, Steel);
                if (!crossing && i % 2 == 0) DrawCylinderEx(Point(.28f, a, .101f), Point(.72f, a, .101f), .028f, .028f, 4, Wood);
            }
        }
        int straight = StraightArms(mask);
        for (int dir = 0; dir < 4; dir++)
        {
            if ((straight & (1 << dir)) == 0) continue;
            bool horizontal = dir % 2 == 1;
            float sign = dir < 2 ? 1 : -1;
            for (int i = 0; !crossing && i < 4; i++)
            {
                float along = sign * (i * .13f + .025f);
                Box(x + (horizontal ? along : 0), .081f, z + (horizontal ? 0 : along), horizontal ? .055f : .44f,
                    .035f, horizontal ? .44f : .055f, Wood);
            }
            for (int side = -1; side <= 1; side += 2)
                Box(x + (horizontal ? sign * .25f : side * .145f), .115f,
                    z + (horizontal ? side * .145f : sign * .25f), horizontal ? .5f : .035f, .035f, horizontal ? .035f : .5f, Steel);
        }
        foreach (int end in BufferStopArms(mask))
        {
            bool horizontal = end % 2 == 1;
            float sign = end < 2 ? 1 : -1, along = sign * (mask == 0 ? .40f : .12f);
            // A braced buffer beyond the end of the usable track. The lead cab
            // stops just short of the red and white bumper.
            for (int side = -1; side <= 1; side += 2)
            {
                float bx = x + (horizontal ? along : side * .145f);
                float bz = z + (horizontal ? side * .145f : along);
                Box(bx, .12f, bz, .045f, .28f, .045f, Steel);
                DrawCylinderEx(new(bx, .36f, bz),
                    new(bx + (horizontal ? sign * .18f : 0), .13f, bz + (horizontal ? 0 : sign * .18f)),
                    .022f, .022f, 4, Steel);
            }
            for (int stripe = 0; stripe < 5; stripe++)
                Box(x + (horizontal ? along : (stripe - 2) * .08f), .30f,
                    z + (horizontal ? (stripe - 2) * .08f : along), horizontal ? .065f : .08f,
                    .085f, horizontal ? .08f : .065f, stripe % 2 == 0 ? new(198, 60, 49, 255) : new(233, 225, 207, 255));
        }
        if (!crossing) return;
        bool eastWest = (mask & 10) != 0;
        for (int side = -1; side <= 1; side += 2)
        {
            float px = x + side * .37f, pz = z + side * .37f;
            Box(px, .08f, pz, .045f, .43f, .045f, Wood);
            for (int lamp = 0; lamp < 2; lamp++)
                Box(px + (eastWest ? (lamp - .5f) * .09f : 0), .48f,
                    pz + (eastWest ? 0 : (lamp - .5f) * .09f), .07f, .075f, .07f,
                    traffic?.LampOn(wx, wz, lamp, time) == true ? new(255, 48, 37, 255) : new(76, 37, 37, 255));
            float closure = traffic?.Closure(wx, wz) ?? 0;
            float angle = (1 - closure) * MathF.PI / 2;
            for (int i = 0; i < 6; i++)
            {
                Vector3 Arm(float d) => new(px + (eastWest ? -side * d * MathF.Cos(angle) : 0),
                    .28f + d * MathF.Sin(angle), pz + (eastWest ? 0 : -side * d * MathF.Cos(angle)));
                DrawCylinderEx(Arm(i * .11f), Arm((i + 1) * .11f), .022f, .022f, 4,
                    i % 2 == 0 ? new(239, 193, 66, 255) : new(47, 51, 54, 255));
            }
        }
    }

    internal static void DrawStation(float x, float z, Tile tile)
    {
        int style = tile.StationStyle, family = style / 4, variant = style % 4;
        Color wall = family switch { 0 => new(191, 157, 113, 255), 1 => new(226, 228, 212, 255),
            2 => new(141, 164, 181, 255), _ => new(176, 113, 91, 255) };
        Color roof = family switch { 0 => new(81, 126, 95, 255), 1 => new(70, 136, 173, 255),
            2 => new(72, 88, 108, 255), _ => new(91, 88, 89, 255) };
        void B(float bx, float by, float bz, float w, float h, float d, Color c)
        {
            for (int r = 0; r < tile.StationRotation; r++) { (bx, bz) = (bz, -bx); (w, d) = (d, w); }
            Box(x + .5f + bx, by, z + .5f + bz, w, h, d, c);
        }
        B(0, .02f, 0, 1.94f, .10f, 1.94f, Stone);
        // Forecourt, station building and covered platform, entirely within 2x2.
        B(0, .12f, .58f, 1.8f, .08f, .55f, new(215, 208, 181, 255));
        for (int i = -3; i <= 3; i++) B(i * .24f, .20f, .76f, .17f, .018f, .045f, new(238, 196, 86, 255));
        float height = family == 2 ? .82f : .52f;
        B(variant == 2 ? -.22f : 0, .12f, -.28f, variant == 2 ? 1.15f : 1.62f, height, .87f, wall);
        for (int i = -2; i <= 2; i++) B(i * .27f, .26f, .165f, .18f, .27f, .018f, Glass);
        B(0, .12f, .18f, .22f, .35f, .035f, roof);
        B(0, .52f, .22f, .53f, .12f, .035f, tile.RailActive ? new(133, 204, 117, 255) : Stone);
        for (int side = -1; side <= 1; side += 2) B(side * .72f, .20f, .56f, .045f, .42f, .045f, Steel);
        B(0, .62f, .49f, 1.82f, .06f, .65f, roof);
        switch (variant)
        {
            case 0: // Stepped gable roof / countryside, seaside, city and brick variants.
                for (int step = 0; step < 5; step++) B(0, height + .12f + step * .045f, -.28f, 1.78f, .045f, 1.02f - step * .17f, roof);
                break;
            case 1: // Clock tower and flat roof.
                B(0, height + .12f, -.28f, 1.78f, .08f, 1.02f, roof);
                B(-.55f, height + .20f, -.27f, .30f, .40f, .32f, wall);
                B(-.55f, height + .32f, -.10f, .19f, .19f, .015f, new(241, 235, 205, 255));
                B(-.55f, height + .34f, -.085f, .018f, .12f, .015f, roof);
                B(-.55f, height + .60f, -.27f, .39f, .06f, .40f, roof);
                break;
            case 2: // Split pavilion with glazed atrium.
                B(-.22f, height + .12f, -.28f, 1.28f, .09f, 1.02f, roof);
                B(.58f, .12f, -.28f, .48f, height + .16f, .78f, Glass);
                B(.58f, height + .28f, -.28f, .57f, .06f, .88f, Steel);
                break;
            case 3: // Sawtooth roof and structural ribs.
                for (int i = -2; i <= 2; i++)
                {
                    B(i * .32f, height + .12f, -.28f, .28f, .10f + (i + 2) % 2 * .10f, .98f, roof);
                    B(i * .32f, .12f, .17f, .035f, height, .04f, Steel);
                }
                break;
        }
        if (family == 0) for (int side = -1; side <= 1; side += 2) B(side * .79f, .13f, -.82f, .18f, .18f, .18f, new(105, 155, 89, 255));
        if (family == 1) B(0, .20f, -.76f, 1.5f, .06f, .14f, Glass);
        if (family == 2) for (int i = -2; i <= 2; i++) B(i * .27f, .66f, .16f, .18f, .18f, .02f, Glass);
        if (family == 3) for (int i = 0; i < 5; i++) B(0, .18f + i * .075f, -.724f, 1.55f, .018f, .015f, Stone);
        // An independent street entrance allows roads on either end of the station,
        // while the platform always stays on the selected two-cell railway edge.
        int entrance = ((tile.StationEntranceSide ?? (tile.StationRotation + 2) % 4) - tile.StationRotation + 4) % 4;
        bool end = entrance is 1 or 3;
        float doorX = end ? (entrance == 1 ? .825f : -.825f) : 0;
        float doorZ = end ? -.28f : entrance == 0 ? .18f : -.725f;
        float pathX = end ? (entrance == 1 ? .91f : -.91f) : 0;
        float pathZ = end ? -.28f : entrance == 0 ? .58f : -.85f;
        B(pathX, .12f, pathZ, end ? .16f : .38f, .025f, end ? .38f : entrance == 0 ? .78f : .25f, Stone);
        B(doorX, .145f, doorZ, end ? .035f : .28f, .36f, end ? .28f : .035f, roof);
        B(doorX, .21f, doorZ + (end ? 0 : entrance == 0 ? .022f : -.022f),
            end ? .046f : .20f, .24f, end ? .20f : .025f, Glass);
        B(end ? pathX : doorX, .51f, end ? doorZ : pathZ,
            end ? .16f : .42f, .055f, end ? .42f : .24f, roof);
        B(end ? doorX : -.105f, .17f, end ? doorZ - .105f : doorZ,
            .04f, .31f, .04f, Stone);
        B(end ? doorX : .105f, .17f, end ? doorZ + .105f : doorZ,
            .04f, .31f, .04f, Stone);
    }
}
