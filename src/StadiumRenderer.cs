using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class StadiumRenderer
{
    private readonly record struct Design(float Width, float Depth, int Rows, int Ends, int Roof, int Lights, int Annex, bool Track);
    // Different bowl proportions, stand arrangements, roofs and auxiliary buildings.
    private static readonly Design[] Designs =
    [
        new(3.6f, 2.8f, 4, 2, 1, 4, 0, true), new(3.8f, 2.5f, 5, 1, 2, 2, 1, false),
        new(3.4f, 3.2f, 4, 2, 3, 4, 2, false), new(4.0f, 2.5f, 3, 0, 1, 4, 3, true),
        new(3.8f, 2.6f, 3, 1, 0, 2, 4, true), new(3.5f, 3.0f, 4, 2, 4, 0, 0, false),
        new(3.9f, 2.7f, 3, 0, 2, 2, 2, false), new(3.3f, 3.2f, 4, 1, 1, 4, 4, true),
        new(3.7f, 2.7f, 6, 2, 3, 4, 1, false), new(3.8f, 2.5f, 5, 2, 2, 2, 3, true),
        new(3.2f, 3.4f, 5, 2, 1, 4, 2, false), new(4.0f, 2.5f, 5, 1, 3, 0, 1, false),
        new(3.7f, 2.9f, 4, 2, 5, 4, 3, true), new(3.9f, 2.6f, 4, 0, 5, 2, 0, false),
        new(3.3f, 3.2f, 5, 1, 4, 4, 2, true), new(3.8f, 2.7f, 6, 2, 5, 4, 4, false)
    ];
    private static void Stand(float x, float z, float length, int rows, int rotation, Color c, bool roof, bool solar)
    {
        Rlgl.PushMatrix(); Rlgl.Translatef(x, 0, z); Rlgl.Rotatef(rotation, 0, 1, 0);
        for (int row = 0; row < rows; row++)
        {
            float y = .13f + row * .115f, pz = row * .13f;
            Box(0, .105f, pz, length, y - .105f + .025f, .135f, White);
            for (float px = -length / 2 + .09f; px < length / 2; px += .12f)
            {
                if (Math.Abs(px) < .09f || Math.Abs(Math.Abs(px) - length * .29f) < .075f) continue;
                Box(px, y + .025f, pz, .075f, .045f, .07f, c);
            }
        }
        float back = rows * .13f;
        Box(0, .1f, back, length, rows * .115f + .12f, .08f, White);
        for (float px = -length / 2 + .2f; px < length / 2; px += .7f)
            Box(px, .1f, back + .045f, .18f, .28f, .02f, Glass);
        if (roof)
        {
            float y = .4f + rows * .115f;
            for (float px = -length / 2 + .15f; px < length / 2; px += .6f)
                Box(px, .1f, back + .06f, .03f, y - .1f, .03f, Steel);
            Box(0, y, back / 2, length + .12f, .045f, back + .34f, c);
            if (solar) Solar(-length / 2 + .16f, y + .05f, back / 2, (int)((length - .2f) / .32f));
        }
        Rlgl.PopMatrix();
    }
    private static void Light(float x, float z)
    {
        Box(x, .1f, z, .045f, 1.65f, .045f, Steel);
        Box(x, 1.75f, z, .38f, .18f, .07f, Steel);
        for (int i = -1; i <= 1; i++) Box(x + i * .115f, 1.78f, z + .042f, .085f, .11f, .025f, White);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle); var d = Designs[tile.LandmarkStyle];
        float cz = -.5f;
        Box(0, .105f, cz, d.Width + .5f, .018f, d.Depth + .5f, d.Track ? new(183, 124, 102, 255) : Steel);
        Box(0, .125f, cz, d.Width, .012f, d.Depth, Green);
        for (int i = 0; i < 8; i++) if (i % 2 == 0)
            Box(-d.Width / 2 + (i + .5f) * d.Width / 8, .138f, cz, d.Width / 8, .003f, d.Depth, new(119, 169, 116, 255));
        foreach (int side in new[] { -1, 1 })
        {
            Box(0, .142f, cz + side * (d.Depth / 2 - .12f), d.Width - .25f, .007f, .025f, White);
            Box(side * (d.Width / 2 - .12f), .142f, cz, .025f, .007f, d.Depth - .25f, White);
            Box(side * (d.Width / 2 - .12f), .15f, cz, .025f, .22f, .025f, White);
            foreach (int post in new[] { -1, 1 }) Box(side * (d.Width / 2 - .12f), .15f, cz + post * .24f, .025f, .22f, .025f, White);
            Box(side * (d.Width / 2 - .12f), .37f, cz, .025f, .025f, .5f, White);
            Stand(0, cz + side * (d.Depth / 2 + .32f), d.Width + .4f, d.Rows, side == 1 ? 0 : 180, c,
                d.Roof >= 2 || d.Roof == 1 && side == -1, d.Roof == 5);
        }
        Box(0, .142f, cz, .025f, .007f, d.Depth - .25f, White);
        for (int i = 0; i < 32; i++)
        {
            float a = i * MathF.Tau / 32, b = (i + 1) * MathF.Tau / 32;
            Beam(new(MathF.Cos(a) * .35f, .15f, cz + MathF.Sin(a) * .35f), new(MathF.Cos(b) * .35f, .15f, cz + MathF.Sin(b) * .35f), .009f, White);
        }
        if (d.Ends > 0) Stand(-d.Width / 2 - .32f, cz, d.Depth, d.Rows - 1, 270, c, d.Roof is 3 or 4, false);
        if (d.Ends > 1) Stand(d.Width / 2 + .32f, cz, d.Depth, d.Rows - 1, 90, c, d.Roof == 3, false);
        if (d.Lights > 0) { Light(-3.25f, -2.9f); Light(3.25f, -2.9f); }
        if (d.Lights > 2) { Light(-3.25f, 1.8f); Light(3.25f, 1.8f); }
        // Ordinary-size turnstiles, entrances and shops around the broad arena.
        switch (d.Annex)
        {
            case 0: Hall(0, 2.75f, 3.4f, .5f, .38f, c); break;
            case 1: Hall(-1.85f, 2.7f, 1.0f, .65f, .6f, c); Hall(1.85f, 2.7f, 1.0f, .65f, .6f, c); break;
            case 2: for (int i = -1; i <= 1; i++) Hall(i * 1.65f, 2.75f, .75f, .5f, .35f, c); break;
            case 3: Gable(-1.7f, 2.7f, 1.65f, .6f, .45f, c); Arcade(1.35f, 2.7f, 1.8f, c); break;
            case 4: Arcade(0, 2.75f, 3.3f, c); Tree(-2.35f, 2.75f); Tree(2.35f, 2.75f); break;
        }
        Box(0, .15f, -3.15f, .045f, 1.0f, .045f, Steel);
        Box(0, 1.15f, -3.15f, .9f, .4f, .08f, Steel);
        for (int i = -1; i <= 1; i++) Box(i * .22f, 1.25f, -3.1f, .12f, .12f, .01f, White);
        Rlgl.PopMatrix();
    }
}
