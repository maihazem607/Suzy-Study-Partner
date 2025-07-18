using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Connection String (update in appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ✅ Add EF Core + Identity Services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login"; // redirect here if not logged in
});

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// ✅ Kestrel: Configure for HTTPS on localhost:5000
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenLocalhost(5000, listenOptions => listenOptions.UseHttps()); // HTTPS on localhost:5000
    serverOptions.ListenAnyIP(5001); // HTTP on any IP for development access
});

// ✅ Register app-specific services
builder.Services.AddRazorPages();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddHttpClient();
builder.Services.AddAntiforgery();

var app = builder.Build();

// Ensure DB migrations are applied
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}


// ✅ Middleware stack
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();            // Add this to serve CSS/JS/images
app.UseRouting();

app.UseAuthentication();         // ✅ Added for Identity
app.UseAuthorization();

app.MapRazorPages().WithStaticAssets();
app.MapStaticAssets();

app.Run();