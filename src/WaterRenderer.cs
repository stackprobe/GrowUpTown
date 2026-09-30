using System.Numerics;
using Raylib_cs;

namespace GrowUpTown;

internal readonly record struct SurfaceQuad(Vector3 A, Vector3 B, Vector3 C, Vector3 D, Color Color, bool AnimatedWater = false);
internal static class WaterRenderer
{
    internal const float WaterHeight = -.16f;
    private const float Inset = .14f;
    // GPU-only color animation: one quad per tile, no animated mesh or texture uploads.
    // Spatial frequencies repeat every eight tiles; modulo coordinates retain precision
    // at distant world origins and match on both sides of chunk boundaries.
    internal static int WaveOrigin(int coordinate) => ((coordinate % 256) + 256) % 256;
    internal static float WavePhase(double time) => (float)((time * .5) % (Math.PI * 2));
    internal const string FragmentShader = """
        #version 330
        in vec2 fragTexCoord;
        in vec4 fragColor;
        out vec4 finalColor;
        uniform float wavePhase;
        void main()
        {
            // Banks share the batch but keep their original dry-land colors.
            if (fragTexCoord.x < -1000.0) { finalColor = fragColor; return; }
            vec2 p = fragTexCoord;
            float bend = sin(6.2831853 * (p.x * 0.125 + p.y * 0.25) - wavePhase);
            float swell = sin(6.2831853 * (p.x * 0.25 + p.y * 0.125) - wavePhase);
            float ripple = sin(6.2831853 * (p.y * 1.5 + p.x * 0.25) + bend * 0.65 - wavePhase * 3.0);
            float broken = sin(6.2831853 * (p.x * 0.5 - p.y * 0.125) - wavePhase);
            // Broad soft swells and short bright crests travel across the whole surface.
            float crest = smoothstep(0.86, 0.99, ripple) * smoothstep(-0.25, 0.65, broken);
            vec3 water = fragColor.rgb + vec3(0.025, 0.04, 0.035) * swell;
            water = mix(water, vec3(0.57, 0.81, 0.85), crest * 0.6);
            finalColor = vec4(water, 1.0);
        }
        """;
    internal static void Append(List<SurfaceQuad> quads, City city, int wx, int wz, int x, int z)
    {
        float left = x - .5f, top = z - .5f;
        Vector3 Point(float u, float v, float y) => new(left + u, y, top + v);
        quads.Add(new(Point(0, 0, WaterHeight), Point(0, 1, WaterHeight),
            Point(1, 1, WaterHeight), Point(1, 0, WaterHeight), new(76, 161, 188, 255), true));
        float[] grid = [0, Inset, 1 - Inset, 1];
        var dry = new List<(int X, int Z)>();
        for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
            if ((dx != 0 || dz != 0) && !city.HasWater(wx + dx, wz + dz)) dry.Add((dx, dz));
        Vector3 Shore(float u, float v)
        {
            float distance = Inset;
            foreach (var (dx, dz) in dry)
            {
                float sx = Math.Max(Math.Max(dx - u, 0), u - (dx + 1));
                float sz = Math.Max(Math.Max(dz - v, 0), v - (dz + 1));
                distance = Math.Min(distance, MathF.Sqrt(sx * sx + sz * sz));
            }
            return Point(u, v, distance >= Inset - .00001f ? WaterHeight : WaterHeight * distance / Inset);
        }
        // Shared edge samples measure the same distance to land on both tiles.
        for (int iz = 0; iz < 3; iz++) for (int ix = 0; ix < 3; ix++)
        {
            var a = Shore(grid[ix], grid[iz]); var b = Shore(grid[ix], grid[iz + 1]);
            var c = Shore(grid[ix + 1], grid[iz + 1]); var d = Shore(grid[ix + 1], grid[iz]);
            if (a.Y <= WaterHeight && b.Y <= WaterHeight && c.Y <= WaterHeight && d.Y <= WaterHeight) continue;
            quads.Add(new(a, b, c, d, ix == 0 || iz == 0 ? new(168, 180, 133, 255) : new(188, 196, 151, 255)));
        }
    }
}
