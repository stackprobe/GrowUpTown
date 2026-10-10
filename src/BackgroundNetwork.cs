using System.Diagnostics;

namespace GrowUpTown;

public sealed partial class City
{
    public bool DeferredUpdates { get; private set; }
    public bool NetworkUpdatePending => networkDirty || snapshotCity is not null || networkJob is not null || networkResult is not null;
    public string? NetworkUpdateError { get; private set; }
    private long networkRevision, capturedRevision;
    private double networkClock, firstDirtyTime, lastEditTime;
    private bool networkDirty;
    private City? snapshotCity, networkResult;
    private ChunkPos[] snapshotKeys = [], resultKeys = [];
    private HashSet<ChunkPos> packedSnapshotKeys = [];
    private int snapshotIndex, resultIndex;
    private Task<City>? networkJob;
    internal int NetworkJobsStarted { get; private set; }

    public void EnableDeferredUpdates() => DeferredUpdates = true;
    public void StopBackgroundUpdates()
    {
        // Workers own only snapshots; abandoning a job never leaves a live-city writer.
        networkRevision++; networkDirty = false; snapshotCity = networkResult = null;
        if (networkJob is not null) ObserveAbandoned(networkJob);
        networkJob = null; snapshotKeys = resultKeys = []; packedSnapshotKeys = [];
    }
    private static void ObserveAbandoned(Task<City> job) =>
        _ = job.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
    private void ScheduleNetworkUpdate()
    {
        networkRevision++;
        if (!networkDirty) firstDirtyTime = networkClock;
        networkDirty = true; lastEditTime = networkClock;
    }
    private void SynchronizeEdit(int x, int z, TileKind kind)
    {
        if (kind == TileKind.Bulldoze) (x, z) = AnchorAt(x, z);
        int size = Footprint(kind == TileKind.Bulldoze ? this[x, z].Kind : kind);
        var affected = new HashSet<ChunkPos>();
        for (int dz = 0; dz < size; dz++) for (int dx = 0; dx < size; dx++) affected.Add(ChunkPos.FromTile(x + dx, z + dz));
        var conditions = new Conditions(this);
        foreach (var key in affected) Advance(chunks[key], Month - 1, conditions);
    }
    public void ServiceBackgroundUpdates(double elapsedSeconds)
    {
        if (!DeferredUpdates) return;
        networkClock += Math.Max(0, elapsedSeconds);
        long started = Stopwatch.GetTimestamp();
        bool HasBudget() => Stopwatch.GetElapsedTime(started).TotalMilliseconds < 2;

        if (networkJob is { IsCompleted: true })
        {
            var completed = networkJob; networkJob = null;
            if (completed.IsCompletedSuccessfully)
            {
                if (capturedRevision == networkRevision)
                { networkResult = completed.Result; resultKeys = snapshotKeys; resultIndex = 0; }
            }
            else
            {
                NetworkUpdateError = completed.Exception?.GetBaseException().Message ?? "再計算を完了できませんでした";
                // Keep the latest edit pending and retry after a short interval.
                networkDirty = true; firstDirtyTime = lastEditTime = networkClock;
            }
        }
        if (networkResult is not null)
        {
            if (capturedRevision != networkRevision) networkResult = null;
            else
            {
                while (resultIndex < resultKeys.Length && HasBudget())
                {
                    var key = resultKeys[resultIndex++];
                    chunks[key].ApplyNetwork(networkResult.chunks[key]);
                }
                if (resultIndex == resultKeys.Length) { networkResult = null; NetworkUpdateError = null; }
            }
        }
        if (snapshotCity is not null && capturedRevision != networkRevision) snapshotCity = null;
        if (networkJob is not null || networkResult is not null) return;
        if (snapshotCity is null && networkDirty && (networkClock - lastEditTime >= .35 || networkClock - firstDirtyTime >= 2))
        {
            capturedRevision = networkRevision;
            snapshotCity = new City(false); snapshotCity.chunks.Clear();
            snapshotKeys = chunks.Keys.ToArray(); snapshotIndex = 0; packedSnapshotKeys = [];
        }
        if (snapshotCity is null) return;
        // Capture on the owner thread in small batches. No mutable tile or dictionary
        // is shared with the worker, and packed chunks need no owner-thread expansion.
        while (snapshotIndex < snapshotKeys.Length && HasBudget())
        {
            var key = snapshotKeys[snapshotIndex++]; var source = chunks[key];
            if (source.IsPacked) packedSnapshotKeys.Add(key);
            snapshotCity.chunks.Add(key, source.NetworkSnapshot());
        }
        if (snapshotIndex != snapshotKeys.Length) return;
        var snapshot = snapshotCity; var packedKeys = packedSnapshotKeys;
        snapshotCity = null; networkDirty = false; NetworkJobsStarted++;
        networkJob = Task.Run(() =>
        {
            snapshot.Reconnect();
            foreach (var key in packedKeys) snapshot.chunks[key].PrepareNetworkPayload();
            return snapshot;
        });
    }
}
