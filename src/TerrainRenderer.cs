using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

internal readonly record struct GroundSpan(int X, int Z, int Length);
internal sealed class TerrainGeometry
{
    internal readonly List<GroundSpan> Land = [];
    internal readonly List<SurfaceQuad> Water = [];
    internal readonly List<SurfaceQuad> Skirts = [];
    internal static TerrainGeometry Build(City city, WorldChunk chunk)
    {
        var result = new TerrainGeometry();
        int ox = chunk.Position.OriginX, oz = chunk.Position.OriginZ;
        for (int z = 0; z < City.Size; z++)
        {
            int start = -1;
            for (int x = 0; x <= City.Size; x++)
            {
                bool water = x < City.Size && city.HasWater(ox + x, oz + z);
                if (x < City.Size && !water) { if (start < 0) start = x; }
                else if (start >= 0) { result.Land.Add(new(start, z, x - start)); start = -1; }
                if (water) WaterRenderer.Append(result.Water, city, ox + x, oz + z, x, z);
            }
        }
        // Close only the outside of purchased land; never put walls at district seams.
        void Wall(Vector3 a, Vector3 b) => result.Skirts.Add(new(a, a - new Vector3(0, .24f, 0),
            b - new Vector3(0, .24f, 0), b, new(122, 153, 122, 255)));
        if (!city.Chunks.ContainsKey(new(chunk.Position.X - 1, chunk.Position.Z))) Wall(new(-.5f, 0, -.5f), new(-.5f, 0, 23.5f));
        if (!city.Chunks.ContainsKey(new(chunk.Position.X + 1, chunk.Position.Z))) Wall(new(23.5f, 0, 23.5f), new(23.5f, 0, -.5f));
        if (!city.Chunks.ContainsKey(new(chunk.Position.X, chunk.Position.Z - 1))) Wall(new(23.5f, 0, -.5f), new(-.5f, 0, -.5f));
        if (!city.Chunks.ContainsKey(new(chunk.Position.X, chunk.Position.Z + 1))) Wall(new(-.5f, 0, 23.5f), new(23.5f, 0, 23.5f));
        return result;
    }
}
internal sealed class TerrainRenderer : IDisposable
{
    private readonly Texture2D texture;
    private readonly Shader waterShader;
    private readonly int wavePhaseLocation;
    private readonly Dictionary<WorldChunk, (long Revision, TerrainGeometry Geometry)> cache = [];
    internal TerrainRenderer()
    {
        var image = GenImageChecked(96, 96, 4, 4, new Color(169, 196, 147, 255), new Color(164, 190, 140, 255));
        texture = LoadTextureFromImage(image); UnloadImage(image);
        SetTextureFilter(texture, TextureFilter.Point);
        waterShader = LoadShaderFromMemory(null, WaterRenderer.FragmentShader);
        wavePhaseLocation = GetShaderLocation(waterShader, "wavePhase");
    }
    internal void Draw(City city, IReadOnlyList<WorldChunk> chunks, int originX, int originZ)
    {
        // Keep only visible geometry. Revisiting cold districts regenerates it once.
        foreach (var key in cache.Keys.Where(key => !chunks.Contains(key)).ToArray()) cache.Remove(key);
        foreach (var chunk in chunks)
        {
            if (!cache.TryGetValue(chunk, out var entry) || entry.Revision != chunk.TerrainRevision)
                cache[chunk] = (chunk.TerrainRevision, TerrainGeometry.Build(city, chunk));
            float x = chunk.Position.OriginX - originX + 11.5f, z = chunk.Position.OriginZ - originZ + 11.5f;
            DrawCube(new(x, -.355f, z), 24, .23f, 24, new(122, 153, 122, 255));
        }
        Rlgl.SetTexture(texture.Id); Rlgl.Begin(DrawMode.Quads); Rlgl.Color4ub(255, 255, 255, 255);
        foreach (var chunk in chunks)
        {
            float ox = chunk.Position.OriginX - originX, oz = chunk.Position.OriginZ - originZ;
            foreach (var span in cache[chunk].Geometry.Land)
            {
                float x = span.X - .5f, z = span.Z - .5f;
                float u0 = span.X / 24f, u1 = (span.X + span.Length) / 24f;
                float v0 = span.Z / 24f, v1 = (span.Z + 1) / 24f;
                Rlgl.TexCoord2f(u0, v0); Rlgl.Vertex3f(ox + x, 0, oz + z);
                Rlgl.TexCoord2f(u0, v1); Rlgl.Vertex3f(ox + x, 0, oz + z + 1);
                Rlgl.TexCoord2f(u1, v1); Rlgl.Vertex3f(ox + x + span.Length, 0, oz + z + 1);
                Rlgl.TexCoord2f(u1, v0); Rlgl.Vertex3f(ox + x + span.Length, 0, oz + z);
            }
        }
        Rlgl.End(); Rlgl.SetTexture(0);
        DrawSurfaces(chunks, originX, originZ, false);
    }
    internal void DrawWater(IReadOnlyList<WorldChunk> chunks, int originX, int originZ, double time)
    {
        float phase = WaterRenderer.WavePhase(time);
        SetShaderValue(waterShader, wavePhaseLocation, phase, ShaderUniformDataType.Float);
        BeginShaderMode(waterShader);
        DrawSurfaces(chunks, originX, originZ, true);
        EndShaderMode();
    }
    private void DrawSurfaces(IReadOnlyList<WorldChunk> chunks, int originX, int originZ, bool water)
    {
        Rlgl.SetTexture(0); Rlgl.Begin(DrawMode.Quads);
        foreach (var chunk in chunks)
        {
            Vector3 offset = new(chunk.Position.OriginX - originX, 0, chunk.Position.OriginZ - originZ);
            int waveX = WaterRenderer.WaveOrigin(chunk.Position.OriginX), waveZ = WaterRenderer.WaveOrigin(chunk.Position.OriginZ);
            foreach (var q in water ? cache[chunk].Geometry.Water : cache[chunk].Geometry.Skirts)
            {
                Rlgl.Color4ub(q.Color.R, q.Color.G, q.Color.B, q.Color.A);
                void Vertex(Vector3 p)
                {
                    if (water) Rlgl.TexCoord2f(q.AnimatedWater ? waveX + p.X : -10000, waveZ + p.Z);
                    p += offset; Rlgl.Vertex3f(p.X, p.Y, p.Z);
                }
                Vertex(q.A); Vertex(q.B); Vertex(q.C); Vertex(q.D);
            }
        }
        Rlgl.End();
    }
    public void Dispose() { cache.Clear(); UnloadShader(waterShader); UnloadTexture(texture); }
}
