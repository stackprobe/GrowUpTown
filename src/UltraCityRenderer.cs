using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class UltraCityRenderer
{
    private static void Box(float x, float y, float z, float w, float h, float d, Color color) =>
        DrawCube(new(x, y + h / 2, z), w, h, d, color);

    internal static void Draw(float x, float z, Tile tile, double time)
    {
        Color frame = new(230, 229, 209, 255), dark = new(62, 79, 88, 255);
        Color light = new(146, 207, 222, 255), garden = new(119, 183, 103, 255);
        Color industry = new(224, 160, 83, 255);
        int variant = UltraAppearance.RoofVariant(tile);
        float story = BuildingFacades.FloorHeight(tile.Kind), height = BuildingFacades.Height(tile);
        Box(x, .02f, z, 2.96f, .10f, 2.96f, frame);
        for (int floor = 0; floor < tile.Level; floor++)
        {
            float y = .12f + floor * story;
            // The batched facade atlas supplies all three RCI colors and face-specific details.
            Box(x, y, z, 2.32f, story, 2.32f, dark);
            Box(x, y, z, 2.45f, .055f, 2.45f, frame);
            // Permanent sky gardens on every third completed segment.
            if ((floor + variant) % 3 == 2)
            {
                Box(x, y, z, 2.77f, .10f, 2.77f, frame);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(x, y + .1f, z + side * 1.31f, 1.75f, .15f, .16f, garden);
                    Box(x + side * 1.31f, y + .1f, z, .16f, .15f, 1.75f, garden);
                }
            }
        }
        for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
        {
            Box(x + sx * 1.10f, .12f, z + sz * 1.10f, .22f, height + .22f, .22f, frame);
            Box(x + sx * 1.10f, height + .34f, z + sz * 1.10f, .24f, .06f, .24f, light);
        }
        // Retail concourse, production modules and sealed filtration equipment.
        Box(x, .12f, z + 1.20f, 1.60f, .50f, .12f, dark);
        Box(x, .62f, z + 1.28f, 1.90f, .07f, .32f, light);
        for (int i = -1; i <= 1; i++)
        {
            Box(x + i * .48f, .16f, z + 1.27f, .32f, .32f, .04f, new(147, 214, 223, 255));
            Box(x + 1.28f, .12f, z + i * .62f, .28f, .47f, .46f, industry);
            Box(x + 1.28f, .59f, z + i * .62f, .29f, .06f, .47f, frame);
        }
        Box(x, height + .12f, z, 2.58f, .12f, 2.58f, frame);
        Box(x, height + .24f, z, 1.86f, .10f, 1.86f, garden);
        if (variant == 0)
        {
            Box(x, height + .34f, z, 1.02f, .50f, 1.02f, light);
            Box(x, height + .84f, z, 1.24f, .07f, 1.24f, frame);
            Box(x, height + .91f, z, .065f, .45f, .065f, industry);
        }
        else if (variant == 1)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Box(x + side * .44f, height + .34f, z, .60f, .66f, .80f, side == 1 ? industry : light);
                Box(x + side * .44f, height + 1f, z, .70f, .06f, .90f, frame);
            }
        }
        else if (variant == 2)
        {
            Box(x, height + .34f, z, 1.25f, .26f, 1.05f, industry);
            Box(x, height + .60f, z, .95f, .28f, .80f, garden);
            Box(x, height + .88f, z, .62f, .20f, .55f, light);
        }
        else
        {
            Box(x, height + .34f, z, 1.40f, .16f, .90f, frame);
            for (int i = -1; i <= 1; i++)
                Box(x + i * .40f, height + .50f, z, .28f, .42f, .80f, i == 0 ? industry : light);
        }
        // Harmless filtered vapor: no pollution is registered by the simulation.
        Box(x - .83f, height + .24f, z - .83f, .30f, .38f, .30f, industry);
        Box(x - .83f, height + .62f, z - .83f, .34f, .06f, .34f, frame);
        if (tile.Connected)
            for (int puff = 0; puff < 3; puff++)
            {
                float age = (float)((time / 3 + puff / 3.0) % 1);
                float radius = .10f * MathF.Sin(age * MathF.PI);
                DrawSphereEx(new(x - .83f + age * .12f, height + .76f + age * .70f, z - .83f), radius, 4, 8, new(228, 246, 244, 255));
            }
    }
}
