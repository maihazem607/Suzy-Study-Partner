using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Suzy.Data;
using Suzy.Services;
using System.Net;

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

// ✅ Kestrel: Listen on all network interfaces (localhost + LAN)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000); // HTTP
    // Optional: HTTPS
    // serverOptions.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps());
});

// ✅ Register app-specific services
builder.Services.AddRazorPages();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddHttpClient();
builder.Services.AddAntiforgery();

var app = builder.Build();

// Ensure DB is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
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

// ✅ Optional Debug: Show LAN IP
Console.WriteLine($"✅ Access your app from other devices using: http://{GetLocalIPAddress()}:5000");

// Helper method
static string GetLocalIPAddress()
{
    var host = Dns.GetHostEntry(Dns.GetHostName());
    foreach (var ip in host.AddressList)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            return ip.ToString();
    }
    return "localhost";
}
