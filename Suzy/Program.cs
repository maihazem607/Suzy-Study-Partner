using Suzy.Services;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// ✅ Use ListenAnyIP — this will bind to ALL available network interfaces (safe & stable)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000); // HTTP
    // Optional: HTTPS if needed
    // serverOptions.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps());
});

// Services
builder.Services.AddRazorPages();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddHttpClient();
builder.Services.AddAntiforgery();

var app = builder.Build();

// Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();

// Optional: Show IP in terminal
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
