namespace GrowUpTown;

// Derived only from saved facade IDs: independent of camera, time and later growth.
internal static class UltraAppearance
{
    internal static int Signature(Tile tile) => tile.FaceStyles[0] + 3 * tile.FaceStyles[1]
        + 5 * tile.FaceStyles[2] + 7 * tile.FaceStyles[3];
    internal static int RoofVariant(Tile tile) => Signature(tile) % 4;
    internal static int Category(Tile tile, int face, int panel) => (Signature(tile) + face + panel) % 3;
    internal static int Style(Tile tile, int floor, int face, int panel) =>
        (tile.FaceStyles[floor * 4 + face] + panel * 5) % FacadeStyles.Count;
    internal static float PanelWidth(Tile tile, int face, int panel) =>
        panel == (Signature(tile) + face) % 3 ? .96f : .68f;
}
