// NAMESPACE ADI DEĞİŞTİRİLDİ ve ITaskService doğru şekilde uygulandı.
namespace GorevTakipUygulamasi.Services.TaskServices
{
    using GorevTakipUygulamasi.Data;
    using GorevTakipUygulamasi.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System;
    using GorevTakipUygulamasi.Services.Notifications;

    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public TaskService(ApplicationDbContext context, NotificationService notificationService, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<List<TaskItem>> GetUserTasksAsync(string userId)
        {
            return await _context.TaskItems
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            return await _context.TaskItems
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
        }

        public async Task<TaskItem?> GetTaskByIdAsync(int id, string userId)
        {
            return await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
        }

        public async Task<TaskItem> CreateTaskAsync(TaskItem task)
        {
            task.CreatedDate = DateTime.Now;
            task.CompletedDate = null;

            _context.TaskItems.Add(task);
            await _context.SaveChangesAsync();
            return task;
        }

        public async Task<TaskItem?> UpdateTaskAsync(TaskItem task)
        {
            var existingTask = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == task.Id && t.UserId == task.UserId);

            if (existingTask == null)
                return null;

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.DueDate = task.DueDate;
            existingTask.Status = task.Status;

            if (task.Status == Models.TaskStatus.Tamamlandi && existingTask.CompletedDate == null)
            {
                existingTask.CompletedDate = DateTime.Now;
                await SendCompletionNotification(existingTask);
            }
            else if (task.Status != Models.TaskStatus.Tamamlandi)
            {
                existingTask.CompletedDate = null;
            }

            await _context.SaveChangesAsync();
            return existingTask;
        }

        public async Task<bool> DeleteTaskAsync(int id, string userId)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return false;

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeTaskStatusAsync(int id, string userId, Models.TaskStatus newStatus)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (task == null)
                return false;

            task.Status = newStatus;

            if (newStatus == Models.TaskStatus.Tamamlandi && task.CompletedDate == null)
            {
                task.CompletedDate = DateTime.Now;
                await SendCompletionNotification(task);
            }
            else if (newStatus != Models.TaskStatus.Tamamlandi)
            {
                task.CompletedDate = null;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetCompletedTaskCountAsync(string userId)
        {
            return await _context.TaskItems
                .CountAsync(t => t.UserId == userId && t.Status == Models.TaskStatus.Tamamlandi);
        }

        public async Task<int> GetPendingTaskCountAsync(string userId)
        {
            return await _context.TaskItems
                .CountAsync(t => t.UserId == userId && t.Status != Models.TaskStatus.Tamamlandi);
        }

        private async Task SendCompletionNotification(TaskItem task)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(task.UserId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    Console.WriteLine($"🔄 E-posta gönderiliyor: {user.Email}");
                    await _notificationService.SendTaskCompletionNotificationAsync(
                        task.Title,
                        task.Description ?? "",
                        user.Email
                    );
                }
                else
                {
                    Console.WriteLine($"⚠️ Kullanıcı bulunamadı veya e-posta boş. UserId: {task.UserId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 E-posta gönderme hatası: {ex.Message}");
            }
        }
    }
}
