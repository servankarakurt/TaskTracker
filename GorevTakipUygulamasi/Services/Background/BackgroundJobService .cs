using GorevTakipUygulamasi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Hangfire;

namespace GorevTakipUygulamasi.Services.Background
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BackgroundJobService> _logger;
        // private readonly INotificationService _notificationService;

        public BackgroundJobService(ApplicationDbContext context, ILogger<BackgroundJobService> logger/*, INotificationService notificationService*/)
        {
            _context = context;
            _logger = logger;
            // _notificationService = notificationService;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
        public System.Threading.Tasks.Task SendDailySummaryEmailsAsync()
        {
            _logger.LogInformation("Günlük özet e-postaları gönderiliyor...");
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}