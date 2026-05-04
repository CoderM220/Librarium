using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});
Directory.CreateDirectory("/app/keys");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("LibrariumApp");


builder.Services.AddDbContext<Librarium.Models.LibrariumDbContext>(options =>
    options.UseSqlite("Data Source=librarium.db"));



// ── SERVICES ──
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<Librarium.Services.EmailService>();
builder.Services.AddScoped<Librarium.Services.PushNotificationService>();
builder.Services.AddHostedService<Librarium.Services.ReminderService>();

var app = builder.Build();

// ── MIDDLEWARE PIPELINE ──
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Auth/AdminLogin");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto
    });
}
app.UseStaticFiles();

app.UseRouting();

// ✅ Session MUST come before Authorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=AdminLogin}/{id?}");

// ── SEED DATABASE ──
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Librarium.Models.LibrariumDbContext>();
    Librarium.Models.DbInitializer.Initialize(context);
}

app.Run();
