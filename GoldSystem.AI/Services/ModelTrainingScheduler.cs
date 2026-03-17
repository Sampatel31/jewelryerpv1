using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoldSystem.AI.Services;

/// <summary>
/// Background service that retrains ML.NET models every Sunday at 02:00.
/// Runs in a polling loop (1-minute intervals) and is completely non-blocking —
/// a failed retraining run logs the error and resumes normal operation.
/// Tracks the last training date so retraining is never skipped due to a narrow
/// scheduling window or service restart during the target hour.
/// </summary>
public class ModelTrainingScheduler : BackgroundService
{
    private readonly ISalesForecastService _forecastService;
    private readonly IAnomalyDetectorService _anomalyService;
    private readonly ILogger<ModelTrainingScheduler> _logger;

    // Allow up to 5-minute window to avoid missing the slot if polling is slightly delayed.
    private const int TRAINING_WINDOW_MINUTES = 5;

    private DateOnly _lastTrainedDate = DateOnly.MinValue;

    public ModelTrainingScheduler(
        ISalesForecastService forecastService,
        IAnomalyDetectorService anomalyService,
        ILogger<ModelTrainingScheduler> logger)
    {
        _forecastService = forecastService ?? throw new ArgumentNullException(nameof(forecastService));
        _anomalyService = anomalyService ?? throw new ArgumentNullException(nameof(anomalyService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("Model training scheduler started.");

        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);

            // Retrain every Sunday at 02:00, within the first TRAINING_WINDOW_MINUTES.
            // The _lastTrainedDate guard ensures we only retrain once per week even if
            // the service restarts during the target window.
            if (now.DayOfWeek == DayOfWeek.Sunday
                && now.Hour == 2
                && now.Minute < TRAINING_WINDOW_MINUTES
                && _lastTrainedDate < today)
            {
                try
                {
                    _logger.LogInformation("Starting scheduled model retraining...");

                    await _forecastService.TrainIfDataSufficientAsync();
                    await _anomalyService.TrainIfDataSufficientAsync();

                    _lastTrainedDate = today;
                    _logger.LogInformation("Scheduled model retraining completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during scheduled model retraining");
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(1), ct);
        }
    }
}
