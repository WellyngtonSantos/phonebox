using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using PhoneBox.Agent;
using Xunit;

/// <summary>Each sample carries a number that grows, so the panel adds a chart point only for a new sample, not for
/// the same one sent again with a media or volume change.</summary>
public sealed class SystemMonitorSeqTests
{
    [Fact]
    public async Task EachSampleHasAHigherSeqThanTheOneBefore()
    {
        var monitor = new SystemMonitor(NullLogger<SystemMonitor>.Instance);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await monitor.StartAsync(cts.Token);
        try
        {
            var first = await Next(monitor, 0, cts.Token);
            var second = await Next(monitor, first.Seq, cts.Token);
            Assert.True(first.Seq > 0);
            Assert.True(second.Seq > first.Seq);
        }
        finally { await monitor.StopAsync(CancellationToken.None); }
    }

    [Fact]
    public void TheSeqReachesThePanelAsCamelCase()
    {
        var snap = new PcSnapshot(1, null, "cpu", 1, 2, null, 0, 0, [], 0, Seq: 7);
        var json = JsonSerializer.Serialize(snap, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.Contains("\"seq\":7", json);
    }

    private static async Task<PcSnapshot> Next(SystemMonitor monitor, long after, CancellationToken ct)
    {
        while (true)
        {
            if (monitor.Snapshot() is { } s && s.Seq > after) return s;
            await Task.Delay(100, ct);
        }
    }
}
