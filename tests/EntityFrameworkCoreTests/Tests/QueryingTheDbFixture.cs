using EntityFrameworkCoreTests.Context;
using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Tests
{
    public class QueryingTheDbFixture
    {
        private readonly ITestOutputHelper _testOutput;

        public QueryingTheDbFixture(ITestOutputHelper testOutputHelper)
        {
            _testOutput = testOutputHelper;
        }

        [Fact]
        public void ReferencedCollectionWhenNotIncludedIsNull()
        {
            using var ctx =
                SqlDb.CreateConnectionString()
                     .AsSqlConnectionString<BookStoreContext>()
                     .EnsureDb()
                     .SeedWith(@"TestData\RawTestData1.json")
                     .BuildDbContext()
                     .StartLogging(_testOutput.AsLineWriter());

            ctx
                .Books
                .First()
                .Reviews
                .Should().BeNull("because the Reviews table to which the Books is linked has not been included by Include()");
        }
    }
}
