using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MVC and database services
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("HelloWorldDB"));

var app = builder.Build();

// Seed a message into the in-memory database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Messages.Add(new Message { Text = "Hello World" });
    context.SaveChanges();
}

// Configure middleware
app.UseStaticFiles();
app.UseRouting();
app.MapDefaultControllerRoute();

app.Run();
