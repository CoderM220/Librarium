using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// ✅ Ensure key folder exists (important for Render)
Directory.CreateDirectory("/app/keys");

// ✅ Data Protection (fix session cookie error)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("LibrariumApp");

// ── DATABASE ──
builder.Services.AddDbContext<Librarium.Models.LibrariumDbContext>(options =>
    options.UseSqlite("Data Source=librarium.db"));

// ── SESSION ──
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

app.UseHttpsRedirection();
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
