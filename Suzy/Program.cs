using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Services;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Add services to the container. ---

// Connection String (safer retrieval)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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

// ✅ Kestrel: Allow access from other devices on HTTP (port 5000)
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// Register app-specific services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddAntiforgery();

// Register custom services
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<ChatAnalyticsService>();

var app = builder.Build();

// --- Configure the HTTP request pipeline ---

// Apply pending EF migrations on startup
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

// Redirect HTTPS requests to HTTP (only relevant if HTTPS enabled, here it’s skipped)
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
