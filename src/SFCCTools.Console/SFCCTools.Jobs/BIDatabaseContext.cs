using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Configuration;
using SFCCTools.OCAPI.ShopAPI.Types;

namespace SFCCTools.Jobs
{

    public class BIDatabaseContextFactory : IDesignTimeDbContextFactory<BIDatabaseContext>
    {
        public BIDatabaseContext CreateDbContext(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                            // Use the appsettings from the project root for migrations, etc
                            .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "appsettings.json"), optional: false, reloadOnChange: false)
                            .Build();
            var optionsBuilder = new DbContextOptionsBuilder<BIDatabaseContext>();
            optionsBuilder.UseNpgsql(configuration.GetConnectionString("Default"));
            return new BIDatabaseContext(optionsBuilder.Options);
        }
    }

    public class BIDatabaseContext : DbContext
    {
        static BIDatabaseContext()
        {
        }
        
        public BIDatabaseContext(DbContextOptions<BIDatabaseContext> options) : base(options) {}

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var orderModel = modelBuilder.Entity<Order>();
            orderModel.HasIndex(o => o.CustomerNo);
            
            orderModel.HasIndex(o => o.Status);
            orderModel.Property(e => e.Status).HasConversion(new EnumToStringConverter<OrderStatus>());
            
            orderModel.HasIndex(o => o.CreationDate);
            orderModel.HasIndex(o => o.ShippingMethod);
            orderModel.HasIndex(o => o.ShippingStateCode);
            orderModel.HasIndex(o => o.ShippingCountryCode);
            orderModel.HasIndex(o => o.BillingCountryCode);
            orderModel.HasIndex(o => o.BillingStateCode);
            orderModel.HasIndex(o => o.RemoteHost);

            var pliModel = modelBuilder.Entity<ProductLineItem>();
            pliModel.HasKey(p => new {p.Index, p.OrderId});
            pliModel.HasIndex(p => p.OrderId);
            pliModel.HasIndex(p => p.ProductId);
            
            var pmModel = modelBuilder.Entity<PaymentMethod>();
            pmModel.HasKey(p => new {p.Index, p.OrderId});
            pmModel.HasIndex(p => p.Method);
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<RuntimeConfig> RuntimeConfigs { get; set; }
    }
}