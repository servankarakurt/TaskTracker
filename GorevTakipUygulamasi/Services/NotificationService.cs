using GorevTakipUygulamasi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
// DEĞİŞİKLİK: 'System.Threading.Tasks.Task' için 'SystemTask' takma adı eklendi.
using SystemTask = System.Threading.Tasks.Task;

namespace GorevTakipUygulamasi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        // private readonly IEmailSender _emailSender; // Eğer bir e-posta servisiniz varsa bu satırı aktif edebilirsiniz.

        public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async SystemTask SendDailySummaryEmailsAsync() // DEĞİŞİKLİK: 'Task' -> 'SystemTask'
        {
            _logger.LogInformation("Günlük özet e-postaları gönderimi başlıyor.");

            var users = await _context.Users.ToListAsync();

            foreach (var user in users)
            {
                var tasksDueToday = await _context.TaskItems
                    .Where(t => t.UserId == user.Id && t.DueDate.Date == DateTime.UtcNow.Date && !t.IsCompleted)
                    .ToListAsync();

                if (tasksDueToday.Any())
                {
                    var subject = "Bugün Yapılacak Görevleriniz";
                    var message = "Merhaba, bugün tamamlamanız gereken görevler aşağıdadır:\n\n";
                    tasksDueToday.ForEach(t => message += $"- {t.Title}\n");

                    _logger.LogInformation("{UserId} ID'li kullanıcıya e-posta gönderiliyor: {Subject}", user.Id, subject);
                    // await _emailSender.SendEmailAsync(user.Email, subject, message); // E-posta gönderme servisi aktif olduğunda bu satırı kullanın.
                }
            }

            _logger.LogInformation("Günlük özet e-postaları gönderimi tamamlandı.");
        }
    }

    public interface INotificationService
    {
        SystemTask SendDailySummaryEmailsAsync(); // DEĞİŞİKLİK: 'Task' -> 'SystemTask'
    }
}