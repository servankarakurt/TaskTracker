using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Services.Hatirlatici
{
    public interface IReminderService
    {
        Task<ServiceResponse<ReminderItem>> CreateReminderAsync(CreateReminderDto dto, string userId);
        Task<ServiceResponse<ReminderItem>> GetReminderAsync(Guid id, string userId);
        Task<ServiceResponse<List<ReminderItem>>> GetUserRemindersAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<ServiceResponse<List<ReminderItem>>> GetRemindersByDateAsync(string userId, DateOnly date);
        Task<ServiceResponse<ReminderItem>> UpdateReminderAsync(Guid id, UpdateReminderDto dto, string userId);
        Task<ServiceResponse<bool>> DeleteReminderAsync(Guid id, string userId);
        Task<ServiceResponse<bool>> ToggleReminderAsync(Guid id, string userId);
        Task<ServiceResponse<PagedResult<ReminderItem>>> GetRemindersPagedAsync(ReminderFilter filter);
        Task<ServiceResponse<List<ReminderItem>>> GetPendingEmailRemindersAsync();
        Task<ServiceResponse<bool>> MarkEmailAsSentAsync(Guid id);
    }
}
