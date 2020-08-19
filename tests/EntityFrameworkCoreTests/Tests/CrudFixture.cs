using EntityFrameworkCoreTests.Context;
using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using System;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Tests
{
    public class CrudFixture
    {
        private readonly ITestOutputHelper _testOutput;

        public CrudFixture(ITestOutputHelper testOutputHelper)
        {
            _testOutput = testOutputHelper;
        }

        [Fact]
        public void FirstTest()
        {
            using var ctx =
                SqlDb.CreateConnectionString()
                     .AsSqlConnectionString<BookStoreContext>()
                     .EnsureDb()
                     .SeedWith(@"TestData\RawTestData1.json")
                     .ToDbContext()
                     .StartLogging(_testOutput.AsLineWriter());

            foreach (var book in ctx.Books)
                _testOutput.WriteLine(new { book.Title, book.Price }.ToString());
        }
    }
}
