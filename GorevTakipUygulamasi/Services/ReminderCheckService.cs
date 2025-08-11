using GorevTakipUygulamasi.Services.Background;
using Microsoft.Extensions.DependencyInjection; // Eksik using eklendi
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
// 'Task' çakışmasını çözmek için tam yoluyla belirtiyoruz:
using System.Threading.Tasks;

namespace GorevTakipUygulamasi.Services
{
    public class ReminderCheckService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReminderCheckService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

        public ReminderCheckService(IServiceProvider serviceProvider, ILogger<ReminderCheckService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Hatırlatıcı kontrol servisi başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Arka plan işleri tetikleniyor. Zaman: {time}", DateTimeOffset.Now);

                try
                {
                    // Her döngüde yeni bir 'scope' oluşturarak servisleri doğru şekilde alıyoruz.
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var backgroundJobService = scope.ServiceProvider.GetRequiredService<IBackgroundJobService>();
                        await backgroundJobService.ProcessPendingEmailRemindersAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Arka plan işi yürütülürken hata oluştu.");
                }

                // Bir sonraki döngüye kadar bekle.
                await System.Threading.Tasks.Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Hatırlatıcı kontrol servisi durduruluyor.");
        }
    }
}