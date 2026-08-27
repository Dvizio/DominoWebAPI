namespace DominoWebAPI.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class GameSessionCleanupService : BackgroundService
{
    private readonly GameSessionManager _sessionManager;
    private readonly ILogger<GameSessionCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _inactivityTimeout = TimeSpan.FromMinutes(10);

    public GameSessionCleanupService(
        GameSessionManager sessionManager,
        ILogger<GameSessionCleanupService> logger)
    {
        _sessionManager = sessionManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GameSessionCleanupService started. Monitoring for sessions idle/disconnected > 10 minutes.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                int removed = _sessionManager.CleanupExpiredSessions(_inactivityTimeout);
                if (removed > 0)
                {
                    _logger.LogInformation("GameSessionCleanupService removed {Count} expired / idle game session(s) from memory.", removed);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background session cleanup.");
            }
        }
    }
}

