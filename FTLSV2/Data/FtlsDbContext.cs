using Microsoft.EntityFrameworkCore;
using FTLSV2.Models;

namespace FTLSV2.Data
{
    public class FtlsDbContext : DbContext
    {
        public FtlsDbContext(DbContextOptions<FtlsDbContext> options) : base(options)
        {
        }

        // This tells Entity Framework to link the User model to the 'users' table
        public DbSet<User> Users { get; set; }
    }
}