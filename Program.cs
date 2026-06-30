using KRD.AttendanceWeb.Database;
using KRD.AttendanceWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Blazor + Interactive Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// EF Core SQLite — configuration is handled in AppDbContext.OnConfiguring
builder.Services.AddDbContext<AppDbContext>();

// Auth session
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AttendanceService>();

// Session support
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Init DB
DatabaseHelper.InitializeDatabase(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<KRD.AttendanceWeb.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
