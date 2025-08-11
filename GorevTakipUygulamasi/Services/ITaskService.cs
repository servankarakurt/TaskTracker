using GorevTakipUygulamasi.Models;

namespace GorevTakipUygulamasi.Services
{
    public interface ITaskService
    {
        // Kullanıcının tüm görevlerini getir
        Task<List<TaskItem>> GetUserTasksAsync(string userId);

        // Tüm görevleri getir (admin/genel liste için)
        Task<List<TaskItem>> GetAllTasksAsync();

        // Tüm görevleri getir (kullanıcı parametreli versiyon)
        Task<List<TaskItem>> GetAllTasksAsync(string userId);

        // ID'ye göre görev getir
        Task<TaskItem?> GetTaskByIdAsync(int id, string userId);

        // Yeni görev oluştur
        Task<TaskItem> CreateTaskAsync(TaskItem task);

        // Görev güncelle
        Task<TaskItem?> UpdateTaskAsync(TaskItem task);

        // Görev sil
        Task<bool> DeleteTaskAsync(int id, string userId);

        // Görev durumunu değiştir
        Task<bool> ChangeTaskStatusAsync(int id, string userId, Models.TaskStatus newStatus);

        // Görevi tamamlandı olarak işaretle (basit versiyon)
        Task<bool> MarkTaskAsCompletedAsync(int id);

        // Görevi tamamlandı olarak işaretle (kullanıcı parametreli versiyon)
        Task<bool> MarkTaskAsCompletedAsync(int id, string userId);

        // Kullanıcının tamamlanmış görev sayısı
        Task<int> GetCompletedTaskCountAsync(string userId);

        // Kullanıcının bekleyen görev sayısı
        Task<int> GetPendingTaskCountAsync(string userId);
    }
}