using Raylib_cs;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class AutoFactoryRenderer
{
    private static readonly Color Asphalt = new(73, 84, 91, 255), Rubber = new(37, 43, 48, 255);
    private static readonly Color Robot = new(231, 167, 61, 255);

    private static void Car(float x, float z, Color c, float y = .12f, bool unfinished = false)
    {
        // Use ordinary street-car dimensions even inside this eight-cell facility.
        foreach (int side in new[] { -1, 1 })
            foreach (float axle in new[] { -.09f, .09f })
                Box(x + side * .075f, y, z + axle, .035f, .075f, .055f, Rubber);
        Box(x, y + .035f, z, .16f, .075f, .29f, c);
        if (unfinished)
        {
            Box(x, y + .11f, z, .12f, .012f, .15f, Steel);
            return;
        }
        Box(x, y + .11f, z + .015f, .13f, .055f, .14f, Glass);
        Box(x, y + .165f, z + .025f, .135f, .014f, .085f, c);
        foreach (int side in new[] { -1, 1 })
            Box(x + side * .05f, y + .065f, z - .15f, .035f, .025f, .012f, White);
    }

    private static void Arm(float x, float z, int side)
    {
        Box(x, .11f, z, .17f, .045f, .18f, Steel);
        Beam(new(x, .155f, z), new(x, .39f, z), .035f, Robot);
        Beam(new(x, .39f, z), new(x + side * .15f, .50f, z), .03f, Robot);
        Beam(new(x + side * .15f, .50f, z), new(x + side * .28f, .34f, z), .022f, Steel);
        Box(x + side * .28f, .28f, z, .035f, .075f, .045f, Rubber);
    }

    private static void Assembly(float x, float z, float d, Color c)
    {
        Box(x, .105f, z, 1.25f, .014f, d, Asphalt);
        Box(x, .12f, z, .46f, .06f, d - .12f, Steel);
        foreach (int side in new[] { -1, 1 })
            Box(x + side * .32f, .123f, z, .035f, .007f, d - .12f, Robot);
        int count = (int)(d / .60f);
        for (int i = 0; i < count; i++)
        {
            float pz = z - d / 2 + .35f + i * .60f;
            Box(x, .184f, pz, .46f, .018f, .03f, White);
            Car(x, pz, i % 2 == 0 ? c : White, .20f, i < count / 2);
            if (i % 2 == 0) { Arm(x - .49f, pz, 1); Arm(x + .49f, pz, -1); }
        }
    }

    private static void Workshop(float x, float z, float w, float d, Color c, bool solar = false)
    {
        const float h = .78f;
        Hall(x, z, w, d, h, c);
        // Repeated shallow roof ridges and skylights identify the production halls.
        int bays = Math.Max(1, (int)(w / .65f));
        float bay = w / bays;
        for (int i = 0; i < bays; i++)
        {
            float px = x - w / 2 + bay * (i + .5f);
            Box(px, h + .16f, z, bay - .05f, .09f, d - .10f, White);
            Box(px, h + .25f, z, .10f, .025f, d - .18f, Glass);
        }
        for (float px = -w / 2 + .30f; px < w / 2 - .10f; px += .60f)
        {
            Box(x + px, .11f, z + d / 2 + .025f, .38f, .35f, .03f, Asphalt);
            Box(x + px, .46f, z + d / 2 + .045f, .42f, .035f, .08f, c);
        }
        Box(x + w / 2 - .16f, .94f, z - d / 2 + .20f, .18f, .10f, .24f, Steel);
        if (solar) Solar(x - w / 2 + .22f, 1.075f, z, Math.Max(1, (int)((w - .20f) / .32f)));
    }

    private static void FinishedCars(float x, float z, int rows, Color c)
    {
        Box(x, .105f, z, 1.30f, .01f, rows * .48f + .12f, Asphalt);
        for (int row = 0; row < rows; row++) for (int col = 0; col < 4; col++)
        {
            float px = x - .48f + col * .32f, pz = z + (row - (rows - 1) / 2f) * .48f;
            Box(px - .13f, .119f, pz, .012f, .005f, .39f, White);
            Car(px, pz, (row + col) % 3 == 0 ? c : (col % 2 == 0 ? White : Robot));
        }
    }

    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        switch (tile.LandmarkStyle)
        {
            case 0: Workshop(-1.40f, -.60f, 2.35f, 4.30f, c); Assembly(.50f, -.60f, 4.30f, c); FinishedCars(2.20f, -.60f, 7, c); break;
            case 1: Workshop(-1.70f, -.60f, 1.60f, 4.30f, c); Workshop(1.70f, -.60f, 1.60f, 4.30f, c); Assembly(0, -.60f, 4.30f, c); break;
            case 2: Workshop(0, -1.80f, 5.20f, 1.70f, c); Assembly(-1.50f, .50f, 2.50f, c); Assembly(0, .50f, 2.50f, c); FinishedCars(1.90f, .45f, 5, c); break;
            case 3: Workshop(-1.50f, -1.65f, 2.25f, 1.90f, c); Workshop(-1.50f, .65f, 2.25f, 1.70f, c); Assembly(.40f, -.60f, 4.30f, c); FinishedCars(2.10f, -.60f, 7, c); break;
            case 4: Workshop(-1.65f, -.60f, 1.80f, 4.30f, c); Assembly(0, -.60f, 4.30f, c); FinishedCars(1.90f, -.70f, 5, c); Tree(1.60f, 1.45f); Tree(2.30f, 1.45f); break;
            case 5: Workshop(-1.35f, -.60f, 2.50f, 4.30f, c); Box(-1.35f, 1.075f, -.60f, 2.30f, .035f, 4.0f, Green); Assembly(.60f, -.60f, 4.30f, c); FinishedCars(2.30f, -.60f, 7, c); break;
            case 6: Workshop(-1.60f, -1.70f, 1.90f, 1.80f, c); Workshop(1.60f, -1.70f, 1.90f, 1.80f, c); Assembly(0, -.60f, 4.30f, c); FinishedCars(-1.70f, .50f, 4, c); FinishedCars(1.70f, .50f, 4, c); break;
            case 7: Workshop(-1.40f, -.60f, 2.35f, 4.30f, c, true); Assembly(.50f, -.60f, 4.30f, c); FinishedCars(2.20f, -.60f, 7, c); break;
            case 8: Hall(-1.85f, -1.95f, 1.50f, 1.30f, 1.55f, c); Workshop(-1.85f, .35f, 1.50f, 2.60f, c); Assembly(-.10f, -.60f, 4.30f, c); Workshop(1.75f, -.60f, 1.80f, 4.30f, c); break;
            case 9: Workshop(-1.85f, -.60f, 1.45f, 4.30f, c); Assembly(0, -.60f, 4.30f, c); Workshop(1.85f, -.60f, 1.45f, 4.30f, c); Atrium(0, -2.35f, .95f, .65f, .65f); break;
            case 10: Workshop(0, -1.90f, 5.20f, 1.50f, c); Assembly(-1.65f, .30f, 2.65f, c); Assembly(0, .30f, 2.65f, c); Assembly(1.65f, .30f, 2.65f, c); break;
            case 11: Dome(-1.70f, -1.65f, 1.80f, 1.90f, .65f, c); Workshop(-1.70f, .65f, 1.80f, 1.70f, c); Assembly(.20f, -.60f, 4.30f, c); FinishedCars(2.05f, -.60f, 7, c); break;
            case 12: Workshop(0, -1.75f, 5.20f, 1.90f, c); Assembly(-.80f, .55f, 2.45f, c); Assembly(.80f, .55f, 2.45f, c); FinishedCars(-2.30f, .55f, 4, c); FinishedCars(2.30f, .55f, 4, c); break;
            case 13: Workshop(-1.40f, -1.55f, 2.35f, 2.10f, c); Workshop(-1.40f, .90f, 2.35f, 1.35f, c); Assembly(.50f, -.60f, 4.30f, c); FinishedCars(2.20f, -.60f, 7, c); break;
            case 14: Workshop(0, -.60f, 1.75f, 4.30f, c); Assembly(-1.70f, -.60f, 4.30f, c); Assembly(1.70f, -.60f, 4.30f, c); break;
            case 15: Workshop(-1.70f, -.60f, 1.70f, 4.30f, c, true); Workshop(1.70f, -.60f, 1.70f, 4.30f, c, true); Assembly(0, -.60f, 4.30f, c); break;
        }
        Parking(2.75f, c); Hall(-2.60f, 2.75f, .60f, .55f, .35f, c);
        // Gate and car-shaped sign mark the finished-vehicle dispatch entrance.
        Box(2.30f, .10f, 2.75f, .04f, .55f, .04f, Steel);
        Box(2.30f, .50f, 2.75f, .62f, .24f, .045f, c);
        Box(2.30f, .56f, 2.78f, .40f, .07f, .025f, White);
        Box(2.30f, .63f, 2.78f, .21f, .05f, .025f, White);
        Box(2.70f, .30f, 3.12f, .65f, .035f, .035f, Robot);
        Rlgl.PopMatrix();
    }
}
