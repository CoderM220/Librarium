using Microsoft.EntityFrameworkCore;
using Librarium.Models;
var builder = WebApplication.CreateBuilder(args);

// ── DATABASE ──
builder.Services.AddDbContext<LibrariumDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
// ── SESSION ──
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<Librarium.Services.EmailService>();
builder.Services.AddHostedService<Librarium.Services.ReminderService>();
builder.Services.AddScoped<Librarium.Services.PushNotificationService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Auth/AdminLogin");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=AdminLogin}/{id?}");

// ── SEED DATABASE ON STARTUP ──
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<Librarium.Models.LibrariumDbContext>();
    Librarium.Models.DbInitializer.Initialize(context);
}
app.Run();
