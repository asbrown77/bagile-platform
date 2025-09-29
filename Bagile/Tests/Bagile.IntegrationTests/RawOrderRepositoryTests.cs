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
            var connStr = "Host=localhost;Port=5432;Database=bagile;Username=postgres;Password=postgres;SearchPath=bagile";
            _repo = new RawOrderRepository(connStr);
        }

        [Test, Category("Integration")]
        public async Task UpsertAsync_ShouldInsertAndUpdate()
        {
            var repo = new RawOrderRepository(DatabaseFixture.ConnectionString);

            var id1 = await repo.InsertAsync("woo", "123", "{ \"id\": 123 }");
            var id2 = await repo.InsertAsync("woo", "123", "{ \"id\": 123, \"changed\": true }");

            Assert.That(id2, Is.GreaterThan(id1));

            var all = await repo.GetAllAsync();
            Assert.That(all.Count(), Is.EqualTo(2));
            Assert.That(all, Has.Some.Matches<RawOrder>(r =>
                r.ExternalId == "123" && r.Payload.Contains("changed")));
        }
    }
}