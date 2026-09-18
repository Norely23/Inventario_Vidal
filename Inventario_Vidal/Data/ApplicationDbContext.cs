using Inventario_Vital.Models;
using Microsoft.EntityFrameworkCore;

namespace Inventario_Vital.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Material> Materiales { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Proyecto> Proyectos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Índices únicos (REQ-11, REQ-20, REQ-26)
            modelBuilder.Entity<Material>().HasIndex(m => m.CodigoBarras).IsUnique();
            modelBuilder.Entity<Proveedor>().HasIndex(p => p.RUC).IsUnique();
            modelBuilder.Entity<Proyecto>().HasIndex(p => p.Codigo).IsUnique();
        }
    }
}