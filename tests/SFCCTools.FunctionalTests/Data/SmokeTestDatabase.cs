using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using SFCCTools.Jobs;
using Xunit;

namespace SFCCTools.FunctionalTests.Data
{
    public class SmokeTestDatabase : IClassFixture<DatabaseFixture>
    {
        private BIDatabaseContext DbContext { get; set; }

        public SmokeTestDatabase(DatabaseFixture fixture)
        {
            DbContext = fixture.ServiceProvider.GetService<BIDatabaseContext>();
        }

        [Fact]
        [Trait("Category", "RequiresDatabase")]
        public void TestCreatingOrder()
        {
            var order = new Order()
            {
                OrderId = "test123"
            };
            DbContext.Add<Order>(order);
            DbContext.SaveChanges();

            var order2 = DbContext.Orders.SingleOrDefault(o => o.OrderId == "test123");
            Assert.NotNull(order2);

            DbContext.Orders.Remove(order2);
            DbContext.SaveChanges();
        }
    }
}