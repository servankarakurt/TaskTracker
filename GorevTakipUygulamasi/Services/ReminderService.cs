using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GorevTakipUygulamasi.Services;
using GorevTakipUygulamasi.Services.LogicApp;
using GorevTakipUygulamasi.Models;
using System.Security.Claims;

namespace GorevTakipUygulamasi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TestReminderController : ControllerBase
    {
        private readonly IReminderService _reminderService;
        private readonly ILogicAppService _logicAppService;
        private readonly ILogger<TestReminderController> _logger;

        public TestReminderController(
            IReminderService reminderService,
            ILogicAppService logicAppService,
            ILogger<TestReminderController> logger)
        {
            _reminderService = reminderService;
            _logicAppService = logicAppService;
            _logger = logger;
        }

        [HttpPost("test-connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var result = await _logicAppService.TestConnectionAsync();
                return Ok(new { success = result, message = result ? "Bağlantı başarılı" : "Bağlantı başarısız" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test connection error");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("create-test-reminder")]
        public async Task<IActionResult> CreateTestReminder()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var testReminder = new CreateReminderDto
                {
                    Title = "Test Hatırlatıcısı",
                    Description = "Bu bir test hatırlatıcısıdır",
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    Time = TimeOnly.FromDateTime(DateTime.Now.AddMinutes(2)), // 2 dakika sonra
                    EmailReminder = true
                };

                var result = await _reminderService.CreateReminderAsync(testReminder, userId);

                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Test hatırlatıcısı oluşturuldu",
                        reminderId = result.Data?.Id,
                        scheduledTime = result.Data?.ReminderDate
                    });
                }
                else
                {
                    return BadRequest(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Create test reminder error");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("send-immediate-email")]
        public async Task<IActionResult> SendImmediateEmail([FromBody] ImmediateEmailRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var notification = new ReminderNotificationDto
                {
                    ReminderId = Guid.NewGuid().ToString(),
                    UserId = userId,
                    UserEmail = request.Email,
                    Title = request.Title ?? "Test Email",
                    Description = request.Description ?? "Bu bir test emailidir",
                    ScheduledDateTime = DateTime.Now,
                    NotificationType = "Email"
                };

                var result = await _logicAppService.SendImmediateEmailAsync(notification);

                return Ok(new
                {
                    success = result,
                    message = result ? "Email gönderildi" : "Email gönderilemedi"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Send immediate email error");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        public class ImmediateEmailRequest
        {
            public string Email { get; set; } = "";
            public string? Title { get; set; }
            public string? Description { get; set; }
        }
    }
}