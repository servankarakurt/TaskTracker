using System.ComponentModel.DataAnnotations;

namespace GorevTakipUygulamasi.Models
{
    public class ReminderItem
    {
        public Guid Id { get; set; }

        [Required, StringLength(100)]
        public string Title { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly Time { get; set; }

        public DateTime ReminderDate
        {
            get => Date.ToDateTime(Time);
            set
            {
                Date = DateOnly.FromDateTime(value);
                Time = TimeOnly.FromDateTime(value);
            }
        }

        public DateTime ReminderTime => ReminderDate;

        public bool EmailReminder { get; set; } = true;
        public bool IsCompleted { get; set; } = false;
        public bool IsSent { get; set; } = false;
        public string UserId { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    }

    public enum ReminderStatus { Active, Completed, Cancelled, Expired }

    public class CreateReminderDto
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(500)] public string? Description { get; set; }
        [Required] public DateOnly Date { get; set; }
        [Required] public TimeOnly Time { get; set; }
        public bool EmailReminder { get; set; } = true;
    }

    public class UpdateReminderDto
    {
        [Required, StringLength(100)] public string Title { get; set; } = "";
        [StringLength(500)] public string? Description { get; set; }
        [Required] public DateOnly Date { get; set; }
        [Required] public TimeOnly Time { get; set; }
        public bool EmailReminder { get; set; } = true;
        public bool IsCompleted { get; set; } = false;
    }

    public class ServiceResponse<T>
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ServiceResponse<T> Success(T data, string message = "İşlem başarılı") =>
            new() { IsSuccess = true, Message = message, Data = data };

        public static ServiceResponse<T> Error(string message, List<string>? errors = null) =>
            new() { IsSuccess = false, Message = message, Errors = errors ?? new List<string>() };
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class ReminderFilter
    {
        public string? UserId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? EmailReminder { get; set; }
        public ReminderStatus? Status { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
