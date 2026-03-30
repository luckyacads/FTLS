using FTLSV2.Data;
using Microsoft.EntityFrameworkCore;
using System; // Added to ensure Exception is recognized

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FtlsDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // This tells the app: "When the website starts (root URL), show LoginPage"
    options.Conventions.AddPageRoute("/LoginPage", "");
});

var app = builder.Build();

// ==========================================
// --- NEONDB QUICK CONNECTION TEST ---
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FtlsDbContext>();
    try
    {
        // This physically attempts to reach your NeonDB
        if (dbContext.Database.CanConnect())
        {
            System.Diagnostics.Debug.WriteLine("\n===========================================");
            System.Diagnostics.Debug.WriteLine("SUCCESS: Successfully connected to NeonDB!");
            System.Diagnostics.Debug.WriteLine("===========================================\n");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("\nFAILED: Could not connect to NeonDB.\n");
        }
    }
    catch (System.Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"\nERROR: {ex.Message}\n");
    }
}
// ==========================================

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();