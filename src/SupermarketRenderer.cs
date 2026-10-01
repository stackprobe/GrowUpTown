using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class SupermarketRenderer
{
    private static readonly Color Paving = new(204, 208, 191, 255), Asphalt = new(97, 113, 120, 255);
    private static readonly Color Glass = new(92, 162, 183, 255), White = new(236, 233, 213, 255);
    private static readonly Color Green = new(86, 153, 105, 255), Wood = new(158, 116, 77, 255);
    private static readonly Color[] Colors = [new(214, 102, 87, 255), new(66, 153, 181, 255), new(120, 158, 83, 255), new(205, 158, 70, 255),
        new(235, 159, 83, 255), new(123, 123, 185, 255), new(66, 168, 166, 255), new(157, 181, 109, 255),
        new(200, 92, 116, 255), new(178, 115, 83, 255), new(98, 139, 179, 255), new(113, 163, 115, 255),
        new(100, 172, 197, 255), new(150, 131, 184, 255), new(234, 175, 78, 255), new(131, 178, 136, 255)];
    private static void Box(float x, float y, float z, float w, float h, float d, Color c) => DrawCube(new(x, y + h / 2, z), w, h, d, c);
    private static void Planter(float x, float z, float y = .10f)
    {
        Box(x, y, z, .32f, .13f, .32f, Wood);
        DrawSphereEx(new(x, y + .25f, z), .19f, 4, 8, Green);
    }
    private static void Store(float x, float z, float w, float d, float h, Color color)
    {
        Box(x, .10f, z, w, h, d, White);
        // Glazing, automatic doors and a colored fascia on the permanent front.
        Box(x, .23f, z + d / 2 + .012f, w - .13f, h * .52f, .024f, Glass);
        for (int i = -2; i <= 2; i++) Box(x + i * (w - .2f) / 5, .22f, z + d / 2 + .03f, .028f, h * .54f, .035f, White);
        Box(x, .13f, z + d / 2 + .048f, .32f, h * .66f, .025f, Asphalt);
        Box(x, .16f, z + d / 2 + .065f, .25f, h * .54f, .02f, Glass);
        Box(x, .16f, z + d / 2 + .08f, .016f, h * .54f, .018f, White);
        Box(x, h * .75f, z + d / 2 + .08f, w + .08f, .22f, .18f, color);
        Box(x, h + .1f, z, w + .10f, .07f, d + .10f, color);
        // Grocery basket emblem, readable from above and from the street.
        Box(x, h * .75f + .04f, z + d / 2 + .178f, .32f, .07f, .025f, White);
        foreach (int side in new[] { -1, 1 }) Box(x + side * .13f, h * .75f + .1f, z + d / 2 + .178f, .025f, .065f, .025f, White);
        for (int i = 0; i < 3; i++) DrawSphereEx(new(x + (i - 1) * .08f, h * .75f + .14f, z + d / 2 + .19f), .043f, 4, 6, i == 1 ? Green : new(241, 194, 88, 255));
        // Side windows and rear loading shutter distinguish it from a blank box.
        foreach (int side in new[] { -1, 1 }) Box(x + side * (w / 2 + .01f), .34f, z, .025f, .23f, d * .55f, Glass);
        Box(x + w * .23f, .1f, z - d / 2 - .012f, w * .24f, .43f, .025f, Asphalt);
    }
    private static void Gable(float x, float z, float w, float d, float y, Color color)
    {
        foreach (int side in new[] { -1, 1 })
        {
            Rlgl.PushMatrix(); Rlgl.Translatef(x + side * w / 4, y, z); Rlgl.Rotatef(-side * 22, 0, 0, 1);
            DrawCube(Vector3.Zero, w * .55f, .07f, d, color); Rlgl.PopMatrix();
        }
    }
    private static void Canopy(float x, float z, float w, float d, Color color)
    {
        foreach (int side in new[] { -1, 1 }) Box(x + side * (w / 2 - .08f), .1f, z, .055f, .65f, .055f, White);
        Box(x, .75f, z, w, .065f, d, color);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        int id = tile.MarketStyle; Color color = Colors[id];
        Rlgl.PushMatrix(); Rlgl.Translatef(x + 1.5f, 0, z + 1.5f); Rlgl.Rotatef(tile.MarketRotation * 90, 0, 1, 0);
        Box(0, .02f, 0, 3.96f, .065f, 3.96f, Paving);
        Box(0, .086f, 1.32f, 3.75f, .012f, 1.12f, Asphalt);
        // Parking, a clear pedestrian approach, bicycle stands and roadside sign.
        for (int i = 0; i < 6; i++)
        {
            float px = -1.63f + i * .65f;
            Box(px, .10f, 1.35f, .027f, .008f, .76f, White);
            if (i is 0 or 4)
            {
                Box(px + .24f, .11f, 1.38f, .32f, .16f, .62f, i == 0 ? color : White);
                Box(px + .24f, .27f, 1.38f, .28f, .1f, .30f, Glass);
            }
        }
        for (int i = 0; i < 4; i++) Box(0, .103f, 1.02f + i * .23f, .4f, .009f, .09f, White);
        Box(1.68f, .1f, .56f, .055f, 1.1f, .055f, Asphalt);
        Box(1.68f, .88f, .56f, .42f, .45f, .09f, color);
        Box(1.68f, 1.02f, .612f, .27f, .055f, .02f, White);
        for (int i = 0; i < 3; i++) Box(-1.70f + i * .16f, .10f, .67f, .04f, .20f, .26f, White);
        Planter(-1.72f, -1.67f); Planter(1.72f, -1.67f);
        switch (id)
        {
            case 0: // Neighborhood gabled grocery
                Store(0, -.55f, 2.80f, 1.85f, .80f, color); Gable(0, -.55f, 2.98f, 2.02f, 1.16f, color); break;
            case 1: // Urban glass hall with a raised lantern
                Store(0, -.55f, 3.05f, 1.85f, 1.05f, color);
                Box(0, 1.22f, -.55f, 1.25f, .38f, 1.3f, Glass); Box(0, 1.6f, -.55f, 1.36f, .06f, 1.4f, White); break;
            case 2: // Organic green-roof store
                Store(0, -.6f, 2.8f, 1.7f, .85f, color); Box(0, 1.03f, -.6f, 2.55f, .08f, 1.45f, Green);
                for (int i = -1; i <= 1; i++) Planter(i * .7f, -.6f, 1.11f); break;
            case 3: // Wholesale shed, sawtooth roof
                Store(0, -.57f, 3.1f, 2.05f, .95f, color);
                for (int i = -1; i <= 1; i++) Gable(i * .95f, -.57f, .85f, 1.95f, 1.2f, Asphalt); break;
            case 4: // Family shop with a broad striped awning
                Store(0, -.70f, 2.6f, 1.5f, .8f, color);
                for (int i = 0; i < 10; i++) Box(-1.35f + i * .3f, .78f, .45f, .3f, .06f, .75f, i % 2 == 0 ? color : White);
                Planter(-1.45f, .28f); Planter(1.45f, .28f); break;
            case 5: // Twin market wings and connecting atrium
                Store(-.84f, -.60f, 1.25f, 1.8f, 1.05f, color); Store(.84f, -.60f, 1.25f, 1.8f, .80f, color);
                Box(0, .1f, -.5f, .40f, .75f, 1.35f, Glass); break;
            case 6: // Waterfront market with deck
                Store(-.4f, -.65f, 2.2f, 1.7f, .85f, color);
                Box(1.16f, .1f, -.45f, .73f, .10f, 1.95f, Wood); Canopy(1.16f, -.65f, .72f, 1.4f, White);
                Planter(1.15f, .30f); break;
            case 7: // Courtyard grocery
                Store(-1, -.50f, .9f, 1.8f, .8f, color); Store(1, -.50f, .9f, 1.8f, .8f, color);
                Store(0, -1.2f, 1.15f, .6f, .8f, color); Planter(0, -.25f); break;
            case 8: // Arcaded market hall
                Store(0, -.8f, 2.9f, 1.5f, 1.05f, color); Canopy(0, .30f, 3.12f, .70f, color);
                for (int i = -2; i <= 2; i++) Box(i * .55f, .1f, .5f, .07f, .65f, .07f, White); break;
            case 9: // Brick neighborhood store with clock tower
                Store(0, -.62f, 2.85f, 1.7f, .85f, color); Gable(0, -.62f, 3, 1.85f, 1.16f, color);
                Box(-1.1f, .1f, .20f, .45f, 1.65f, .42f, color);
                Box(-1.1f, 1.37f, .418f, .30f, .30f, .02f, White);
                Box(-1.1f, 1.5f, .433f, .025f, .11f, .015f, Asphalt); break;
            case 10: // Distribution annex and raised loading dock
                Store(-.38f, -.45f, 2.28f, 1.8f, .9f, color);
                Box(1.12f, .1f, -.8f, .75f, 1.25f, 1.5f, Asphalt); Box(1.12f, .1f, .2f, .80f, .16f, .6f, color);
                for (int i = 0; i < 3; i++) Box(1.0f + i * .18f, .26f, .2f, .15f, .18f, .20f, Wood); break;
            case 11: // Solar market
                Store(0, -.6f, 2.85f, 1.85f, .85f, color);
                for (int i = -1; i <= 1; i++) for (int j = 0; j < 2; j++) Box(i * .85f, 1.03f, -.98f + j * .7f, .72f, .05f, .55f, new(54, 88, 129, 255));
                Canopy(0, .60f, 2.9f, .45f, Glass); break;
            case 12: // Coastal white portico
                Store(0, -.60f, 2.65f, 1.8f, .8f, color); Canopy(0, .45f, 2.9f, .6f, White);
                Box(0, .82f, .45f, 1.1f, .40f, .55f, White); Gable(0, .45f, 1.35f, .7f, 1.32f, color); break;
            case 13: // Stepped city supermarket
                Store(0, -.5f, 3, 1.95f, .85f, color);
                Box(-.6f, 1.03f, -.8f, 1.65f, .4f, 1.15f, Glass); Box(-.6f, 1.43f, -.8f, 1.76f, .07f, 1.25f, color);
                Box(.95f, 1.03f, -.9f, .42f, .22f, .45f, Asphalt); break;
            case 14: // Food hall with sheltered produce stalls
                Store(0, -.92f, 2.95f, 1.3f, .85f, color);
                for (int i = -1; i <= 1; i++)
                {
                    Canopy(i * .9f, .35f, .72f, .65f, i % 2 == 0 ? color : White);
                    Box(i * .9f, .1f, .35f, .60f, .3f, .40f, Wood);
                    for (int j = -1; j <= 1; j++) DrawSphereEx(new(i * .9f + j * .15f, .45f, .35f), .09f, 4, 6, j == 0 ? Green : color);
                }
                break;
            case 15: // Garden terrace store
                Store(0, -.62f, 2.85f, 1.75f, .95f, color); Box(0, 1.13f, -.62f, 2.6f, .06f, 1.5f, Wood);
                for (int i = -1; i <= 1; i++) Planter(i * .80f, -1.03f, 1.19f);
                for (int i = -1; i <= 1; i++) Box(i * 1.05f, 1.19f, -.25f, .035f, .40f, .035f, White);
                Box(0, 1.59f, -.25f, 2.6f, .06f, .55f, Green); break;
        }
        Rlgl.PopMatrix();
    }
}
