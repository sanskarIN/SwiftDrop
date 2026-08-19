using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class DestinationReservationSetStateMachineTests
{
    [Fact]
    public void Reservations_PreserveUniquenessAcrossSeededFilesystemOperations()
    {
        const int operationCount = 3_000;
        var root = CreateTempDirectory();
        var active = new Dictionary<string, DestinationReservationSet.DestinationReservation>(PathComparisonPolicy.Comparer);
        var reservations = new DestinationReservationSet();
        var random = new Random(0xD3571);

        try
        {
            for (var operation = 0; operation < operationCount; operation++)
            {
                var requested = Path.Combine(root, $"item-{random.Next(12):D2}.bin");
                switch (random.Next(6))
                {
                    case 0:
                    case 1:
                    case 2:
                    {
                        var lease = reservations.Reserve(requested);
                        Assert.False(active.ContainsKey(lease.Path));
                        Assert.False(File.Exists(lease.Path));
                        Assert.False(Directory.Exists(lease.Path));
                        Assert.True(reservations.IsReserved(lease.Path));
                        active.Add(lease.Path, lease);
                        break;
                    }
                    case 3 when active.Count > 0:
                    {
                        var index = random.Next(active.Count);
                        var pair = active.ElementAt(index);
                        pair.Value.Dispose();
                        active.Remove(pair.Key);
                        Assert.False(reservations.IsReserved(pair.Key));
                        break;
                    }
                    case 4:
                    {
                        var fullRequested = Path.GetFullPath(requested);
                        if (!reservations.IsReserved(fullRequested) && !File.Exists(fullRequested))
                            File.WriteAllBytes(fullRequested, [0x53, 0x44]);
                        break;
                    }
                    case 5:
                    {
                        var fullRequested = Path.GetFullPath(requested);
                        if (!reservations.IsReserved(fullRequested) && File.Exists(fullRequested))
                            File.Delete(fullRequested);
                        break;
                    }
                }

                Assert.Equal(active.Count, active.Keys.Distinct(PathComparisonPolicy.Comparer).Count());
                Assert.All(active.Keys, path => Assert.True(reservations.IsReserved(path)));
            }
        }
        finally
        {
            foreach (var lease in active.Values)
                lease.Dispose();
            DeleteBestEffort(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-reservation-state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteBestEffort(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
        }
    }
}
