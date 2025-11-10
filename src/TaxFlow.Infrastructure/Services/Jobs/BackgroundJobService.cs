using System.Collections.Concurrent;
using System.Text.Json;
using TaxFlow.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace TaxFlow.Infrastructure.Services.Jobs;

/// <summary>
/// Background job processing service
/// </summary>
public class BackgroundJobService : IBackgroundJobService
{
    private readonly ILogger<BackgroundJobService> _logger;
    private readonly ConcurrentDictionary<Guid, BackgroundJobStatus> _jobs = new();
    private readonly ConcurrentQueue<(Guid JobId, BackgroundJobType JobType, object Parameters)> _jobQueue = new();

    public BackgroundJobService(ILogger<BackgroundJobService> logger)
    {
        _logger = logger;
    }

    public Task<Guid> EnqueueJobAsync(
        BackgroundJobType jobType,
        object parameters,
        CancellationToken cancellationToken = default)
    {
        var jobId = Guid.NewGuid();

        var jobStatus = new BackgroundJobStatus
        {
            JobId = jobId,
            JobType = jobType,
            Status = "Queued",
            QueuedAt = DateTime.UtcNow
        };

        _jobs[jobId] = jobStatus;
        _jobQueue.Enqueue((jobId, jobType, parameters));

        _logger.LogInformation(
            "Job {JobId} ({JobType}) enqueued",
            jobId,
            jobType);

        // In production, this would trigger actual background processing
        // For now, we just queue it
        _ = ProcessJobAsync(jobId, jobType, parameters, cancellationToken);

        return Task.FromResult(jobId);
    }

    public Task<BackgroundJobStatus> GetJobStatusAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (_jobs.TryGetValue(jobId, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new BackgroundJobStatus
        {
            JobId = jobId,
            Status = "NotFound"
        });
    }

    public Task<bool> CancelJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (_jobs.TryGetValue(jobId, out var status))
        {
            if (status.Status == "Queued" || status.Status == "Running")
            {
                status.Status = "Cancelled";
                status.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("Job {JobId} cancelled", jobId);
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<List<BackgroundJobInfo>> GetPendingJobsAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = _jobs.Values
            .Where(j => j.Status == "Queued" || j.Status == "Running")
            .Select(j => new BackgroundJobInfo
            {
                JobId = j.JobId,
                JobType = j.JobType,
                Status = j.Status,
                QueuedAt = j.QueuedAt
            })
            .OrderBy(j => j.QueuedAt)
            .ToList();

        return Task.FromResult(pending);
    }

    public Task<List<BackgroundJobInfo>> GetCompletedJobsAsync(
        int count = 100,
        CancellationToken cancellationToken = default)
    {
        var completed = _jobs.Values
            .Where(j => j.Status == "Completed" || j.Status == "Failed" || j.Status == "Cancelled")
            .Select(j => new BackgroundJobInfo
            {
                JobId = j.JobId,
                JobType = j.JobType,
                Status = j.Status,
                QueuedAt = j.QueuedAt,
                CompletedAt = j.CompletedAt
            })
            .OrderByDescending(j => j.CompletedAt)
            .Take(count)
            .ToList();

        return Task.FromResult(completed);
    }

    private async Task ProcessJobAsync(
        Guid jobId,
        BackgroundJobType jobType,
        object parameters,
        CancellationToken cancellationToken)
    {
        if (!_jobs.TryGetValue(jobId, out var status))
            return;

        try
        {
            status.Status = "Running";
            status.StartedAt = DateTime.UtcNow;

            _logger.LogInformation("Processing job {JobId} ({JobType})", jobId, jobType);

            // Simulate job processing
            await Task.Delay(100, cancellationToken);

            status.Status = "Completed";
            status.CompletedAt = DateTime.UtcNow;
            status.ProgressPercentage = 100;

            _logger.LogInformation("Job {JobId} completed successfully", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", jobId);

            status.Status = "Failed";
            status.ErrorMessage = ex.Message;
            status.CompletedAt = DateTime.UtcNow;
        }
    }
}
