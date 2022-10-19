using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace dis.YoutubeDLSharp.Helpers;

/// <summary>
/// Provides methods for throttled execution of processes.
/// </summary>
public class ProcessRunner
{
    private const int MaxCount = 100;
    private readonly SemaphoreSlim _semaphore;

    public byte TotalCount { get; private set; }

    public ProcessRunner(byte initialCount)
    {
        _semaphore = new SemaphoreSlim(initialCount, MaxCount);
        TotalCount = initialCount;
    }

    public async Task<(int exitCode, string?[])> RunThrottled(YoutubeDlProcess process, string[]? urls, OptionSet options,
        CancellationToken ct, IProgress<DownloadProgress>? progress = default)
    {
        var errors = new List<string?>();
        process.ErrorReceived += (_, e) => errors.Add(e.Data);
        await _semaphore.WaitAsync(ct);
        try
        {
            var exitCode = await process.RunAsync(urls, options, ct, progress);
            return (exitCode, errors.ToArray());
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void IncrementCount(byte incr)
    {
        _semaphore.Release(incr);
        TotalCount += incr;
    }

    private async Task DecrementCount(byte decr)
    {
        await _semaphore.WaitAsync(decr);
        TotalCount -= decr;
    }

    public async Task SetTotalCount(byte count)
    {
        if (count is < 1 or > MaxCount)
            throw new ArgumentException($"Number of threads must be between 1 and {MaxCount}.");
        if (count > TotalCount)
            IncrementCount((byte)(count - TotalCount));
        else if (count < TotalCount)
            await DecrementCount((byte)(TotalCount - count));
    }
}