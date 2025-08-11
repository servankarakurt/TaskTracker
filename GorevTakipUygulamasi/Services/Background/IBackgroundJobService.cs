namespace GorevTakipUygulamasi.Services.Background
{
    public interface IBackgroundJobService
    {
        Task ProcessPendingEmailRemindersAsync();
        Task CleanupExpiredRemindersAsync();
        Task SendDailySummaryEmailsAsync();
    }
}
