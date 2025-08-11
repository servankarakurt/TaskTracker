using Task = System.Threading.Tasks.Task;

namespace GorevTakipUygulamasi.Services.Background
{
    public interface IBackgroundJobService
    {
        Task ProcessPendingEmailRemindersAsync();
        Task CleanupExpiredRemindersAsync();
        Task SendDailySummaryEmailsAsync();
    }
}