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
        Box(x, y, z, .21f, .08f, .21f, Wood);
        DrawSphereEx(new(x, y + .16f, z), .12f, 4, 8, Green);
    }
    private static void Store(float x, float z, float w, float d, float h, Color color)
    {
        Box(x, .10f, z, w, h, d, White);
        // Human-scale entrances stay the same size even on a wide or tall hall.
        Box(x, .17f, z + d / 2 + .012f, w - .13f, .29f, .024f, Glass);
        int panes = Math.Max(3, (int)(w / .24f));
        for (int i = 0; i <= panes; i++)
            Box(x - (w - .13f) / 2 + i * (w - .13f) / panes, .17f, z + d / 2 + .03f, .018f, .29f, .035f, White);
        Box(x, .10f, z + d / 2 + .048f, .23f, .29f, .025f, Asphalt);
        Box(x, .12f, z + d / 2 + .065f, .19f, .25f, .02f, Glass);
        Box(x, .12f, z + d / 2 + .08f, .012f, .25f, .018f, White);
        Box(x, .47f, z + d / 2 + .06f, w + .06f, .12f, .13f, color);
        for (int i = 0; i < panes; i++)
            Box(x - (w - .2f) / 2 + (i + .5f) * (w - .2f) / panes, h - .05f, z + d / 2 + .013f, (w - .2f) / panes - .035f, .10f, .025f, Glass);
        Box(x, h + .1f, z, w + .10f, .07f, d + .10f, color);
        SpecialBuildingDetails.Roof(x, h + .171f, z, w - .05f, d - .05f, color);
        // Grocery basket emblem, readable from above and from the street.
        Box(x, .49f, z + d / 2 + .135f, .21f, .04f, .02f, White);
        foreach (int side in new[] { -1, 1 }) Box(x + side * .085f, .53f, z + d / 2 + .135f, .016f, .04f, .02f, White);
        for (int i = 0; i < 3; i++) DrawSphereEx(new(x + (i - 1) * .052f, .565f, z + d / 2 + .14f), .026f, 4, 6, i == 1 ? Green : new(241, 194, 88, 255));
        // Side windows and rear loading shutter distinguish it from a blank box.
        foreach (int side in new[] { -1, 1 })
            for (int i = 0; i < Math.Max(1, (int)(d / .3f)); i++)
                Box(x + side * (w / 2 + .01f), .32f, z - d / 2 + .15f + i * .3f, .025f, .16f, .20f, Glass);
        Box(x + w * .23f, .1f, z - d / 2 - .012f, .28f, .32f, .025f, Asphalt);
    }
    private static void Gable(float x, float z, float w, float d, float y, Color color)
    {
        foreach (int side in new[] { -1, 1 })
        {
            Rlgl.PushMatrix(); Rlgl.Translatef(x + side * w / 4, y, z); Rlgl.Rotatef(-side * 22, 0, 0, 1);
            DrawCube(Vector3.Zero, w * .55f, .07f, d, color);
            SpecialBuildingDetails.Roof(0, .036f, 0, w * .55f, d, color); Rlgl.PopMatrix();
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
        SpecialBuildingDetails.Paving(0, .086f, 0, 3.94f, 3.94f);
        Box(0, .086f, 1.32f, 3.75f, .012f, 1.12f, Asphalt);
        // Parking, a clear pedestrian approach, bicycle stands and roadside sign.
        for (int i = 0; i < 11; i++)
        {
            float px = -1.70f + i * .34f;
            if (Math.Abs(px) < .25f) continue;
            Box(px, .10f, 1.18f, .014f, .008f, .43f, White);
            if (i is 0 or 2 or 7 or 9)
            {
                Box(px + .16f, .11f, 1.18f, .14f, .12f, .27f, i % 2 == 0 ? color : White);
                Box(px + .16f, .23f, 1.18f, .115f, .045f, .13f, Glass);
            }
        }
        for (int i = 0; i < 7; i++) Box(0, .103f, 1.0f + i * .13f, .28f, .009f, .045f, White);
        Box(1.68f, .1f, .56f, .035f, .70f, .035f, Asphalt);
        Box(1.68f, .64f, .56f, .28f, .23f, .055f, color);
        Box(1.68f, .72f, .596f, .18f, .035f, .02f, White);
        for (int i = 0; i < 5; i++) Box(-1.70f + i * .10f, .10f, .67f, .018f, .11f, .15f, White);
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
                    Rlgl.PushMatrix(); Rlgl.Translatef(i * .9f, .1f, .35f); Rlgl.Scalef(.65f, .60f, .65f); Rlgl.Translatef(0, -.1f, 0);
                    Canopy(0, 0, .72f, .65f, i % 2 == 0 ? color : White);
                    Box(0, .1f, 0, .60f, .3f, .40f, Wood);
                    for (int j = -2; j <= 2; j++) DrawSphereEx(new(j * .10f, .43f, 0), .045f, 4, 6, j == 0 ? Green : color);
                    Rlgl.PopMatrix();
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
