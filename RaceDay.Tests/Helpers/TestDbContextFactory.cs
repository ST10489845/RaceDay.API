using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;

namespace RaceDay.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static RaceDayDbContext Create()
        {
            var options = new DbContextOptionsBuilder<RaceDayDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new RaceDayDbContext(options);
        }
    }
}