using Azure.Data.Tables;
using GorevTakipUygulamasi.Areas.Identity;
using GorevTakipUygulamasi.Configuration;
using GorevTakipUygulamasi.Data;
using GorevTakipUygulamasi.Services;
using GorevTakipUygulamasi.Services.Background;
using GorevTakipUygulamasi.Services.LogicApp;
using GorevTakipUygulamasi.Services.TaskServices;
using GorevTakipUygulamasi.Services.User;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Blazor Services
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Authentication State Provider
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

// Configuration Settings
builder.Services.Configure<AzureStorageSettings>(
    builder.Configuration.GetSection("AzureStorage"));
builder.Services.Configure<LogicAppSettings>(
    builder.Configuration.GetSection("LogicApp"));
builder.Services.Configure<ReminderNotificationSettings>(
    builder.Configuration.GetSection("ReminderNotification"));

// Azure Table Storage
var storageConnectionString = builder.Configuration.GetConnectionString("AzureStorage");
if (!string.IsNullOrEmpty(storageConnectionString))
{
    builder.Services.AddSingleton(new TableServiceClient(storageConnectionString));
}
else
{
    // Development için local storage emulator
    builder.Services.AddSingleton(new TableServiceClient("UseDevelopmentStorage=true"));
}

// HttpClient for Logic Apps
builder.Services.AddHttpClient<ILogicAppService, LogicAppService>();
builder.Services.AddHttpClient<ReminderNotificationService>();

// Application Services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILogicAppService, LogicAppService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReminderNotificationService>();
builder.Services.AddScoped<IReminderCheckService, ReminderCheckService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// Background Services
builder.Services.AddHostedService<BackgroundWorkerService>();

// Logging
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapRazorPages();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();

// Background Worker Service Class
public class BackgroundWorkerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundWorkerService> _logger;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Her 5 dakikada bir çalýþ

    public BackgroundWorkerService(IServiceProvider serviceProvider, ILogger<BackgroundWorkerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var reminderCheckService = scope.ServiceProvider.GetRequiredService<IReminderCheckService>();

                await reminderCheckService.CheckAndProcessRemindersAsync();

                _logger.LogInformation("Hatýrlatýcý kontrol döngüsü tamamlandý: {Time}", DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background service hatasý: {Error}", ex.Message);
            }

            await Task.Delay(_period, stoppingToken);
        }
    }
}