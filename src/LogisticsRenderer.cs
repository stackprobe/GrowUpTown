using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class LogisticsRenderer
{
    private static readonly Color Concrete = new(194, 201, 191, 255), Asphalt = new(87, 103, 113, 255);
    private static readonly Color Steel = new(153, 172, 182, 255), White = new(232, 233, 216, 255);
    private static readonly Color Glass = new(91, 161, 185, 255), Yellow = new(240, 188, 80, 255);
    private static readonly Color Green = new(93, 150, 109, 255), Wood = new(162, 119, 82, 255);
    private static readonly Color[] Accents = [new(95, 145, 188, 255), new(221, 148, 77, 255), new(111, 154, 180, 255), new(87, 170, 160, 255),
        new(132, 170, 105, 255), new(183, 135, 93, 255), new(195, 104, 110, 255), new(149, 174, 132, 255),
        new(119, 130, 172, 255), new(228, 170, 69, 255), new(131, 191, 205, 255), new(93, 154, 127, 255),
        new(224, 161, 62, 255), new(155, 125, 175, 255), new(139, 163, 174, 255), new(169, 146, 109, 255)];
    private static void Box(float x, float y, float z, float w, float h, float d, Color c) => DrawCube(new(x, y + h / 2, z), w, h, d, c);
    private static void Warehouse(float x, float z, float w, float d, float h, Color accent, int bays = 3)
    {
        Box(x, .13f, z, w, h, d, Concrete);
        Box(x, .13f + h, z, w + .10f, .08f, d + .10f, accent);
        SpecialBuildingDetails.Roof(x, h + .211f, z, w - .06f, d - .06f, accent);
        Box(x, h - .03f, z + d / 2 + .02f, w, .12f, .035f, accent);
        // Wider halls have more loading doors, rather than oversized doors.
        bays = Math.Max(bays, (int)(w / .42f));
        for (int i = 0; i < bays; i++)
        {
            float bx = x + (i - (bays - 1) / 2f) * w / bays;
            float doorWidth = Math.Min(.28f, w / bays * .66f);
            Box(bx, .13f, z + d / 2 + .02f, doorWidth, .33f, .035f, Asphalt);
            for (int slat = 0; slat < 6; slat++) Box(bx, .16f + slat * .047f, z + d / 2 + .044f, doorWidth - .025f, .012f, .015f, Steel);
            Box(bx, .108f, z + d / 2 + .09f, doorWidth + .06f, .022f, .18f, Concrete);
            foreach (int side in new[] { -1, 1 }) Box(bx + side * (doorWidth / 2 + .035f), .13f, z + d / 2 + .14f, .023f, .14f, .023f, Yellow);
            Box(bx, h - .24f, z + d / 2 + .02f, doorWidth, .10f, .025f, Glass);
        }
        foreach (int side in new[] { -1, 1 })
            for (int i = 0; i < Math.Max(2, (int)(d / .22f)); i++)
                Box(x + side * (w / 2 + .012f), .19f, z - d / 2 + .10f + i * .22f, .02f, h - .12f, .018f, Steel);
    }
    private static void Container(float x, float z, float y, Color c, float d = .85f)
    {
        Box(x, y, z, .26f, .24f, d, c);
        for (int i = -2; i <= 2; i++) Box(x + i * .045f, y + .02f, z + d / 2 + .007f, .009f, .20f, .015f, Steel);
        Box(x, y + .24f, z, .27f, .015f, d + .02f, Steel);
    }
    private static void Truck(float x, float z, Color c)
    {
        // About twice the length of a road car, with wheels resting on the apron.
        Box(x, .16f, z - .06f, .18f, .20f, .34f, White);
        Box(x, .15f, z + .17f, .18f, .16f, .14f, c);
        Box(x, .23f, z + .245f, .14f, .065f, .012f, Glass);
        foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) Box(x + a * .09f, .108f, z + b * .16f, .025f, .08f, .08f, Asphalt);
    }
    private static void Office(float x, float z, float w, float d, float h, Color color)
    {
        Box(x, .13f, z, w, h, d, White);
        for (float y = .38f; y < h; y += .3f)
        {
            Box(x, y, z + d / 2 + .01f, w * .82f, .16f, .025f, Glass);
            Box(x + w / 2 + .01f, y, z, .025f, .16f, d * .82f, Glass);
            for (float px = -w * .35f; px < w * .4f; px += .19f) Box(x + px, y, z + d / 2 + .027f, .016f, .16f, .015f, White);
            for (float pz = -d * .35f; pz < d * .4f; pz += .19f) Box(x + w / 2 + .027f, y, z + pz, .015f, .16f, .016f, White);
        }
        Box(x, .13f, z + d / 2 + .025f, .13f, .24f, .025f, Glass);
        Box(x, h + .13f, z, w + .06f, .07f, d + .06f, color);
    }
    private static void Solar(float x, float z, float y, int count)
    {
        for (int i = 0; i < count; i++) for (int j = 0; j < 2; j++)
            Box(x + (i - (count - 1) / 2f) * .62f, y, z + (j - .5f) * .58f, .53f, .055f, .47f, new(48, 81, 122, 255));
    }
    private static void Tree(float x, float z, float y = .12f)
    {
        Box(x, y, z, .08f, .24f, .08f, Wood);
        DrawSphereEx(new(x, y + .38f, z), .21f, 4, 8, Green);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Color color = Accents[tile.LogisticsStyle];
        Rlgl.PushMatrix(); Rlgl.Translatef(x + 1.5f, 0, z + 1.5f); Rlgl.Rotatef(tile.LogisticsRotation * 90, 0, 1, 0);
        Box(0, .02f, 0, 3.96f, .075f, 3.96f, Concrete);
        Box(0, .096f, 0, 3.78f, .012f, 3.78f, Asphalt);
        // Loading apron, parked trucks and a gate remain unchanged after placement.
        for (int i = -4; i <= 4; i++) Box(i * .37f, .11f, 1.05f, .014f, .009f, .64f, Yellow);
        Truck(-1.29f, 1.04f, color); Truck(.93f, 1.04f, color); Truck(.19f, 1.04f, White);
        Box(-1.72f, .11f, 1.71f, .05f, .23f, .05f, White);
        Box(-1.47f, .31f, 1.71f, .53f, .024f, .024f, Yellow);
        Box(1.69f, .11f, .79f, .035f, .60f, .035f, Steel);
        Box(1.69f, .58f, .79f, .27f, .19f, .04f, color);
        // Parcel symbol on the facility sign.
        Box(1.69f, .62f, .817f, .13f, .10f, .012f, White);
        Box(1.69f, .62f, .828f, .016f, .10f, .01f, color);
        for (int i = 0; i < 5; i++) Box(-1.70f, .11f, -.3f - i * .23f, .14f, .12f, .14f, Wood);
        switch (tile.LogisticsStyle)
        {
            case 0: // Broad distribution shed
                Warehouse(0, -.55f, 2.95f, 1.8f, .85f, color, 4);
                for (int i = -1; i <= 1; i++) Box(i * .8f, 1.08f, -.55f, .48f, .08f, .9f, Glass); break;
            case 1: // Twin warehouses
                Warehouse(-.8f, -.60f, 1.25f, 1.8f, .80f, color, 2); Warehouse(.8f, -.60f, 1.25f, 1.8f, 1.05f, color, 2); break;
            case 2: // Office-front urban depot
                Warehouse(-.4f, -.60f, 2.1f, 1.8f, .90f, color); Office(1.12f, -.75f, .65f, 1.55f, 1.60f, color); break;
            case 3: // Container freight yard
                for (int i = -3; i <= 3; i++) for (int j = 0; j < 2; j++) Container(i * .42f, -.4f - j * .95f, .11f, i % 2 == 0 ? color : Steel);
                Container(-.84f, -1.35f, .365f, color); Container(.84f, -.4f, .365f, Yellow); break;
            case 4: // Solar logistics hall
                Warehouse(0, -.55f, 2.85f, 1.9f, .85f, color, 4); Solar(0, -.55f, 1.08f, 4); break;
            case 5: // Sawtooth industrial depot
                Warehouse(0, -.6f, 2.9f, 1.8f, .85f, color);
                for (int i = -1; i <= 1; i++)
                {
                    Box(i * .9f, 1.02f, -.60f, .70f, .28f, 1.7f, Glass);
                    Rlgl.PushMatrix(); Rlgl.Translatef(i * .9f, 1.30f, -.60f); Rlgl.Rotatef(18, 0, 0, 1);
                    DrawCube(Vector3.Zero, .88f, .06f, 1.85f, color); Rlgl.PopMatrix();
                }
                break;
            case 6: // Cross-dock center
                Warehouse(0, -.50f, 3.05f, 1.05f, .85f, color, 5);
                for (int i = -1; i <= 1; i++) Container(i * .95f, -1.43f, .11f, Steel, .48f); break;
            case 7: // Buffered neighborhood depot
                Warehouse(0, -.55f, 2.25f, 1.75f, .75f, color);
                foreach (int side in new[] { -1, 1 }) for (int i = 0; i < 4; i++) Tree(side * 1.46f, -.15f - i * .46f);
                break;
            case 8: // Tall automated storage hall
                Warehouse(-.3f, -.65f, 2.20f, 1.8f, 1.75f, color, 3); Office(1.16f, -.45f, .60f, 1.1f, .65f, color);
                for (int i = -1; i <= 1; i++) Box(-.3f + i * .6f, .70f, .263f, .05f, 1.0f, .025f, White); break;
            case 9: // Parcel sorting center
                Warehouse(0, -.96f, 2.9f, 1.05f, .85f, color, 4);
                Box(0, .29f, -.04f, 2.75f, .10f, .3f, Steel);
                for (int i = -5; i <= 5; i++) Box(i * .23f, .39f, -.04f, .10f, .09f, .12f, Wood);
                Box(-1.32f, .11f, -.04f, .07f, .18f, .22f, White); Box(1.32f, .11f, -.04f, .07f, .18f, .22f, White); break;
            case 10: // Refrigerated warehouse
                Warehouse(-.25f, -.6f, 2.4f, 1.8f, 1.05f, color);
                for (int i = 0; i < 3; i++)
                {
                    DrawCylinder(new(1.32f, .13f, -1.15f + i * .56f), .21f, .21f, .8f, 12, White);
                    Box(-.85f + i * .60f, 1.26f, -.6f, .4f, .20f, .50f, Steel);
                }
                break;
            case 11: // Planted roof depot
                Warehouse(0, -.55f, 2.85f, 1.8f, .85f, color);
                Box(0, 1.04f, -.55f, 2.60f, .07f, 1.55f, Green);
                for (int i = -1; i <= 1; i++) Tree(i * .75f, -.85f, 1.11f); break;
            case 12: // Gantry crane freight terminal
                Warehouse(-.8f, -.8f, 1.2f, 1.6f, .8f, color, 2);
                Container(.62f, -.80f, .11f, Steel); Container(1.24f, -.80f, .11f, color);
                foreach (int side in new[] { -1, 1 }) Box(.88f + side * .62f, .11f, -.80f, .08f, 1.55f, .09f, Yellow);
                Box(.88f, 1.66f, -.80f, 1.50f, .12f, .22f, Yellow);
                Box(.88f, .95f, -.80f, .025f, .70f, .025f, Steel); break;
            case 13: // Multi-level depot with side ramp
                Warehouse(-.35f, -.60f, 2.2f, 1.8f, 1.3f, color); Box(-.35f, .85f, -.60f, 2.28f, .09f, 1.88f, White);
                Rlgl.PushMatrix(); Rlgl.Translatef(1.10f, .50f, -.60f); Rlgl.Rotatef(24, 1, 0, 0);
                DrawCube(Vector3.Zero, .55f, .07f, 2.05f, Concrete); Rlgl.PopMatrix(); break;
            case 14: // Sheltered central loading court
                Warehouse(-1.0f, -.60f, .85f, 1.8f, .9f, color, 1); Warehouse(1.0f, -.60f, .85f, 1.8f, .9f, color, 1);
                Warehouse(0, -1.25f, 1.16f, .5f, .9f, color, 2); Box(0, 1.12f, -.2f, 1.35f, .06f, .75f, Glass); break;
            case 15: // Timber and solar consolidation hub
                Warehouse(0, -.55f, 2.85f, 1.85f, .8f, color);
                for (int i = -3; i <= 3; i++) Box(i * .4f, .15f, .40f, .06f, .66f, .06f, Wood);
                Solar(-.6f, -.65f, 1.03f, 2); Box(.9f, 1.03f, -.65f, .6f, .08f, 1.30f, Green);
                Tree(1.55f, -.8f); break;
        }
        Rlgl.PopMatrix();
    }
}
