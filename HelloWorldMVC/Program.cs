using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HelloWorldMVC.Data;
using HelloWorldMVC.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Configuration;


namespace HelloWorldMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            // Apply migrations and seed initial data
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    context.Database.Migrate();
                    
                    // Seed "Hello World" message if database is empty
                    if (!context.Messages.Any())
                    {
                        context.Messages.Add(new Message { Text = "Hello World" });
                        context.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating or seeding the database.");
                }
            }
            host.Run();
        }

       public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            var builtConfig = config.Build();
            var keyVaultUri = builtConfig["KeyVault:VaultUri"];
            var keyVaultEnabled = builtConfig.GetValue("KeyVault:Enabled", defaultValue: false);

            // Key Vault should be explicitly enabled (so local dev doesn't require Azure credentials).
            if (keyVaultEnabled && !string.IsNullOrWhiteSpace(keyVaultUri))
            {
                config.AddAzureKeyVault(
                    new Uri(keyVaultUri),
                    new DefaultAzureCredential()
                );
            }
        })
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });

    }
}
