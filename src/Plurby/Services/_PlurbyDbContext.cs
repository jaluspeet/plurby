using Microsoft.EntityFrameworkCore;
using Plurby.Infrastructure;
using Plurby.Services.Shared;

namespace Plurby.Services
{
    public class PlurbyDbContext : DbContext
    {
        public PlurbyDbContext()
        {
        }

        public PlurbyDbContext(DbContextOptions<PlurbyDbContext> options) : base(options)
        {
            DataGenerator.InitializeUsers(this);
        }

        public DbSet<User> Users { get; set; }
    }
}
