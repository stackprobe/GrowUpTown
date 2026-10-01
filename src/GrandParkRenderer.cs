using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal static class GrandParkRenderer
{
    private static readonly Color Grass = new(117, 174, 114, 255), Path = new(228, 216, 184, 255);
    private static readonly Color Wood = new(153, 110, 75, 255), Stone = new(189, 199, 186, 255);
    private static readonly Color Water = new(82, 170, 190, 255), Leaf = new(67, 130, 95, 255);
    private static readonly Color Rose = new(230, 137, 167, 255), Gold = new(239, 200, 101, 255);
    private static void Box(float x, float y, float z, float w, float h, float d, Color c) => DrawCube(new(x, y + h / 2, z), w, h, d, c);
    private static void Disk(float x, float z, float r, Color c, float y = .085f, float h = .035f) => DrawCylinder(new(x, y, z), r, r, h, 24, c);
    private static void Tree(float x, float z, Color? color = null, float size = 1)
    {
        Box(x, .09f, z, .08f, .65f * size, .08f, Wood);
        DrawSphereEx(new(x, .70f * size, z), .29f * size, 5, 8, color ?? Leaf);
        DrawSphereEx(new(x, .92f * size, z), .20f * size, 4, 8, color ?? new Color(117, 170, 102, 255));
    }
    private static void Bed(float x, float z, Color c, float w = .65f, float d = .65f)
    {
        Box(x, .08f, z, w, .07f, d, Stone);
        Box(x, .15f, z, w - .09f, .06f, d - .09f, Leaf);
        for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++)
            DrawSphereEx(new(x + i * (w - .2f) / 3, .23f, z + j * (d - .2f) / 3), .07f, 4, 6, c);
    }
    private static void Bench(float x, float z)
    {
        Box(x, .09f, z, .06f, .17f, .20f, Stone);
        Box(x, .26f, z, .50f, .06f, .22f, Wood);
        Box(x, .32f, z + .10f, .50f, .19f, .04f, Wood);
    }
    private static void Pond(float x, float z, float r)
    {
        Disk(x, z, r + .07f, Stone); Disk(x, z, r, Water, .125f, .01f);
    }
    private static void Pavilion(float x, float z)
    {
        Box(x, .10f, z, .90f, .10f, .90f, Wood);
        foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 })
            Box(x + a * .32f, .20f, z + b * .32f, .06f, .65f, .06f, Wood);
        DrawCylinder(new(x, .85f, z), .12f, .69f, .28f, 4, new(89, 119, 113, 255));
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Rlgl.PushMatrix();
        Rlgl.Translatef(x + 1.5f, 0, z + 1.5f);
        Rlgl.Rotatef(tile.ParkRotation * 90, 0, 1, 0);
        Box(0, .015f, 0, 3.98f, .06f, 3.98f, Grass);
        // Perimeter promenade and four permanent entrances; never reacts to later edits.
        foreach (int side in new[] { -1, 1 })
        {
            Box(side * 1.70f, .078f, 0, .22f, .014f, 3.62f, Path);
            Box(0, .078f, side * 1.70f, 3.62f, .014f, .22f, Path);
            Box(0, .078f, side * 1.85f, .34f, .014f, .30f, Path);
            Box(side * 1.85f, .078f, 0, .30f, .014f, .34f, Path);
            foreach (int half in new[] { -1, 1 })
            {
                Box(half * 1.08f, .08f, side * 1.95f, 1.70f, .07f, .04f, Stone);
                Box(side * 1.95f, .08f, half * 1.08f, .04f, .07f, 1.70f, Stone);
            }
        }
        Box(0, .078f, 1.33f, .32f, .014f, .75f, Path);
        // Entrance sign and lamps identify the whole 4x4 plot as one park.
        Box(.40f, .08f, 1.82f, .04f, .36f, .04f, Wood);
        Box(.40f, .38f, 1.82f, .32f, .18f, .06f, Leaf);
        foreach (int side in new[] { -1, 1 })
        {
            Box(side * 1.55f, .08f, 1.55f, .035f, .60f, .035f, Stone);
            DrawSphereEx(new(side * 1.55f, .72f, 1.55f), .08f, 4, 6, Gold);
        }
        switch (tile.ParkStyle)
        {
            case 0: // Axial fountain garden
                Box(0, .08f, 0, .32f, .015f, 3.4f, Path); Box(0, .08f, 0, 3.4f, .015f, .32f, Path);
                Pond(0, 0, .65f); Disk(0, 0, .18f, Stone, .14f, .35f);
                Disk(0, 0, .35f, Water, .49f); DrawSphereEx(new(0, .68f, 0), .12f, 6, 8, Water);
                foreach (int a in new[] { -1, 1 }) foreach (int b in new[] { -1, 1 }) Bed(a * 1.08f, b * 1.08f, Rose);
                break;
            case 1: // Lake with a timber bridge
                Pond(0, -.1f, 1.25f); Box(0, .21f, 0, .43f, .10f, 2.9f, Wood);
                for (int i = -1; i <= 1; i += 2) { Box(i * .23f, .32f, 0, .04f, .23f, 2.5f, Wood); Tree(i * 1.35f, i * 1.1f); }
                Bench(1.1f, 1.1f); break;
            case 2: // Playground
                Box(0, .085f, 0, 2.6f, .03f, 2.4f, Path);
                Box(-.65f, .12f, -.55f, .75f, .10f, .8f, Gold);
                Box(.60f, .12f, -.55f, .38f, .65f, .38f, Wood);
                Box(.60f, .77f, -.55f, .6f, .08f, .6f, Rose);
                Rlgl.PushMatrix(); Rlgl.Translatef(.60f, .44f, .05f); Rlgl.Rotatef(40, 1, 0, 0);
                DrawCube(Vector3.Zero, .36f, .045f, 1.25f, Water); Rlgl.PopMatrix();
                for (int a = -1; a <= 1; a += 2) Box(-.65f + a * .35f, .12f, .65f, .055f, .8f, .055f, Wood);
                Box(-.65f, .92f, .65f, .80f, .06f, .06f, Gold);
                Box(-.65f, .37f, .65f, .32f, .04f, .20f, Rose);
                foreach (int a in new[] { -1, 1 }) Box(-.65f + a * .13f, .4f, .65f, .018f, .52f, .018f, Stone);
                Tree(-1.35f, -1.3f); Bench(0, 1.35f); break;
            case 3: // Woodland walk
                for (int i = -1; i <= 1; i++) { Box(i * .45f, .08f, i * .9f, 1.0f, .015f, .30f, Path); }
                for (int i = 0; i < 10; i++) { float a = i * MathF.Tau / 10; Tree(MathF.Cos(a) * 1.23f, MathF.Sin(a) * 1.23f, size: 1 + (i % 3) * .12f); }
                Bench(0, .6f); break;
            case 4: // Picnic grove
                for (int a = -1; a <= 1; a += 2) for (int b = -1; b <= 1; b += 2)
                {
                    float px = a * .85f, pz = b * .80f;
                    Box(px, .09f, pz, .09f, .35f, .09f, Wood); Box(px, .44f, pz, .66f, .07f, .38f, Wood);
                    Bench(px, pz + .35f); Tree(a * 1.35f, b * 1.35f);
                }
                break;
            case 5: // Open-air theatre
                for (int i = 0; i < 3; i++) Box(0, .08f, -.9f + i * .45f, 2.5f - i * .35f, .36f - i * .1f, .30f, Stone);
                Disk(0, .75f, .70f, Wood, .09f, .17f); Tree(-1.35f, 1.25f); Tree(1.35f, 1.25f); break;
            case 6: // Rose parterres
                for (int a = -1; a <= 1; a++) for (int b = -1; b <= 1; b++) Bed(a * .95f, b * .95f, (a + b) % 2 == 0 ? Rose : Gold);
                break;
            case 7: // Japanese pond and pavilion
                Pond(-.65f, -.55f, .80f); Pavilion(.75f, .65f);
                for (int i = 0; i < 5; i++) Disk(-1.1f + i * .40f, .50f - i * .15f, .12f, Stone, .12f);
                Tree(1.15f, -1.1f, Rose); Tree(-1.15f, 1.1f); break;
            case 8: // Shelter forest
                for (int i = 0; i < 5; i++) { Tree(-1.30f + i * .65f, -1.18f, size: 1.1f); Tree(-1.30f + i * .65f, -.5f, size: .85f); }
                Box(0, .08f, .28f, 2.8f, .015f, .3f, Path); Bench(-.75f, .95f); Bench(.75f, .95f); break;
            case 9: // Sculpture court
                Disk(0, 0, 1.25f, Path);
                for (int i = 0; i < 3; i++)
                {
                    float px = (i - 1) * .85f, pz = i % 2 == 0 ? -.45f : .55f;
                    Box(px, .12f, pz, .48f, .24f, .48f, Stone);
                    Rlgl.PushMatrix(); Rlgl.Translatef(px, .68f, pz); Rlgl.Rotatef(35 + i * 20, 0, 0, 1);
                    DrawCube(Vector3.Zero, .28f, .65f, .28f, i == 1 ? Gold : Water); Rlgl.PopMatrix();
                }
                Bed(-1.25f, 1.2f, Rose); Bed(1.25f, -1.2f, Gold); break;
            case 10: // Sports green
                Box(0, .09f, 0, 2.55f, .02f, 2.65f, new(85, 149, 123, 255));
                foreach (int a in new[] { -1, 1 })
                {
                    Box(a * 1.16f, .115f, 0, .035f, .008f, 2.4f, Path); Box(0, .115f, a * 1.20f, 2.35f, .008f, .035f, Path);
                    Box(a * .48f, .12f, -1.20f, .04f, .55f, .04f, Stone);
                }
                Box(0, .67f, -1.20f, 1.0f, .04f, .04f, Stone); Box(0, .115f, 0, 2.35f, .008f, .035f, Path);
                DrawSphereEx(new(.35f, .20f, .35f), .09f, 5, 8, Gold); break;
            case 11: // Hedge labyrinth
                Box(0, .08f, 0, 2.8f, .02f, 2.8f, Path);
                for (int i = 0; i < 4; i++)
                {
                    float p = -1.2f + i * .8f;
                    Box(p, .10f, i % 2 == 0 ? -.25f : .25f, .17f, .38f, 2.0f, Leaf);
                    Box(p + .19f, .10f, i % 2 == 0 ? -1.2f : 1.2f, .55f, .38f, .17f, Leaf);
                }
                break;
            case 12: // Community gardens
                for (int i = -1; i <= 1; i++) for (int j = -1; j <= 1; j++) Bed(i * .95f, j * .80f, j % 2 == 0 ? Gold : Leaf, .73f, .43f);
                Box(1.1f, .09f, 1.25f, .65f, .45f, .45f, Wood); Box(1.1f, .54f, 1.25f, .76f, .07f, .56f, Leaf); break;
            case 13: // Wetland boardwalk
                Pond(-.65f, -.45f, .85f); Pond(.65f, .60f, .8f);
                Box(0, .20f, 0, .34f, .07f, 3.4f, Wood); Box(-.65f, .20f, -.75f, 1.3f, .07f, .30f, Wood);
                for (int i = 0; i < 9; i++) Box(-1.15f + i * .27f, .12f, i % 2 == 0 ? .95f : -1.15f, .07f, .35f, .07f, Leaf);
                break;
            case 14: // Botanical collection
                for (int i = 0; i < 8; i++) { float a = i * MathF.Tau / 8; Tree(MathF.Cos(a) * 1.15f, MathF.Sin(a) * 1.15f, i % 3 == 0 ? Gold : i % 3 == 1 ? Rose : Leaf, .8f + i % 2 * .2f); }
                Disk(0, 0, .55f, Path); Bench(0, 0); break;
            case 15: // Conservatory garden
                Box(0, .1f, -.35f, 1.6f, .65f, 1.1f, new(148, 205, 196, 255));
                for (int i = -2; i <= 2; i++) { Box(i * .38f, .10f, -.35f, .035f, .70f, 1.15f, Stone); }
                Box(0, .77f, -.35f, 1.7f, .07f, 1.2f, Stone);
                Bed(-.9f, .85f, Rose); Bed(.9f, .85f, Gold); Tree(-1.3f, -1.25f); Tree(1.3f, -1.25f); break;
        }
        Rlgl.PopMatrix();
    }
}
