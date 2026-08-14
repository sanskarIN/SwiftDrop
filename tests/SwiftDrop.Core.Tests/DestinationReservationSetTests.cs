using System.Text;
using SwiftDrop.Core.Security;
using Xunit;

namespace SwiftDrop.Core.Tests;

public sealed class DestinationReservationSetTests
{
    [Fact]
    public void Reserve_DeconflictsConcurrentReservations()
    {
        var root = CreateTempDirectory();
        try
        {
            var requested = Path.Combine(root, "report.pdf");
            var reservations = new DestinationReservationSet();
            using var first = reservations.Reserve(requested);
            using var second = reservations.Reserve(requested);

            Assert.Equal(Path.GetFullPath(requested), first.Path);
            Assert.NotEqual(first.Path, second.Path);
            Assert.EndsWith("report (1).pdf", second.Path, StringComparison.Ordinal);
            Assert.True(reservations.IsReserved(first.Path));
            Assert.True(reservations.IsReserved(second.Path));
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public async Task Reserve_RemainsUniqueUnderConcurrentPressure()
    {
        var root = CreateTempDirectory();
        var leases = new List<DestinationReservationSet.DestinationReservation>();
        try
        {
            var requested = Path.Combine(root, "same-name.bin");
            var reservations = new DestinationReservationSet();
            var tasks = Enumerable.Range(0, 64)
                .Select(_ => Task.Run(() => reservations.Reserve(requested)))
                .ToArray();

            var acquired = await Task.WhenAll(tasks);
            leases.AddRange(acquired);

            Assert.Equal(64, acquired.Select(x => x.Path).Distinct(PathComparisonPolicy.Comparer).Count());
            Assert.All(acquired, lease => Assert.True(reservations.IsReserved(lease.Path)));
        }
        finally
        {
            foreach (var lease in leases) lease.Dispose();
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void Reserve_SkipsExistingCompletedDestination()
    {
        var root = CreateTempDirectory();
        try
        {
            var requested = Path.Combine(root, "photo.jpg");
            File.WriteAllText(requested, "existing");
            var reservations = new DestinationReservationSet();
            using var reservation = reservations.Reserve(requested);

            Assert.NotEqual(Path.GetFullPath(requested), reservation.Path);
            Assert.EndsWith("photo (1).jpg", reservation.Path, StringComparison.Ordinal);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void Reserve_MaxLengthNameKeepsUniqueBoundedCollisionMarker()
    {
        var root = CreateTempDirectory();
        try
        {
            var safeName = FileNameSanitizer.SanitizeSegment(new string('a', 300) + ".txt");
            var requested = Path.Combine(root, safeName);
            var reservations = new DestinationReservationSet();
            using var first = reservations.Reserve(requested);
            using var second = reservations.Reserve(requested);
            using var third = reservations.Reserve(requested);

            var secondName = Path.GetFileName(second.Path);
            var thirdName = Path.GetFileName(third.Path);
            Assert.StartsWith("(1) ", secondName, StringComparison.Ordinal);
            Assert.StartsWith("(2) ", thirdName, StringComparison.Ordinal);
            Assert.NotEqual(second.Path, third.Path);
            Assert.True(Encoding.UTF8.GetByteCount(secondName) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
            Assert.True(Encoding.UTF8.GetByteCount(thirdName) <= FileNameSanitizer.MaximumSegmentUtf8Bytes);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    [Fact]
    public void Dispose_ReleasesReservationForReuse()
    {
        var root = CreateTempDirectory();
        try
        {
            var requested = Path.Combine(root, "file.txt");
            var reservations = new DestinationReservationSet();
            var first = reservations.Reserve(requested);
            Assert.True(reservations.IsReserved(first.Path));
            first.Dispose();
            Assert.False(reservations.IsReserved(requested));

            using var next = reservations.Reserve(requested);
            Assert.Equal(Path.GetFullPath(requested), next.Path);
        }
        finally
        {
            DeleteBestEffort(root);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "swiftdrop-reservations-" + Guid.NewGuid().ToString("N"));
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
