using System.Threading.Tasks;
using Bagile.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Bagile.IntegrationTests
{
    public class RawOrderRepositoryTests
    {
        private RawOrderRepository _repo;

        [SetUp]
        public void Setup()
        {
            var connStr = "Host=localhost;Port=5432;Database=bagile;Username=postgres;Password=postgres";
            _repo = new RawOrderRepository(connStr);
        }

        [Test]
        public async Task Insert_And_GetAll_Should_Work()
        {
            var id = await _repo.InsertAsync("woo", "{ \"orderId\": 123 }");

            id.Should().BeGreaterThan(0);

            var orders = await _repo.GetAllAsync();
            orders.Should().NotBeEmpty();
        }
    }
}