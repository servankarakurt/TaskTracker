using GorevTakipUygulamasi.Data;
using GorevTakipUygulamasi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks; // Artık normal şekilde kullanabiliriz.

namespace GorevTakipUygulamasi.Services.Background
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BackgroundJobService> _logger;
        // Şimdilik notification servisini yorum satırı yapıyoruz, bir sonraki adımda çözeceğiz.
        // private readonly INotificationService _notificationService;

        // Yapıcı metodu da buna göre güncelliyoruz.
        public BackgroundJobService(ApplicationDbContext context, ILogger<BackgroundJobService> logger/*, INotificationService notificationService*/)
        {
            _context = context;
            _logger = logger;
            // _notificationService = notificationService;
        }

        public async Task ProcessPendingEmailRemindersAsync()
        {
            _logger.LogInformation("Bekleyen e-posta hatırlatıcıları işleniyor...");

            // Artık _context.Reminders'a erişebiliriz!
            var pendingReminders = await _context.Reminders
                .Where(r => !r.IsCompleted && r.ReminderDate <= DateTime.Now)
                .ToListAsync();

            foreach (var reminder in pendingReminders)
            {
                _logger.LogInformation("{Id} ID'li hatırlatıcı için e-posta gönderilecek.", reminder.Id);
                // _notificationService.SendReminderEmailAsync(reminder);
                reminder.IsCompleted = true;
            }

            if (pendingReminders.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        public Task CleanupExpiredRemindersAsync()
        {
            _logger.LogInformation("Süresi geçmiş hatırlatıcılar temizleniyor...");
            return Task.CompletedTask; // async olmadığı için direkt Task döndürüyoruz.
        }

        public Task SendDailySummaryEmailsAsync()
        {
            _logger.LogInformation("Günlük özet e-postaları gönderiliyor...");
            return Task.CompletedTask; // async olmadığı için direkt Task döndürüyoruz.
        }
    }
}