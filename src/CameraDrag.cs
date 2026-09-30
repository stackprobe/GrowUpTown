using System.Numerics;

namespace GrowUpTown;

internal sealed class CameraDrag
{
    internal bool Active { get; private set; }
    internal bool Panning { get; private set; }
    internal void Update(bool down, bool pressed, bool focused, bool overWorld, bool shift)
    {
        if (!down || !focused) { Cancel(); return; }
        if (pressed && overWorld) { Active = true; Panning = shift; }
    }
    internal void Cancel() { Active = false; Panning = false; }
    internal static Vector3 Pan(Vector2 delta, float angle, float pitch, float zoom, int screenHeight)
    {
        Vector3 right = new(MathF.Cos(angle), 0, -MathF.Sin(angle));
        Vector3 forward = new(-MathF.Sin(angle), 0, -MathF.Cos(angle));
        return (-right * delta.X + forward * (delta.Y / MathF.Sin(pitch))) * zoom / screenHeight;
    }
}
