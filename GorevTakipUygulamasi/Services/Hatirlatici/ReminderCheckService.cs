using GorevTakipUygulamasi.Data;
using GorevTakipUygulamasi.Services.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SystemTask = System.Threading.Tasks.Task;

namespace GorevTakipUygulamasi.Services
{
    public class ReminderCheckService : IReminderCheckService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReminderCheckService> _logger;
        private readonly ReminderNotificationService _notificationService;

        public ReminderCheckService(ApplicationDbContext context, ILogger<ReminderCheckService> logger, ReminderNotificationService notificationService)
        {
            _context = context;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        }

        public async SystemTask CheckAndProcessRemindersAsync()
        {
            _logger.LogInformation("Hatırlatıcı kontrolü başladı.");
            var remindersToProcess = await _context.Reminders
                .Where(r => r.ReminderTime <= DateTime.UtcNow && !r.IsSent)
                .ToListAsync();

            foreach (var reminder in remindersToProcess)
            {
                _logger.LogInformation("{ReminderId} ID'li hatırlatıcı işleniyor.", reminder.Id);
                reminder.IsSent = true;
                _context.Update(reminder);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("{Count} adet hatırlatıcı işlendi.", remindersToProcess.Count);
        }

        public async SystemTask CleanupExpiredRemindersAsync()
        {
            _logger.LogInformation("Süresi dolmuş hatırlatıcı temizliği yapılacak.");
            await SystemTask.CompletedTask;
        }
    }

    public interface IReminderCheckService
    {
        SystemTask CheckAndProcessRemindersAsync();
        SystemTask CleanupExpiredRemindersAsync();
    }
}
