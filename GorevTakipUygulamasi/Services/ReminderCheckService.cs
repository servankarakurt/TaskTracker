using GorevTakipUygulamasi.Services.Background;

namespace GorevTakipUygulamasi.Services
{
    public class ReminderCheckService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderCheckService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // 5 dakikada bir kontrol

        public ReminderCheckService(
            IServiceProvider serviceProvider,
            ILogger<ReminderCheckService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hatırlatıcı kontrol servisi başlatıldı. Kontrol aralığı: {Interval}", _checkInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();

                    // Ana iş: Email hatırlatıcılarını kontrol et
                    await backgroundJobService.ProcessPendingEmailRemindersAsync();

                    // Saatlik temizlik (sadece her saat başı)
                    if (DateTime.Now.Minute == 0)
                    {
                        await backgroundJobService.CleanupExpiredRemindersAsync();
                    }

                    // Günlük özet (sadece sabah 08:00'de)
                    if (DateTime.Now.Hour == 8 && DateTime.Now.Minute == 0)
                    {
                        await backgroundJobService.SendDailySummaryEmailsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hatırlatıcı kontrol servisi hatası");
                }

                // Belirtilen aralıkta bekle
                try
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Servis durdurulduğunda normal davranış
                    break;
                }
            }

            _logger.LogInformation("Hatırlatıcı kontrol servisi durduruldu.");
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hatırlatıcı kontrol servisi başlatılıyor...");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Hatırlatıcı kontrol servisi durduruluyor...");
            await base.StopAsync(cancellationToken);
        }
    }
}
