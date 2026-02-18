var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//REPLACE THE OLD LINE WITH THIS BLOCK
builder.Services.AddRazorPages(options =>
{
    // This tells the app: "When the website starts (root URL), show LoginPage"
    options.Conventions.AddPageRoute("/LoginPage", "");
});

var app = builder.Build();

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