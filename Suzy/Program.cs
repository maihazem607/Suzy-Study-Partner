using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Services;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore; 
var builder = WebApplication.CreateBuilder(args);

// --- Add services to the container. ---

// Connection String (safer retrieval)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Add EF Core + Identity Services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Useful for getting detailed database errors in development
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// Kestrel: Configure for HTTPS and HTTP
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000, listenOptions => listenOptions.UseHttps());
    serverOptions.ListenAnyIP(5001);
});

// Register app-specific services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddAntiforgery();

// Register your custom services here (no duplicates needed)
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<ChatAnalyticsService>();


var app = builder.Build();

// --- Configure the HTTP request pipeline. ---

// Ensure DB migrations are applied on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
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
app.MapControllers();

app.Run();