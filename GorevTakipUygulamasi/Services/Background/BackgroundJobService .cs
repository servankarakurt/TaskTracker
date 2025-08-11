using GorevTakipUygulamasi.Models;
using GorevTakipUygulamasi.Services.LogicApp;
using GorevTakipUygulamasi.Services.User;

namespace GorevTakipUygulamasi.Services.Background
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IReminderService _reminderService;
        private readonly ILogicAppService _logicAppService;
        private readonly IUserService _userService;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IReminderService reminderService,
            ILogicAppService logicAppService,
            IUserService userService,
            ILogger<BackgroundJobService> logger)
        {
            _reminderService = reminderService;
            _logicAppService = logicAppService;
            _userService = userService;
            _logger = logger;
        }

        public async Task ProcessPendingEmailRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Email hatırlatıcı kontrolü başlatılıyor...");

                var pendingRemindersResponse = await _reminderService.GetPendingEmailRemindersAsync();

                if (!pendingRemindersResponse.IsSuccess || !pendingRemindersResponse.Data.Any())
                {
                    _logger.LogInformation("Bekleyen email hatırlatıcısı bulunamadı.");
                    return;
                }

                var pendingReminders = pendingRemindersResponse.Data;
                _logger.LogInformation("{Count} adet bekleyen email hatırlatıcısı bulundu.", pendingReminders.Count);

                foreach (var reminder in pendingReminders)
                {
                    try
                    {
                        // Kullanıcı bilgilerini al
                        var userProfile = await _userService.GetUserProfileAsync(reminder.UserId);

                        if (userProfile == null || !userProfile.EmailNotificationsEnabled)
                        {
                            _logger.LogWarning("Kullanıcı bulunamadı veya email bildirimleri kapalı: {UserId}", reminder.UserId);
                            continue;
                        }

                        // Email notification DTO'sunu oluştur
                        var notification = new ReminderNotificationDto
                        {
                            ReminderId = reminder.Id.ToString(),
                            UserId = reminder.UserId,
                            UserEmail = userProfile.Email,
                            Title = reminder.Title,
                            Description = reminder.Description,
                            ScheduledDateTime = reminder.Date.ToDateTime(reminder.Time),
                            NotificationType = "Email"
                        };

                        // Logic App'e gönder
                        var success = await _logicAppService.SendImmediateEmailAsync(notification);

                        if (success)
                        {
                            // Email gönderildi olarak işaretle
                            await _reminderService.MarkEmailAsSentAsync(reminder.Id);

                            _logger.LogInformation("Email hatırlatıcısı gönderildi: {ReminderId} - {Title}",
                                reminder.Id, reminder.Title);
                        }
                        else
                        {
                            _logger.LogError("Email gönderilemedi: {ReminderId} - {Title}",
                                reminder.Id, reminder.Title);
                        }

                        // Rate limiting için kısa bekleme
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Email hatırlatıcısı işlenirken hata: {ReminderId}", reminder.Id);
                    }
                }

                _logger.LogInformation("Email hatırlatıcı kontrolü tamamlandı.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email hatırlatıcı kontrolü sırasında genel hata");
            }
        }

        public async Task CleanupExpiredRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Süresi dolmuş hatırlatıcılar temizleniyor...");

                // 30 günden eski tamamlanmış hatırlatıcıları temizle
                var cutoffDate = DateTime.UtcNow.AddDays(-30);

                // Bu işlem için özel bir cleanup metodu gerekli
                // Şimdilik log mesajı bırakıyoruz
                _logger.LogInformation("Cleanup işlemi: {CutoffDate} tarihinden eski hatırlatıcılar", cutoffDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup işlemi sırasında hata");
            }
        }

        public async Task SendDailySummaryEmailsAsync()
        {
            try
            {
                _logger.LogInformation("Günlük özet emailları gönderiliyor...");

                // Yarınki hatırlatıcıları topla ve kullanıcılara özet gönder
                var tomorrow = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

                // Bu özellik için ayrı bir implementasyon gerekli
                _logger.LogInformation("Yarınki hatırlatıcılar için özet: {Date}", tomorrow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Günlük özet gönderimi sırasında hata");
            }
        }
    }
}
