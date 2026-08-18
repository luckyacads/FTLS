using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq; // Added for LINQ support

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<FtlsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/LoginPage", "");
});

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    // Effectively prevents inactivity from logging the user out.
    // The browser session cookie will still disappear when the browser closes.
    options.IdleTimeout = TimeSpan.FromDays(365);

    options.Cookie.Name = ".FTLS.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- NEONDB QUICK CONNECTION TEST & SEEDING ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FtlsDbContext>();
    try
    {
        if (dbContext.Database.CanConnect())
        {
            System.Diagnostics.Debug.WriteLine("\nSUCCESS: Successfully connected to NeonDB!\n");
            DbSeeder.Seed(dbContext);
        }
    }
    catch (System.Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"\nERROR: {ex.Message}\n");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "-1";
    await next();
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

#region SUBJECTS API
app.MapGet("/api/subjects", async (string? code, string? title, FtlsDbContext db) =>
{
    var query = db.Subjects.AsQueryable();
    if (!string.IsNullOrEmpty(code)) query = query.Where(s => s.Code.Contains(code));
    if (!string.IsNullOrEmpty(title)) query = query.Where(s => s.Title.Contains(title));
    return Results.Ok(await query.ToListAsync());
})
.WithName("GetAllSubjects").WithTags("Subjects");

app.MapGet("/api/subjects/{id}", async (int id, FtlsDbContext db) =>
{
    var subject = await db.Subjects.FindAsync(id);
    return subject is not null ? Results.Ok(subject) : Results.NotFound();
})
.WithName("GetSubjectById").WithTags("Subjects");

app.MapPost("/api/subjects", async (Subject subject, FtlsDbContext db) =>
{
    if (subject == null) return Results.BadRequest();
    if (await db.Subjects.AnyAsync(s => s.Code == subject.Code))
        return Results.Conflict($"Subject code '{subject.Code}' already exists.");

    db.Subjects.Add(subject);
    await db.SaveChangesAsync();
    return Results.Created($"/api/subjects/{subject.SubjectId}", subject);
}).WithName("CreateSubject").WithTags("Subjects");

app.MapPut("/api/subjects/{id}", async (int id, Subject updatedSubject, FtlsDbContext db) =>
{
    var subject = await db.Subjects.FindAsync(id);
    if (subject == null) return Results.NotFound();
    subject.Code = updatedSubject.Code;
    subject.Title = updatedSubject.Title;
    subject.Units = updatedSubject.Units;
    db.Subjects.Update(subject);
    await db.SaveChangesAsync();
    return Results.Ok(subject);
}).WithName("UpdateSubject").WithTags("Subjects");

app.MapDelete("/api/subjects/{id}", async (int id, FtlsDbContext db) =>
{
    var subject = await db.Subjects.FindAsync(id);
    if (subject == null) return Results.NotFound();
    db.Subjects.Remove(subject);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteSubject").WithTags("Subjects");
#endregion

#region USERS API
app.MapGet("/api/users", async (string? lastName, int? facultyId, FtlsDbContext db) =>
{
    var query = db.Users.AsQueryable();
    if (!string.IsNullOrEmpty(lastName)) query = query.Where(u => u.LastName.Contains(lastName));
    if (facultyId.HasValue) query = query.Where(u => u.FacultyId == facultyId.Value);
    return Results.Ok(await query.ToListAsync());
})
.WithName("GetAllUsers").WithTags("Users");

app.MapGet("/api/users/{id}", async (int id, FtlsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
})
.WithName("GetUserById").WithTags("Users");

app.MapPost("/api/users", async (User user, FtlsDbContext db) =>
{
    if (user == null) return Results.BadRequest();
    if (await db.Users.AnyAsync(u => u.FacultyId == user.FacultyId))
        return Results.Conflict($"Faculty ID '{user.FacultyId}' already exists.");

    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.FacultyId}", user);
}).WithName("CreateUser").WithTags("Users");

app.MapPut("/api/users/{id}", async (int id, User updatedUser, FtlsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    user.FirstName = updatedUser.FirstName;
    user.LastName = updatedUser.LastName;
    db.Users.Update(user);
    await db.SaveChangesAsync();
    return Results.Ok(user);
}).WithName("UpdateUser").WithTags("Users");

app.MapDelete("/api/users/{id}", async (int id, FtlsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    db.Users.Remove(user);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteUser").WithTags("Users");
#endregion

// ... (Keep the rest of your API regions exactly as you have them, they are correct!)

app.MapRazorPages();
app.Run();