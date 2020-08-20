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
    /// <summary>
    /// Every test method is placed in a separate class to let Xunit run them in parallel.
    /// </summary>
    public class QueryingTheDbFixture
    {
        private readonly ITestOutputHelper _testOutput;

        private BookStoreContext CreateBookStoreContext(string filePathToSeedDbWith = @"TestData\RawTestData1.json")
        {
            return SqlDb.CreateConnectionString(1)
                        .AsSqlConnectionString<BookStoreContext>()
                        .EnsureDb()
                        .SeedWith(filePathToSeedDbWith)
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());
        }

        public QueryingTheDbFixture(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        public class BookCountShouldBeFourTest : QueryingTheDbFixture
        {

            public BookCountShouldBeFourTest(ITestOutputHelper testOutput) : base(testOutput) { }

            [Fact]
            public void BookCountShouldBeFour()
            {
                using var context = CreateBookStoreContext();

                context
                    .Books
                    .Count()
                    .Should().Be(4, "because the json test data has 4 items");
            }
        }

        public class ReviewsShouldBeNullTest : QueryingTheDbFixture
        {
            public ReviewsShouldBeNullTest(ITestOutputHelper testOutput) : base(testOutput) { }

            [Fact]
            public void ReviewsShouldBeNull()
            {
                using var context = CreateBookStoreContext();

                context
                    .Books
                    .First()
                    .Reviews
                    .Should().BeNull("because the Reviews table to which the Books is linked has not been included by Include()");
            }
        }

        public class ReviewsShouldNotBeNullTest : QueryingTheDbFixture
        {
            public ReviewsShouldNotBeNullTest(ITestOutputHelper testOutput) : base(testOutput) { }

            [Fact]
            public void ReviewsShouldNotBeNull()
            {
                using var context = CreateBookStoreContext();

                context
                    .Books
                    .Include(b => b.Reviews)
                    .First()
                    .Reviews
                    .Should().NotBeNull("because Reviews have been included with Include()");
            }
        }

        public class AuthorShouldNotBeNullTest : QueryingTheDbFixture
        {
            public AuthorShouldNotBeNullTest(ITestOutputHelper testOutput) : base(testOutput) { }

            [Fact]
            public void AuthorShouldNotBeNull()
            {
                using var context = CreateBookStoreContext();

                context
                    .Books
                    .Include(b => b.AuthorsLink)
                    .ThenInclude(al => al.Author)
                    .First()
                    .AuthorsLink
                    .First()
                    .Author
                    .Should().NotBeNull("because Author has been included as a second-level relationship with ThenInclude()");
            }
        }
    }
}
