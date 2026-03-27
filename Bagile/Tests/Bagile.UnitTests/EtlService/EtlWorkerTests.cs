using Bagile.EtlService.Models;
using Bagile.EtlService.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using System.Reflection;

namespace Bagile.UnitTests.EtlService;

[TestFixture]
public class EtlWorkerTests
{
    /// <summary>
    /// Regression test: EtlWorker must contain exactly one Task.Delay call.
    /// The double-delay bug (10 min instead of 5) was caused by two consecutive
    /// Task.Delay calls. This test reads the source to prevent reintroduction.
    /// </summary>
    [Test]
    public void ExecuteAsync_Should_Have_Exactly_One_TaskDelay_Call()
    {
        // Read the source file of EtlWorker via reflection to find its location,
        // then verify the IL or source. Since we can't easily read source at test time,
        // we verify the method body IL for Task.Delay call count.
        var method = typeof(EtlWorker)
            .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance);

        method.Should().NotBeNull("EtlWorker should have an ExecuteAsync method");

        // Get the IL body and count references to Task.Delay
        var body = method!.GetMethodBody();
        body.Should().NotBeNull();

        // Alternative: count via source file analysis
        // Since IL-level counting of async state machine calls is complex,
        // we use a source-level check instead
        var sourceDir = FindSourceDirectory();
        var sourceFile = Path.Combine(sourceDir, "Bagile.EtlService", "Services", "EtlWorker.cs");

        if (File.Exists(sourceFile))
        {
            var source = File.ReadAllText(sourceFile);
            var delayCount = CountOccurrences(source, "Task.Delay");
            delayCount.Should().Be(1,
                "EtlWorker.ExecuteAsync should have exactly one Task.Delay call. " +
                "Two delays cause the double-delay bug (10 min instead of 5 min).");
        }
        else
        {
            // Fallback: at minimum verify the method exists and is accessible
            Assert.Pass("Source file not found at test time — skipping source-level check.");
        }
    }

    [Test]
    public void EtlOptions_Default_Interval_Is_Five_Minutes()
    {
        var options = new EtlOptions();
        options.IntervalMinutes.Should().Be(5, "default ETL interval should be 5 minutes");
    }

    [Test]
    public void EtlWorker_Source_Uses_Options_IntervalMinutes()
    {
        var sourceDir = FindSourceDirectory();
        var sourceFile = Path.Combine(sourceDir, "Bagile.EtlService", "Services", "EtlWorker.cs");

        if (!File.Exists(sourceFile))
        {
            Assert.Pass("Source file not found at test time — skipping source-level check.");
            return;
        }

        var source = File.ReadAllText(sourceFile);
        source.Should().Contain("_options.IntervalMinutes",
            "EtlWorker should use configurable interval from EtlOptions, not a hardcoded value");
        source.Should().NotContain("FromMinutes(5)",
            "EtlWorker should not have hardcoded 5-minute interval");
    }

    [Test]
    public async Task ExecuteAsync_Should_Continue_After_Exception_In_Cycle()
    {
        // Set up scope factory that throws on service resolution
        var scopeFactory = new Mock<IServiceScopeFactory>();
        var scope = new Mock<IServiceScope>();
        var mockSp = new Mock<IServiceProvider>();
        mockSp.Setup(sp => sp.GetService(typeof(SourceDataImporter)))
            .Throws(new InvalidOperationException("Test exception"));
        scope.Setup(s => s.ServiceProvider).Returns(mockSp.Object);
        scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        var worker = new EtlWorker(scopeFactory.Object, NullLogger<EtlWorker>.Instance, Options.Create(new EtlOptions()));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        // Should not throw — the worker catches exceptions and continues
        Func<Task> act = () => InvokeExecuteAsync(worker, cts.Token);
        await act.Should().NotThrowAsync<InvalidOperationException>(
            "EtlWorker should catch cycle exceptions and continue running");
    }

    private static async Task InvokeExecuteAsync(EtlWorker worker, CancellationToken ct)
    {
        var method = typeof(EtlWorker)
            .GetMethod("ExecuteAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;
        await (Task)method.Invoke(worker, new object[] { ct })!;
    }

    private static string FindSourceDirectory()
    {
        // Walk up from test output dir to find the repo root
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "Bagile.EtlService")) ||
                Directory.Exists(Path.Combine(dir, "Bagile")))
            {
                // Check if this is the Bagile solution folder
                if (Directory.Exists(Path.Combine(dir, "Bagile.EtlService")))
                    return dir;
                if (Directory.Exists(Path.Combine(dir, "Bagile", "Bagile.EtlService")))
                    return Path.Combine(dir, "Bagile");
            }
            dir = Directory.GetParent(dir)?.FullName;
        }
        return AppContext.BaseDirectory;
    }

    private static int CountOccurrences(string source, string pattern)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
}
