using System.Numerics;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace GrowUpTown;

// Visible-area animation, like cars. Town connectivity is calculated independently.
internal sealed class TrainTraffic
{
    internal const int MaxTrains = 64;
    internal const double SpawnInterval = 2;
    internal const float Speed = .65f, StationWait = 3;
    internal const int CarCount = 3;
    internal const float CarSpacing = .55f, RearOffset = (CarCount - 1) * CarSpacing, ReverseWait = 2;
    private static readonly Vector2[] Arms = [new(0, 1), new(1, 0), new(0, -1), new(-1, 0)];
    private readonly Dictionary<(int X, int Z), int> tracks = [];
    private readonly List<Train> trains = [];
    private readonly Dictionary<(int X, int Z), Gate> gates = [];
    private City? previousCity;
    private double previousTime;
    private double nextSpawn;
    internal readonly record struct Leg(int X, int Z, int Entry, int Exit, bool ReverseStub = false)
    {
        internal float Length => Entry == Exit ? .32f : (Entry + 2) % 4 != Exit ? MathF.PI / 4 : 1;
    }
    private sealed class Train
    {
        internal List<Leg> Route = [];
        internal int Index;
        internal float Progress, Wait;
        internal uint Seed;
        internal bool Reversing, Reversed;
        internal (int X, int Z)? LastStation;
    }
    private sealed class Gate
    {
        internal float Warning, Closed;
        internal bool Requested;
    }
    internal int Count => trains.Count;
    internal IEnumerable<(int X, int Z, float Progress, float Wait)> Positions => trains.Select(t =>
        (t.Route[t.Index].X, t.Route[t.Index].Z, t.Progress, t.Wait));
    internal IEnumerable<(Vector2 Position, Vector2 Heading)[]> Carriages => trains.Select(t =>
        Enumerable.Range(0, CarCount).Select(car => CarPose(t, car)).ToArray());
    internal float Closure(int x, int z) => gates.TryGetValue((x, z), out var g) ? g.Closed : 0;
    internal bool StopCars(int x, int z) => gates.TryGetValue((x, z), out var g) && (g.Requested || g.Closed > 0);
    internal bool LampOn(int x, int z, int lamp, double time) => StopCars(x, z) && ((int)(time * 4) + lamp) % 2 == 0;
    internal static int ChooseExit(int mask, int entry, uint seed) => mask == 15 ? (entry + 2) % 4 : TrafficRenderer.ChooseExit(mask, entry, seed);
    internal static (Vector2 Position, Vector2 Heading) Pose(int entry, int exit, float t)
    {
        if (entry == exit)
            return (Arms[entry] * (.5f - .32f * t), -Arms[entry]);
        if ((entry + 2) % 4 == exit) return (Vector2.Lerp(Arms[entry] * .5f, Arms[exit] * .5f, t), Arms[exit]);
        Vector2 center = (Arms[entry] + Arms[exit]) * .5f;
        Vector2 a = -Arms[exit] * .5f, b = -Arms[entry] * .5f;
        float angle = t * MathF.PI / 2;
        return (center + a * MathF.Cos(angle) + b * MathF.Sin(angle),
            Vector2.Normalize(-a * MathF.Sin(angle) + b * MathF.Cos(angle)));
    }
    private static (Vector2 Position, Vector2 Heading) LegPose(Leg leg, float progress)
    {
        var pose = Pose(leg.Entry, leg.Exit, leg.ReverseStub ? 1 - progress : progress);
        return (pose.Position, leg.ReverseStub ? -pose.Heading : pose.Heading);
    }
    private static (int Index, float Progress) Behind(Train t, float distance)
    {
        int index = t.Index;
        float along = t.Progress * t.Route[index].Length - distance;
        while (along < 0 && index > 0) along += t.Route[--index].Length;
        return (index, Math.Max(0, along) / t.Route[index].Length);
    }
    private static (Vector2 Position, Vector2 Heading) CarPose(Train t, int car)
    {
        var sample = Behind(t, car * CarSpacing); var leg = t.Route[sample.Index];
        var pose = LegPose(leg, sample.Progress);
        return (new Vector2(leg.X, leg.Z) + pose.Position, t.Reversed ? -pose.Heading : pose.Heading);
    }
    private void Reverse(Train t)
    {
        // The previous rear cab becomes the leading cab. Reversing the travelled
        // path keeps every carriage in place, including on curved approaches.
        var rear = Behind(t, RearOffset); int oldIndex = t.Index;
        t.Route = t.Route.Take(oldIndex + 1).Reverse().Select(leg => new Leg(leg.X, leg.Z,
            leg.Exit, leg.Entry, leg.Entry == leg.Exit && !leg.ReverseStub)).ToList();
        t.Index = oldIndex - rear.Index; t.Progress = 1 - rear.Progress;
        t.Reversed = !t.Reversed; t.Reversing = false; t.LastStation = null;
        Extend(t);
    }
    private bool Valid(Leg leg, bool travelled = false) => tracks.TryGetValue((leg.X, leg.Z), out int mask)
        && (mask & (1 << leg.Entry)) != 0 && (mask & (1 << leg.Exit)) != 0
        && (leg.Entry != leg.Exit || travelled || BitOperations.PopCount((uint)mask) == 1)
        && (mask != 15 || leg.Exit == (leg.Entry + 2) % 4 || travelled && leg.Entry == leg.Exit);
    private bool ReconcileRoute(Train t)
    {
        // Edits ahead of or behind the train must not replace an intact train.
        // Only removed track under a carriage invalidates the physical train.
        int first = Behind(t, RearOffset + .24f).Index;
        if (first > 0) { t.Route.RemoveRange(0, first); t.Index -= first; }
        for (int i = 0; i <= t.Index; i++) if (!Valid(t.Route[i], travelled: true)) return false;
        for (int i = t.Index + 1; i < t.Route.Count; i++)
            if (!Valid(t.Route[i])) { t.Route.RemoveRange(i, t.Route.Count - i); break; }
        Extend(t);
        return true;
    }
    private void Extend(Train t)
    {
        while (t.Route.Count < t.Index + 5)
        {
            var last = t.Route[^1];
            if (last.Entry == last.Exit && !last.ReverseStub) break;
            var arm = Arms[last.Exit];
            int x = last.X + (int)arm.X, z = last.Z + (int)arm.Y, entry = (last.Exit + 2) % 4;
            if (!tracks.TryGetValue((x, z), out int mask)) break;
            t.Route.Add(new(x, z, entry, ChooseExit(mask, entry, ++t.Seed)));
        }
    }
    internal void AddTrain(int x, int z, int entry, uint seed = 1)
    {
        if (!tracks.TryGetValue((x, z), out int mask) || (mask & (1 << entry)) == 0) return;
        var t = new Train { Seed = seed };
        t.Route.Add(new(x, z, entry, ChooseExit(mask, entry, seed)));
        // Seed the already-travelled path so all three carriages fit at spawn.
        float history = 0;
        while (history < RearOffset)
        {
            var first = t.Route[0]; var arm = Arms[first.Entry];
            int px = first.X + (int)arm.X, pz = first.Z + (int)arm.Y, exit = (first.Entry + 2) % 4;
            if (!tracks.TryGetValue((px, pz), out int previousMask)) return;
            int previousEntry = ChooseExit(previousMask, exit, ++t.Seed);
            var previous = new Leg(px, pz, previousEntry, exit, previousEntry == exit);
            t.Route.Insert(0, previous); t.Index++; history += previous.Length;
            if (previous.Entry == previous.Exit && history < RearOffset) return;
        }
        Extend(t); trains.Add(t);
    }
    private static (int X, int Z)? StationAt(City city, Leg leg)
    {
        if (leg.Exit != (leg.Entry + 2) % 4) return null;
        foreach (var n in city.Neighbors(leg.X, leg.Z))
        {
            if (city[n.x, n.z].Kind != TileKind.Station) continue;
            var anchor = city.AnchorAt(n.x, n.z); var station = city[anchor.X, anchor.Z];
            if (leg.Exit % 2 == station.StationRotation % 2) continue;
            int second = leg.Exit < 2 ? 1 : 0;
            if (City.StationSideCell(anchor.X, anchor.Z, station.StationRotation, second) == (leg.X, leg.Z)) return anchor;
        }
        return null;
    }
    internal void Update(City city, IReadOnlyList<WorldChunk> chunks, int minX, int maxX, int minZ, int maxZ,
        double time, Func<int, int, bool>? carInside = null, bool spawn = true, Func<Vector2, bool>? onScreen = null)
    {
        if (previousCity != city || time < previousTime)
        {
            trains.Clear(); gates.Clear(); previousCity = city; previousTime = time; nextSpawn = time + SpawnInterval;
        }
        float dt = (float)Math.Clamp(time - previousTime, 0, .4); previousTime = time;
        tracks.Clear();
        foreach (var chunk in chunks) foreach (var (index, tile) in chunk.Cells)
        {
            if (!City.IsRail(tile)) continue;
            int x = chunk.Position.OriginX + index % City.Size, z = chunk.Position.OriginZ + index / City.Size;
            if (x >= minX && x <= maxX && z >= minZ && z <= maxZ) tracks[(x, z)] = city.RailMask(x, z);
        }
        trains.RemoveAll(t => !ReconcileRoute(t));
        if (spawn && dt > 0 && time >= nextSpawn)
        {
            // A fixed timer, independent of map edits. Prefer actual screen
            // positions over the much wider bounds used to cull tall buildings.
            // Replace unseen trains only while the screen has no visible train.
            bool Visible(Train t) => onScreen is null || Enumerable.Range(0, CarCount).Any(car => onScreen(CarPose(t, car).Position));
            if (!trains.Any(Visible)) trains.RemoveAll(t => !Visible(t));
            // One train per visible rail network avoids head-on encounters on
            // single track. Diamond crossings still have two separate routes.
            var occupied = new HashSet<(int X, int Z, int Lane)>();
            void MarkNetwork(int x, int z, int entry)
            {
                var queue = new Queue<(int X, int Z, int Lane)>();
                queue.Enqueue((x, z, tracks[(x, z)] == 15 ? entry % 2 : 0));
                while (queue.TryDequeue(out var p))
                {
                    if (!occupied.Add(p)) continue;
                    int mask = tracks[(p.X, p.Z)];
                    for (int side = 0; side < 4; side++)
                    {
                        if ((mask & (1 << side)) == 0 || (mask == 15 && side % 2 != p.Lane)) continue;
                        int nx = p.X + (int)Arms[side].X, nz = p.Z + (int)Arms[side].Y;
                        if (tracks.TryGetValue((nx, nz), out int nextMask)) queue.Enqueue((nx, nz, nextMask == 15 ? side % 2 : 0));
                    }
                }
            }
            foreach (var t in trains) { var p = t.Route[t.Index]; MarkNetwork(p.X, p.Z, p.Entry); }
            IEnumerable<KeyValuePair<(int X, int Z), int>> candidates = tracks;
            if (onScreen is not null)
                candidates = candidates.OrderByDescending(candidate =>
                {
                    int score = 0;
                    foreach (var arm in Arms) for (int step = 1; step <= 3; step++)
                    {
                        var p = (X: candidate.Key.X + (int)arm.X * step, Z: candidate.Key.Z + (int)arm.Y * step);
                        if (!tracks.ContainsKey(p) || !onScreen(new(p.X, p.Z))) break;
                        score++;
                    }
                    return score;
                });
            foreach (var (p, mask) in candidates)
            {
                if (trains.Count >= MaxTrains) break;
                if (mask == 0 || city[p.X, p.Z].RailCrossing || onScreen?.Invoke(new(p.X, p.Z)) == false) continue;
                uint seed = unchecked((uint)p.X * 73856093u ^ (uint)p.Z * 19349663u);
                // Try every incoming direction; a hashed choice alone can fail
                // to fit three cars even when the opposite direction fits.
                int start = (int)(seed % 4);
                for (int attempt = 0; attempt < 4; attempt++)
                {
                    int entry = (start + attempt) % 4;
                    if ((mask & (1 << entry)) == 0 || occupied.Contains((p.X, p.Z, mask == 15 ? entry % 2 : 0))) continue;
                    int before = trains.Count; AddTrain(p.X, p.Z, entry, seed);
                    if (trains.Count > before)
                    {
                        var added = trains[^1]; int rear = Behind(added, RearOffset + .24f).Index;
                        bool blocked = added.Route.Skip(rear).Take(added.Index - rear + 1).Any(leg =>
                            city[leg.X, leg.Z].RailCrossing || trains.Any(other => other != added && Occupies(other, leg.X, leg.Z)));
                        // Never materialize carriages over an open crossing or
                        // an occupied diamond. A later candidate can still fit.
                        if (blocked) { trains.RemoveAt(trains.Count - 1); continue; }
                        MarkNetwork(p.X, p.Z, entry);
                        if (mask != 15 || trains.Count >= MaxTrains) break;
                    }
                }
            }
            nextSpawn = time + SpawnInterval;
        }
        foreach (var key in gates.Keys.Where(p => !tracks.ContainsKey(p) || !city[p.X, p.Z].RailCrossing).ToArray()) gates.Remove(key);
        foreach (var g in gates.Values) g.Requested = false;
        foreach (var t in trains)
        {
            Extend(t);
            // Reserve the planned route ahead, and hold until the rear carriage clears.
            float distance = -t.Progress * t.Route[t.Index].Length;
            for (int i = t.Index; i < t.Route.Count && distance <= 2.5f; i++)
            {
                var leg = t.Route[i];
                if (city[leg.X, leg.Z].RailCrossing) Request(leg);
                distance += leg.Length;
            }
            float behind = t.Progress * t.Route[t.Index].Length;
            for (int i = t.Index - 1; i >= 0 && behind < RearOffset + .43f; i--)
            {
                var leg = t.Route[i]; if (city[leg.X, leg.Z].RailCrossing) Request(leg);
                behind += leg.Length;
            }
        }
        void Request(Leg leg)
        {
            if (!gates.TryGetValue((leg.X, leg.Z), out var g)) gates[(leg.X, leg.Z)] = g = new();
            g.Requested = true;
        }
        foreach (var g in gates.Values)
        {
            g.Warning = g.Requested ? g.Warning + dt : 0;
            g.Closed = Math.Clamp(g.Closed + (g.Requested ? g.Warning >= .7f ? dt / .8f : 0 : -dt / 1.1f), 0, 1);
        }
        foreach (var t in trains)
        {
            var leg = t.Route[t.Index];
            if (t.Wait > 0)
            {
                t.Wait = Math.Max(0, t.Wait - dt);
                if (t.Wait == 0 && t.Reversing) Reverse(t);
                continue;
            }
            float next = t.Progress + dt * Speed / leg.Length;
            var station = StationAt(city, leg);
            if (station is null) t.LastStation = null;
            const float stop = RearOffset / 2;
            if (station is not null && station != t.LastStation && t.Progress < stop && next >= stop)
            {
                t.Progress = stop; t.Wait = StationWait; t.LastStation = station; continue;
            }
            if (t.Index + 1 < t.Route.Count)
            {
                var ahead = t.Route[t.Index + 1];
                if (city[ahead.X, ahead.Z].RailCrossing && (Closure(ahead.X, ahead.Z) < .999f || carInside?.Invoke(ahead.X, ahead.Z) == true))
                    next = Math.Min(next, .65f);
                if (trains.Any(other => other != t && Occupies(other, ahead.X, ahead.Z))) next = Math.Min(next, .65f);
            }
            t.Progress = Math.Max(t.Progress, next);
            if (t.Progress >= 1 && leg.Entry == leg.Exit && !leg.ReverseStub)
            {
                t.Progress = 1; t.Wait = ReverseWait; t.Reversing = true; continue;
            }
            if (t.Progress >= 1 && t.Index + 1 < t.Route.Count)
            {
                t.Progress = (t.Progress - 1) * leg.Length / t.Route[t.Index + 1].Length; t.Index++;
                if (t.Index > 3) { t.Route.RemoveAt(0); t.Index--; }
            }
        }
        trains.RemoveAll(t => t.Progress >= 1 && !t.Reversing);
    }
    private static bool Occupies(Train train, int x, int z)
    {
        float behind = train.Progress * train.Route[train.Index].Length;
        var head = train.Route[train.Index];
        if (head.X == x && head.Z == z) return true;
        for (int i = train.Index - 1; i >= 0 && behind < RearOffset + .23f; i--)
        {
            var leg = train.Route[i]; if (leg.X == x && leg.Z == z) return true;
            behind += leg.Length;
        }
        return false;
    }
    internal void Draw(int originX, int originZ)
    {
        foreach (var t in trains) for (int car = 0; car < CarCount; car++)
        {
            var pose = CarPose(t, car);
            Rlgl.PushMatrix();
            Rlgl.Translatef(pose.Position.X - originX, .16f, pose.Position.Y - originZ);
            Rlgl.Rotatef(-MathF.Atan2(pose.Heading.Y, pose.Heading.X) * 180 / MathF.PI, 0, 1, 0);
            DrawCube(new(0, .12f, 0), .46f, .23f, .25f, new(224, 226, 214, 255));
            DrawCube(new(0, .095f, 0), .47f, .055f, .26f, new(54, 139, 163, 255));
            DrawCube(new(0, .25f, 0), .43f, .035f, .23f, new(98, 113, 119, 255));
            for (int i = -1; i <= 1; i++) foreach (int side in new[] { -1, 1 })
                DrawCube(new(i * .13f, .18f, side * .128f), .09f, .07f, .012f, new(45, 73, 89, 255));
            if (car == 0 || car == CarCount - 1)
            {
                float cab = (car == 0 ? 1 : -1) * (t.Reversed ? -1 : 1);
                DrawCube(new(cab * .234f, .17f, 0), .012f, .085f, .18f, new(45, 73, 89, 255));
                DrawCube(new(cab * .24f, .055f, 0), .015f, .027f, .18f,
                    car == 0 ? new(255, 231, 157, 255) : new(231, 65, 53, 255));
            }
            Rlgl.PopMatrix();
        }
    }
}
