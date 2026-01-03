using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HelloWorldMVC.Data;
using Microsoft.EntityFrameworkCore;
using HelloWorldMVC.Services;
using System.Threading.Channels;

namespace HelloWorldMVC
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite("Data Source=HelloWorld.db"));

            // In-memory "queue + store" for demo purposes (later replaced by Azure Queue/ServiceBus + DB).
            services.AddSingleton<InMemoryJobStore>();
            services.AddSingleton(Channel.CreateUnbounded<ImageJob>());
            services.AddHostedService<ImageJobDispatcher>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Images}/{action=Upload}/{id?}");
            });
        }
    }
}

