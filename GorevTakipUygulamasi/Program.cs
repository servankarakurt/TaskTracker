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

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Azure Table Storage
builder.Services.Configure<AzureStorageSettings>(
    builder.Configuration.GetSection("AzureStorage"));

builder.Services.AddSingleton<TableServiceClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("AzureStorage").Get<AzureStorageSettings>();
    return new TableServiceClient(settings?.ConnectionString ?? "UseDevelopmentStorage=true");
});

// Logic App Settings
builder.Services.Configure<LogicAppSettings>(
    builder.Configuration.GetSection("LogicApp"));

builder.Services.Configure<ReminderNotificationSettings>(
    builder.Configuration.GetSection("ReminderNotification"));

// Services
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILogicAppService, LogicAppService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ReminderNotificationService>();
builder.Services.AddScoped<IReminderCheckService, ReminderCheckService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();

// HttpClient for LogicApp services
builder.Services.AddHttpClient<ILogicAppService, LogicAppService>();
builder.Services.AddHttpClient<ReminderNotificationService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();

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

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();