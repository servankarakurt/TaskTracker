namespace GorevTakipUygulamasi.Services.Background
{
    public interface IBackgroundJobService
    {
        System.Threading.Tasks.Task SendDailySummaryEmailsAsync();
    }
}