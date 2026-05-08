using FTLSV2.Data;
using FTLSV2.Models;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<FtlsDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AddPageRoute("/LoginPage", "");
});
builder.Services.AddSession();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// --- NEONDB QUICK CONNECTION TEST ---
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<FtlsDbContext>();
    try
    {
        if (dbContext.Database.CanConnect())
        {
            System.Diagnostics.Debug.WriteLine("\nSUCCESS: Successfully connected to NeonDB!\n");
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

// ==========================================
// --- ADDED: ANTI-CACHING MIDDLEWARE ---
// ==========================================
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "-1";
    await next();
});
// ==========================================

app.UseRouting();
app.UseSession();
app.UseAuthorization();

#region SUBJECTS API
app.MapGet("/api/subjects", async (FtlsDbContext db) => Results.Ok(await db.Subjects.ToListAsync()))
    .WithName("GetAllSubjects").WithTags("Subjects");

app.MapPost("/api/subjects", async (Subject subject, FtlsDbContext db) =>
{
    if (subject == null) return Results.BadRequest();
    if (await db.Subjects.AnyAsync(s => s.Code == subject.Code))
        return Results.Conflict($"Subject code '{subject.Code}' already exists.");

    subject.SubjectId = 0;
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
app.MapGet("/api/users", async (FtlsDbContext db) => Results.Ok(await db.Users.ToListAsync()))
    .WithName("GetAllUsers").WithTags("Users");

app.MapPost("/api/users", async (User user, FtlsDbContext db) =>
{
    if (user == null) return Results.BadRequest();
    if (await db.Users.AnyAsync(u => u.FacultyId == user.FacultyId))
        return Results.Conflict($"Faculty ID '{user.FacultyId}' already exists.");

    user.Id = 0;
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Created($"/api/users/{user.Id}", user);
}).WithName("CreateUser").WithTags("Users");

app.MapPut("/api/users/{id}", async (int id, User updatedUser, FtlsDbContext db) =>
{
    var user = await db.Users.FindAsync(id);
    if (user == null) return Results.NotFound();
    user.FacultyId = updatedUser.FacultyId;
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

#region ROOMS API
app.MapGet("/api/rooms", async (FtlsDbContext db) => Results.Ok(await db.Rooms.ToListAsync()))
    .WithName("GetAllRooms").WithTags("Rooms");

app.MapPost("/api/rooms", async (Room room, FtlsDbContext db) =>
{
    if (room == null) return Results.BadRequest();
    if (await db.Rooms.AnyAsync(r => r.Name == room.Name))
        return Results.Conflict($"Room '{room.Name}' already exists.");

    room.RoomId = 0;
    db.Rooms.Add(room);
    await db.SaveChangesAsync();
    return Results.Created($"/api/rooms/{room.RoomId}", room);
}).WithName("CreateRoom").WithTags("Rooms");

app.MapPut("/api/rooms/{id}", async (int id, Room updatedRoom, FtlsDbContext db) =>
{
    var room = await db.Rooms.FindAsync(id);
    if (room == null) return Results.NotFound();
    room.Name = updatedRoom.Name;
    room.Capacity = updatedRoom.Capacity;
    room.Type = updatedRoom.Type;
    db.Rooms.Update(room);
    await db.SaveChangesAsync();
    return Results.Ok(room);
}).WithName("UpdateRoom").WithTags("Rooms");

app.MapDelete("/api/rooms/{id}", async (int id, FtlsDbContext db) =>
{
    var room = await db.Rooms.FindAsync(id);
    if (room == null) return Results.NotFound();
    db.Rooms.Remove(room);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteRoom").WithTags("Rooms");
#endregion

#region SCHOOLS API
app.MapGet("/api/schools", async (FtlsDbContext db) => Results.Ok(await db.Schools.ToListAsync()))
    .WithName("GetAllSchools").WithTags("Schools");

app.MapPost("/api/schools", async (School school, FtlsDbContext db) =>
{
    if (school == null) return Results.BadRequest();
    if (await db.Schools.AnyAsync(s => s.Code == school.Code))
        return Results.Conflict($"School code '{school.Code}' already exists.");

    school.Id = 0;
    db.Schools.Add(school);
    await db.SaveChangesAsync();
    return Results.Created($"/api/schools/{school.Id}", school);
}).WithName("CreateSchool").WithTags("Schools");

app.MapPut("/api/schools/{id}", async (int id, School updatedSchool, FtlsDbContext db) =>
{
    var school = await db.Schools.FindAsync(id);
    if (school == null) return Results.NotFound();
    school.Name = updatedSchool.Name;
    school.Code = updatedSchool.Code;
    db.Schools.Update(school);
    await db.SaveChangesAsync();
    return Results.Ok(school);
}).WithName("UpdateSchool").WithTags("Schools");

app.MapDelete("/api/schools/{id}", async (int id, FtlsDbContext db) =>
{
    var school = await db.Schools.FindAsync(id);
    if (school == null) return Results.NotFound();
    db.Schools.Remove(school);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteSchool").WithTags("Schools");
#endregion

#region DEPARTMENTS API
app.MapGet("/api/departments", async (FtlsDbContext db) => Results.Ok(await db.Departments.ToListAsync()))
    .WithName("GetAllDepartments").WithTags("Departments");

app.MapPost("/api/departments", async (Department department, FtlsDbContext db) =>
{
    if (department == null) return Results.BadRequest();
    if (await db.Departments.AnyAsync(d => d.Code == department.Code))
        return Results.Conflict($"A department with code '{department.Code}' already exists.");

    department.Id = 0;
    db.Departments.Add(department);
    await db.SaveChangesAsync();
    return Results.Created($"/api/departments/{department.Id}", department);
}).WithName("CreateDepartment").WithTags("Departments");

app.MapPut("/api/departments/{id}", async (int id, Department updatedDept, FtlsDbContext db) =>
{
    var dept = await db.Departments.FindAsync(id);
    if (dept == null) return Results.NotFound();
    dept.Name = updatedDept.Name;
    dept.Code = updatedDept.Code;
    db.Departments.Update(dept);
    await db.SaveChangesAsync();
    return Results.Ok(dept);
}).WithName("UpdateDepartment").WithTags("Departments");

app.MapDelete("/api/departments/{id}", async (int id, FtlsDbContext db) =>
{
    var department = await db.Departments.FindAsync(id);
    if (department == null) return Results.NotFound();
    db.Departments.Remove(department);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithName("DeleteDepartment").WithTags("Departments");
#endregion

app.MapRazorPages();
app.Run();