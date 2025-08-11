using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using GorevTakipUygulamasi.Models;
using GorevTakipUygulamasi.Services.LogicApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GorevTakipUygulamasi.Services
{
    public class ReminderService : IReminderService
    {
        private readonly TableClient _tableClient;
        private readonly ILogger<ReminderService> _logger;
        private readonly ILogicAppService _logicAppService;
        private const string TableName = "Reminders";

        public ReminderService(
            TableServiceClient tableServiceClient,
            ILogger<ReminderService> logger,
            ILogicAppService logicAppService)
        {
            _tableClient = tableServiceClient.GetTableClient(TableName);
            _logger = logger;
            _logicAppService = logicAppService;

            // Tabloyu oluştur (yoksa)
            _tableClient.CreateIfNotExists();
        }

        public async Task<ServiceResponse<ReminderItem>> CreateReminderAsync(CreateReminderDto dto, string userId)
        {
            try
            {
                var reminder = new ReminderItem
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Description = dto.Description,
                    Date = dto.Date,
                    Time = dto.Time,
                    EmailReminder = dto.EmailReminder,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Status = ReminderStatus.Active
                };

                var entity = new ReminderEntity(reminder, userId);

                var response = await _tableClient.AddEntityAsync(entity);

                _logger.LogInformation("Hatırlatıcı oluşturuldu: {ReminderId} - {Title}", reminder.Id, reminder.Title);

                // Logic App'e notification schedule et
                if (reminder.EmailReminder)
                {
                    await _logicAppService.ScheduleReminderAsync(reminder);
                }

                return ServiceResponse<ReminderItem>.Success(reminder, "Hatırlatıcı başarıyla oluşturuldu");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcı oluşturulurken hata: {Error}", ex.Message);
                return ServiceResponse<ReminderItem>.Error("Hatırlatıcı oluşturulamadı");
            }
        }

        public async Task<ServiceResponse<ReminderItem>> GetReminderAsync(Guid id, string userId)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<ReminderEntity>(userId, id.ToString());

                if (response?.Value != null)
                {
                    var reminder = response.Value.ToReminderItem();
                    return ServiceResponse<ReminderItem>.Success(reminder);
                }

                return ServiceResponse<ReminderItem>.Error("Hatırlatıcı bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcı getirilirken hata: {ReminderId}", id);
                return ServiceResponse<ReminderItem>.Error("Hatırlatıcı getirilemedi");
            }
        }

        public async Task<ServiceResponse<List<ReminderItem>>> GetUserRemindersAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _tableClient.QueryAsync<ReminderEntity>(
                    filter: $"PartitionKey eq '{userId}' and Status ne 'Cancelled'",
                    maxPerPage: 1000
                );

                var reminders = new List<ReminderItem>();

                await foreach (var entity in query)
                {
                    var reminder = entity.ToReminderItem();

                    // Tarih filtrelemesi
                    if (startDate.HasValue && reminder.Date < DateOnly.FromDateTime(startDate.Value.Date))
                        continue;

                    if (endDate.HasValue && reminder.Date > DateOnly.FromDateTime(endDate.Value.Date))
                        continue;

                    reminders.Add(reminder);
                }

                // Tarihe göre sırala
                reminders = reminders.OrderBy(r => r.Date).ThenBy(r => r.Time).ToList();

                return ServiceResponse<List<ReminderItem>>.Success(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı hatırlatıcıları getirilirken hata: {UserId}", userId);
                return ServiceResponse<List<ReminderItem>>.Error("Hatırlatıcılar getirilemedi");
            }
        }

        public async Task<ServiceResponse<List<ReminderItem>>> GetRemindersByDateAsync(string userId, DateOnly date)
        {
            try
            {
                var dateString = date.ToString("yyyy-MM-dd");
                var query = _tableClient.QueryAsync<ReminderEntity>(
                    filter: $"PartitionKey eq '{userId}' and Date eq '{dateString}' and Status ne 'Cancelled'",
                    maxPerPage: 100
                );

                var reminders = new List<ReminderItem>();

                await foreach (var entity in query)
                {
                    reminders.Add(entity.ToReminderItem());
                }

                // Saate göre sırala
                reminders = reminders.OrderBy(r => r.Time).ToList();

                return ServiceResponse<List<ReminderItem>>.Success(reminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tarih hatırlatıcıları getirilirken hata: {UserId} - {Date}", userId, date);
                return ServiceResponse<List<ReminderItem>>.Error("Hatırlatıcılar getirilemedi");
            }
        }

        public async Task<ServiceResponse<ReminderItem>> UpdateReminderAsync(Guid id, UpdateReminderDto dto, string userId)
        {
            try
            {
                // Önce mevcut hatırlatıcıyı al
                var existingResponse = await GetReminderAsync(id, userId);
                if (!existingResponse.IsSuccess || existingResponse.Data == null)
                {
                    return ServiceResponse<ReminderItem>.Error("Hatırlatıcı bulunamadı");
                }

                var reminder = existingResponse.Data;

                // Güncelle
                reminder.Title = dto.Title;
                reminder.Description = dto.Description;
                reminder.Date = dto.Date;
                reminder.Time = dto.Time;
                reminder.EmailReminder = dto.EmailReminder;
                reminder.IsCompleted = dto.IsCompleted;
                reminder.UpdatedAt = DateTime.UtcNow;

                if (dto.IsCompleted && !reminder.IsCompleted)
                {
                    reminder.CompletedAt = DateTime.UtcNow;
                    reminder.Status = ReminderStatus.Completed;
                }

                var entity = new ReminderEntity(reminder, userId);

                await _tableClient.UpdateEntityAsync(entity, Azure.ETag.All);

                _logger.LogInformation("Hatırlatıcı güncellendi: {ReminderId}", id);

                return ServiceResponse<ReminderItem>.Success(reminder, "Hatırlatıcı başarıyla güncellendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcı güncellenirken hata: {ReminderId}", id);
                return ServiceResponse<ReminderItem>.Error("Hatırlatıcı güncellenemedi");
            }
        }

        public async Task<ServiceResponse<bool>> DeleteReminderAsync(Guid id, string userId)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(userId, id.ToString());

                _logger.LogInformation("Hatırlatıcı silindi: {ReminderId}", id);

                return ServiceResponse<bool>.Success(true, "Hatırlatıcı başarıyla silindi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcı silinirken hata: {ReminderId}", id);
                return ServiceResponse<bool>.Error("Hatırlatıcı silinemedi");
            }
        }

        public async Task<ServiceResponse<bool>> ToggleReminderAsync(Guid id, string userId)
        {
            try
            {
                // Mevcut hatırlatıcıyı al
                var existingResponse = await GetReminderAsync(id, userId);
                if (!existingResponse.IsSuccess || existingResponse.Data == null)
                {
                    return ServiceResponse<bool>.Error("Hatırlatıcı bulunamadı");
                }

                var reminder = existingResponse.Data;
                reminder.IsCompleted = !reminder.IsCompleted;
                reminder.UpdatedAt = DateTime.UtcNow;

                if (reminder.IsCompleted)
                {
                    reminder.CompletedAt = DateTime.UtcNow;
                    reminder.Status = ReminderStatus.Completed;
                }
                else
                {
                    reminder.CompletedAt = null;
                    reminder.Status = ReminderStatus.Active;
                }

                var entity = new ReminderEntity(reminder, userId);
                await _tableClient.UpdateEntityAsync(entity, Azure.ETag.All);

                _logger.LogInformation("Hatırlatıcı durumu değiştirildi: {ReminderId} - {IsCompleted}", id, reminder.IsCompleted);

                return ServiceResponse<bool>.Success(true, reminder.IsCompleted ? "Hatırlatıcı tamamlandı" : "Hatırlatıcı aktif edildi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hatırlatıcı durumu değiştirilirken hata: {ReminderId}", id);
                return ServiceResponse<bool>.Error("İşlem gerçekleştirilemedi");
            }
        }

        public async Task<ServiceResponse<PagedResult<ReminderItem>>> GetRemindersPagedAsync(ReminderFilter filter)
        {
            try
            {
                var queryFilter = $"PartitionKey eq '{filter.UserId}' and Status ne 'Cancelled'";

                // Ek filtreler
                if (filter.IsCompleted.HasValue)
                {
                    queryFilter += $" and IsCompleted eq {filter.IsCompleted.Value.ToString().ToLower()}";
                }

                if (filter.EmailReminder.HasValue)
                {
                    queryFilter += $" and EmailReminder eq {filter.EmailReminder.Value.ToString().ToLower()}";
                }

                var query = _tableClient.QueryAsync<ReminderEntity>(
                    filter: queryFilter,
                    maxPerPage: filter.PageSize
                );

                var allReminders = new List<ReminderItem>();

                await foreach (var entity in query)
                {
                    var reminder = entity.ToReminderItem();

                    // Tarih filtreleme
                    if (filter.StartDate.HasValue && reminder.Date < DateOnly.FromDateTime(filter.StartDate.Value.Date))
                        continue;

                    if (filter.EndDate.HasValue && reminder.Date > DateOnly.FromDateTime(filter.EndDate.Value.Date))
                        continue;

                    // Arama terimi filtreleme
                    if (!string.IsNullOrEmpty(filter.SearchTerm))
                    {
                        var searchTerm = filter.SearchTerm.ToLower();
                        if (!reminder.Title.ToLower().Contains(searchTerm) &&
                            !(reminder.Description?.ToLower().Contains(searchTerm) ?? false))
                            continue;
                    }

                    allReminders.Add(reminder);
                }

                // Sayfalama
                var totalCount = allReminders.Count;
                var pagedReminders = allReminders
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToList();

                var result = new PagedResult<ReminderItem>
                {
                    Items = pagedReminders,
                    TotalCount = totalCount,
                    PageNumber = filter.PageNumber,
                    PageSize = filter.PageSize
                };

                return ServiceResponse<PagedResult<ReminderItem>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sayfalanmış hatırlatıcılar getirilirken hata");
                return ServiceResponse<PagedResult<ReminderItem>>.Error("Hatırlatıcılar getirilemedi");
            }
        }

        public async Task<ServiceResponse<List<ReminderItem>>> GetPendingEmailRemindersAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var currentTime = now.ToString("HH:mm");
                var currentDate = DateOnly.FromDateTime(now).ToString("yyyy-MM-dd");

                // Bugün ve henüz email gönderilmemiş hatırlatıcılar
                var query = _tableClient.QueryAsync<ReminderEntity>(
                    filter: $"Date eq '{currentDate}' and EmailReminder eq true and EmailSent eq false and Status eq 'Active'",
                    maxPerPage: 1000
                );

                var pendingReminders = new List<ReminderItem>();

                await foreach (var entity in query)
                {
                    var reminder = entity.ToReminderItem();

                    // Zamanı kontrol et (şu anki saat >= hatırlatıcı saati)
                    var reminderDateTime = reminder.Date.ToDateTime(reminder.Time);
                    if (now >= reminderDateTime)
                    {
                        pendingReminders.Add(reminder);
                    }
                }

                return ServiceResponse<List<ReminderItem>>.Success(pendingReminders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen email hatırlatıcıları getirilirken hata");
                return ServiceResponse<List<ReminderItem>>.Error("Email hatırlatıcıları getirilemedi");
            }
        }

        public async Task<ServiceResponse<bool>> MarkEmailAsSentAsync(Guid id)
        {
            try
            {
                // Hatırlatıcıyı bul
                var query = _tableClient.QueryAsync<ReminderEntity>(
                    filter: $"RowKey eq '{id}'",
                    maxPerPage: 1
                );

                await foreach (var entity in query)
                {
                    entity.EmailSent = true;
                    entity.EmailSentAt = DateTime.UtcNow;

                    await _tableClient.UpdateEntityAsync(entity, Azure.ETag.All);

                    _logger.LogInformation("Email gönderildi olarak işaretlendi: {ReminderId}", id);
                    return ServiceResponse<bool>.Success(true);
                }

                return ServiceResponse<bool>.Error("Hatırlatıcı bulunamadı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email durumu güncellenirken hata: {ReminderId}", id);
                return ServiceResponse<bool>.Error("Email durumu güncellenemedi");
            }
        }
    }
}
