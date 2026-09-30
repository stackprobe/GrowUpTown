using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// Decorative traffic only. No world-wide routes, persistent simulation or save data.
internal sealed class TrafficRenderer
{
    internal const int MaxCars = 1024;
    private const double SpawnInterval = 18;
    private const float Lifetime = 10, TilesPerSecond = .7f;
    private static readonly Vector2[] Arms = [new(0, 1), new(1, 0), new(0, -1), new(-1, 0)];
    private static readonly Color[] Colors = [new(239, 245, 236, 255), new(104, 189, 218, 255), new(233, 155, 99, 255), new(179, 226, 131, 255)];
    private Dictionary<(int X, int Z), int> roads = new(), previousRoads = new();
    private readonly List<Car> cars = new(MaxCars);
    private City? previousCity;
    private double previousTime;
    internal int Count => cars.Count;
    internal IEnumerable<(int X, int Z)> Positions => cars.Select(car => (car.X, car.Z));

    private struct Car
    {
        internal int X, Z, Entry, Exit, ColorIndex;
        internal uint Seed;
        internal float Progress, Age;
    }

    private static uint Hash(int x, int z) => unchecked((uint)x * 73856093u ^ (uint)z * 19349663u) * 2654435761u;

    internal static int ChooseExit(int mask, int entry, uint seed)
    {
        int choices = entry < 0 ? mask : mask & ~(1 << entry);
        if (choices == 0) return entry; // A dead end turns back in its own lane.
        int choice = (int)(seed % (uint)BitOperations.PopCount((uint)choices));
        for (int arm = 0; arm < 4; arm++)
            if ((choices & (1 << arm)) != 0 && choice-- == 0) return arm;
        return entry;
    }

    internal static (Vector2 Position, Vector2 Heading) Pose(int entry, int exit, float t)
    {
        static Vector2 Right(Vector2 v) => new(-v.Y, v.X);
        Vector2 incoming = -Arms[entry], outgoing = Arms[exit];
        Vector2 a = Arms[entry] * .5f + Right(incoming) * .16f;
        Vector2 d = Arms[exit] * .5f + Right(outgoing) * .16f;
        Vector2 b = a + incoming * .43f, c = d - outgoing * .43f;
        float s = 1 - t;
        return (s * s * s * a + 3 * s * s * t * b + 3 * s * t * t * c + t * t * t * d,
            Vector2.Normalize(3 * s * s * (b - a) + 6 * s * t * (c - b) + 3 * t * t * (d - c)));
    }

    internal void Update(City city, IReadOnlyList<WorldChunk> chunks, int minX, int maxX, int minZ, int maxZ, double time)
    {
        if (previousCity != city)
        {
            cars.Clear(); roads.Clear(); previousRoads.Clear();
            previousCity = city; previousTime = time;
        }
        float dt = (float)Math.Clamp(time - previousTime, 0, .4);
        (roads, previousRoads) = (previousRoads, roads);
        roads.Clear();
        foreach (var chunk in chunks) foreach (var (index, tile) in chunk.Cells)
        {
            if (tile.Kind != TileKind.Road) continue;
            int x = chunk.Position.OriginX + index % City.Size, z = chunk.Position.OriginZ + index / City.Size;
            if (x < minX || x > maxX || z < minZ || z > maxZ) continue;
            roads[(x, z)] = city.RoadMask(x, z);
        }
        for (int i = cars.Count - 1; i >= 0; i--)
        {
            Car car = cars[i];
            car.Age += dt;
            bool valid = roads.TryGetValue((car.X, car.Z), out int mask);
            // Remove cars immediately when their tile or selected connection is bulldozed.
            valid &= mask == 0 || ((mask & (1 << car.Entry)) != 0 && (mask & (1 << car.Exit)) != 0);
            if (valid && mask != 0)
            {
                car.Progress += dt * TilesPerSecond;
                if (car.Progress >= 1)
                {
                    car.X += (int)Arms[car.Exit].X; car.Z += (int)Arms[car.Exit].Y;
                    car.Entry = (car.Exit + 2) % 4;
                    valid = roads.TryGetValue((car.X, car.Z), out int nextMask);
                    car.Exit = ChooseExit(nextMask, car.Entry, ++car.Seed);
                    car.Progress -= 1;
                }
            }
            if (!valid || car.Age >= Lifetime)
            {
                cars[i] = cars[^1]; cars.RemoveAt(cars.Count - 1);
            }
            else cars[i] = car;
        }
        foreach (var (cell, mask) in roads)
        {
            if (cars.Count >= MaxCars) break;
            uint seed = Hash(cell.X, cell.Z);
            double offset = seed % 1800 / 100.0;
            bool newlyVisible = !previousRoads.TryGetValue(cell, out int oldMask) || oldMask != mask;
            bool spawn = newlyVisible ? seed % 3 == 0
                : Math.Floor((time + offset) / SpawnInterval) > Math.Floor((previousTime + offset) / SpawnInterval);
            if (!spawn) continue;
            int entry = ChooseExit(mask, -1, seed);
            if (mask == 0) entry = 0;
            cars.Add(new Car { X = cell.X, Z = cell.Z, Entry = entry, Exit = ChooseExit(mask, entry, seed >> 8),
                Seed = seed, ColorIndex = (int)(seed % (uint)Colors.Length), Progress = (seed % 100) / 100f });
        }
        previousTime = time;
    }

    internal void Draw(int originX, int originZ)
    {
        foreach (var car in cars)
        {
            var (position, heading) = Pose(car.Entry, car.Exit, car.Progress);
            if (roads[(car.X, car.Z)] == 0) { position = Vector2.Zero; heading = Vector2.UnitX; }
            float scale = Math.Min(1, Math.Min(car.Age / .25f, (Lifetime - car.Age) / .35f));
            Rlgl.PushMatrix();
            Rlgl.Translatef(car.X - originX + position.X, RoadRenderer.SurfaceHeight + .015f, car.Z - originZ + position.Y);
            Rlgl.Rotatef(-MathF.Atan2(heading.Y, heading.X) * 180 / MathF.PI, 0, 1, 0);
            Rlgl.Scalef(scale, scale, scale);
            DrawCube(new(0, .07f, 0), .24f, .14f, .13f, Colors[car.ColorIndex]);
            DrawCube(new(-.015f, .155f, 0), .11f, .045f, .105f, new(72, 103, 117, 255));
            Rlgl.PopMatrix();
        }
    }
}
