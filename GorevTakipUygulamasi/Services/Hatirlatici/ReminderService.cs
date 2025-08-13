using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GorevTakipUygulamasi.Models;
using GorevTakipUygulamasi.Services.Hatirlatici;

namespace GorevTakipUygulamasi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReminderController : ControllerBase
    {
        private readonly IReminderService _reminderService;
        private readonly ILogger<ReminderController> _logger;

        public ReminderController(
            IReminderService reminderService,
            ILogger<ReminderController> logger)
        {
            _reminderService = reminderService ?? throw new ArgumentNullException(nameof(reminderService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateReminder([FromBody] CreateReminderDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _reminderService.CreateReminderAsync(dto, userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReminder(Guid id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _reminderService.GetReminderAsync(id, userId);
            if (result.IsSuccess)
                return Ok(result);

            return NotFound(result);
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserReminders([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _reminderService.GetUserRemindersAsync(userId, startDate, endDate);
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReminder(Guid id, [FromBody] UpdateReminderDto dto)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _reminderService.UpdateReminderAsync(id, dto, userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReminder(Guid id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _reminderService.DeleteReminderAsync(id, userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }

        [HttpPost("{id}/toggle")]
        public async Task<IActionResult> ToggleReminder(Guid id)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var result = await _reminderService.ToggleReminderAsync(id, userId);
            if (result.IsSuccess)
                return Ok(result);

            return BadRequest(result);
        }
    }
}
