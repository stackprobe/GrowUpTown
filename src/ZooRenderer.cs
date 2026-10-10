using Raylib_cs;
using static Raylib_cs.Raylib;
using static GrowUpTown.LandmarkDrawing;
using static GrowUpTown.LargeSpecialDrawing;

namespace GrowUpTown;

internal static class ZooRenderer
{
    internal static void Animal(float x, float z, int species)
    {
        Color coat = species switch { 0 => new(208, 175, 101, 255), 1 => Steel, 2 => White, _ => new(63, 75, 72, 255) };
        float body = species == 1 ? .20f : .12f, leg = species == 0 ? .22f : .08f;
        foreach (int sx in new[] { -1, 1 }) foreach (int sz in new[] { -1, 1 })
            Box(x + sx * .055f, .12f, z + sz * .09f, .025f, leg, .025f, coat);
        Box(x, .12f + leg, z, body, .12f, .27f, coat);
        Box(x, .19f + leg, z - .105f, .065f, species == 0 ? .27f : .08f, .07f, coat);
        Box(x, .23f + leg + (species == 0 ? .21f : 0), z - .145f, .09f, .07f, .12f, coat);
        if (species == 1) { Box(x, .15f, z - .22f, .04f, .16f, .04f, coat); Box(x, .30f, z - .13f, .29f, .09f, .025f, coat); }
        if (species == 2) for (int i = -1; i <= 1; i++) Box(x, .25f, z + i * .07f, .125f, .025f, .022f, Steel);
        if (species == 0) for (int i = -1; i <= 1; i++) Box(x + .062f, .35f, z + i * .07f, .008f, .055f, .035f, Wood);
    }
    private static void Habitat(float x, float z, float w, float d, int species, bool wet = false)
    {
        Box(x, .105f, z, w, .014f, d, species == 0 ? new(188, 188, 130, 255) : Green);
        foreach (int side in new[] { -1, 1 })
        {
            Box(x, .12f, z + side * d / 2, w, .09f, .04f, Wood);
            Box(x + side * w / 2, .12f, z, .04f, .09f, d, Wood);
            for (float p = -w / 2; p <= w / 2; p += .3f) Box(x + p, .12f, z + side * d / 2, .022f, .24f, .022f, Steel);
            Box(x, .34f, z + side * d / 2, w, .02f, .02f, Steel);
        }
        if (wet) Pool(x + w * .22f, z - d * .15f, Math.Min(w, d) * .24f);
        else Tree(x + w * .28f, z - d * .26f);
        Gable(x - w * .28f, z - d * .28f, .38f, .35f, .25f, Wood, .13f);
        Animal(x - .22f, z + .17f, species); Animal(x + .17f, z + .30f, species);
    }
    internal static void Draw(float x, float z, Tile tile)
    {
        Begin(x, z, tile); Color c = Accent(tile.LandmarkStyle);
        // Layouts have different enclosure footprints, pavilions and paths, not just colors.
        switch (tile.LandmarkStyle)
        {
            case 0: Habitat(-1.35f, -.9f, 2.2f, 3.4f, 1, true); Habitat(1.35f, -1.7f, 2.1f, 1.8f, 3, true); Habitat(1.35f, .5f, 2.1f, 1.9f, 2); break;
            case 1: Habitat(0, -1.55f, 5.2f, 2.1f, 3, true); Habitat(-1.45f, .8f, 2.0f, 1.6f, 1); Habitat(1.2f, .8f, 2.3f, 1.6f, 0); break;
            case 2: Dome(-1.6f, -1.3f, 1.7f, 2.3f, .55f, c); Habitat(1.0f, -.7f, 2.8f, 3.6f, 3, true); Habitat(-1.6f, 1.05f, 1.7f, 1.1f, 2); break;
            case 3: Habitat(-1.6f, -.7f, 1.8f, 3.9f, 0); Pool(.85f, -1.4f, .9f); Habitat(1.1f, .9f, 2.7f, 1.6f, 3, true); break;
            case 4: Habitat(0, -.8f, 4.8f, 3.4f, 0); Tree(-2.55f, 1.6f); Tree(2.55f, 1.6f); break;
            case 5: Habitat(-1.4f, -1.55f, 2.1f, 2.0f, 0); Habitat(1.35f, -1.55f, 2.1f, 2.0f, 2); Habitat(0, .9f, 4.8f, 1.65f, 1); break;
            case 6: for (int i = -1; i <= 1; i++) Habitat(i * 1.85f, -.9f, 1.5f, 3.2f, (i + 1) % 3); break;
            case 7: Habitat(-1.4f, -.6f, 2.0f, 3.7f, 1); Dome(1.25f, -1.3f, 2.1f, 2.5f, .6f, c); Tree(1.3f, .7f); break;
            case 8: Hall(0, -2.05f, 4.8f, .9f, .6f, c); Habitat(-1.4f, .15f, 2.1f, 2.5f, 0); Habitat(1.35f, .15f, 2.1f, 2.5f, 1); break;
            case 9: Dome(0, -1.55f, 3.5f, 2.0f, .8f, c); Habitat(-1.4f, .9f, 2.1f, 1.7f, 3, true); Habitat(1.35f, .9f, 2.1f, 1.7f, 2); break;
            case 10: for (int i = -1; i <= 1; i++) { Habitat(i * 1.85f, -1.8f, 1.5f, 1.5f, i + 1); Habitat(i * 1.85f, .2f, 1.5f, 1.5f, 2 - i); } break;
            case 11: Habitat(-1.45f, -.8f, 2.0f, 3.5f, 0); Hall(1.2f, -1.8f, 2.2f, 1.3f, .85f, c); Habitat(1.2f, .65f, 2.2f, 1.8f, 1); break;
            case 12: Habitat(0, -1.65f, 4.9f, 1.8f, 2); Hall(-1.6f, .6f, 1.45f, 1.5f, .6f, c); Habitat(1.15f, .6f, 2.7f, 1.5f, 0); break;
            case 13: Gable(-1.3f, -1.5f, 2.2f, 1.9f, .6f, c); Solar(-2.15f, 1.17f, -1.5f, 6); Habitat(1.45f, -.6f, 1.8f, 3.5f, 1); Habitat(-1.3f, 1.05f, 2.2f, 1.1f, 3, true); break;
            case 14: Habitat(0, -1.65f, 4.9f, 1.8f, 0); Habitat(0, .7f, 3.3f, 1.8f, 1); Hall(-2.25f, .7f, .65f, 1.5f, .45f, c); Hall(2.25f, .7f, .65f, 1.5f, .45f, c); break;
            case 15: Dome(0, -1.8f, 2.2f, 1.5f, .65f, c); Habitat(-1.9f, -.3f, 1.3f, 3.8f, 2); Habitat(1.9f, -.3f, 1.3f, 3.8f, 0); Pool(0, .45f, .65f); break;
        }
        Hall(-1.7f, 2.25f, .85f, .55f, .38f, c); Hall(1.7f, 2.25f, .85f, .55f, .38f, c);
        Arcade(0, 2.45f, 1.2f, c);
        Rlgl.PopMatrix();
    }
}
