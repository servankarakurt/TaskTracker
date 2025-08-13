using System.Threading.Tasks;
namespace GorevTakipUygulamasi.Services.Hatirlatici
{
    public interface IReminderCheckService
    {
        Task CheckAndProcessRemindersAsync();
        Task CleanupExpiredRemindersAsync();
    }
}
