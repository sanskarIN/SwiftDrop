using SwiftDrop.Core.Security;

namespace SwiftDrop.Core.Tests;

public sealed class DestinationReservationSetDisposalTests
{
    [Fact]
    public async Task Dispose_IsIdempotentUnderConcurrentCalls()
    {
        var root = Path.Combine(Path.GetTempPath(), "swiftdrop-reservation-dispose-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var requested = Path.Combine(root, "reusable.bin");
            var reservations = new DestinationReservationSet();
            var lease = reservations.Reserve(requested);

            var disposals = Enumerable.Range(0, 32)
                .Select(_ => Task.Run(lease.Dispose))
                .ToArray();
            await Task.WhenAll(disposals);

            Assert.False(reservations.IsReserved(requested));
            using var replacement = reservations.Reserve(requested);
            Assert.Equal(Path.GetFullPath(requested), replacement.Path);
        }
        finally
        {
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch
            {
            }
        }
    }
}
