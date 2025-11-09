using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC and database services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=HelloWorld.db"));

var app = builder.Build();

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    
    // Seed "Hello World" message if database is empty
    if (!context.Messages.Any())
    {
        context.Messages.Add(new Message { Text = "Hello World" });
        context.SaveChanges();
    }
}

// Configure middleware
app.UseStaticFiles();
app.UseRouting();
app.MapDefaultControllerRoute();

app.Run();
