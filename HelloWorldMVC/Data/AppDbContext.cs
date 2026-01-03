using Microsoft.EntityFrameworkCore;
using HelloWorldMVC.Models;

namespace HelloWorldMVC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Message> Messages { get; set; }
        public DbSet<Job> Jobs { get; set; }
    }
}
