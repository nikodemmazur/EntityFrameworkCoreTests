using EntityFrameworkCoreTests.Context;
using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Tests
{
    public class QueryingTheDbFixture
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly DbConnectionString _dbConnectionString;

        private BookStoreContext CreateBookStoreContext() =>
            BookStoreContextFactory.Instance.Create(_dbConnectionString, _testOutput.AsLineWriter());

        public QueryingTheDbFixture(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
            _dbConnectionString = DbConnectionString.Create(GetType().Name);

            BookStoreContextFactory.Instance.InitDb(_dbConnectionString, @"TestData\RawTestData1.json");
        }

        [Fact]
        public void BookCountShouldBeFour()
        {
            using var context = CreateBookStoreContext();

            context
                .Books
                .AsNoTracking()
                .Count()
                .Should().Be(4, "because the JSON test data has 4 items");
        }

        [Fact]
        public void ReviewsShouldBeNull()
        {
            using var context = CreateBookStoreContext();

            context
                .Books
                .AsNoTracking()
                .First()
                .Reviews
                .Should().BeNull("because the Reviews table to which the Books is linked has not been included by Include()");
        }

        [Fact]
        public void ReviewsShouldNotBeNull()
        {
            using var context = CreateBookStoreContext();

            context
                .Books
                .AsNoTracking()
                .Include(b => b.Reviews)
                .First()
                .Reviews
                .Should().NotBeNull("because Reviews have been included with Include()");
        }

        [Fact]
        public void AuthorShouldNotBeNull()
        {
            using var context = CreateBookStoreContext();

            context
                .Books
                .AsNoTracking()
                .Include(b => b.AuthorsLink)
                .ThenInclude(al => al.Author)
                .First()
                .AuthorsLink
                .First()
                .Author
                .Should().NotBeNull("because Author has been included as a second-level relationship with ThenInclude()");
        }

        [Fact]
        public void CountShouldBeExecutedOnTheClientSide()
        {
            using var context = CreateBookStoreContext();

            var logReader = new SimpleLineWriter();
            context.StartLogging(logReader);

            context
                .Books
                .AsNoTracking()
                .AsEnumerable()
                .Count();

            logReader.GetString().ToLower().Should().NotContain("count", "because AsEnumerable() makes the query no longer queryable");
        }

        [Fact]
        public void CountShouldBeExecutedOnTheServerSide()
        {
            using var context = CreateBookStoreContext();

            var logReader = new SimpleLineWriter();
            context.StartLogging(logReader);

            context
                .Books
                .AsNoTracking()
                .Count();

            logReader.GetString().ToLower().Should().Contain("count", "because the whole query is queryable and SQL server has COUNT function");
        }

        [Fact]
        public void BookAndItsRelatedDataAreLoadedEagerly()
        {
            using var context = CreateBookStoreContext();

            var firstBook =
                context
                    .Books
                    .First();

            firstBook.AuthorsLink.Should().BeNull("because has not been included");
            firstBook.Reviews.Should().BeNull("because have not been included.");

            firstBook =
                context
                    .Books
                    .AsNoTracking()
                    .Include(b => b.AuthorsLink)
                    .ThenInclude(al => al.Author)
                    .Include(b => b.Reviews)
                    .Include(b => b.Promotion)
                    .First();

            firstBook.AuthorsLink.Should().NotBeNull("because has been included this time");
            firstBook.AuthorsLink.First().Author.Should().NotBeNull("because has been included as well");
            firstBook.Reviews.Should().NotBeNull("because have been included this time");
        }

        [Fact]
        public void BookAndItsRelatedDataAreLoadedExplicitly()
        {
            using var context = CreateBookStoreContext();

            var firstBook =
                context
                    .Books
                    // .AsNoTracking() - Navigation properties can only be loaded for tracked entities.
                    .First();

            firstBook.AuthorsLink.Should().BeNull("because has not been included");
            firstBook.Reviews.Should().BeNull("because have not been included.");

            context.Entry(firstBook).Collection(b => b.AuthorsLink).Load();
            firstBook.AuthorsLink.ForEach(al => context.Entry(al).Reference(r => r.Author).Load());
            context.Entry(firstBook).Collection(b => b.Reviews).Load();

            firstBook.AuthorsLink.Should().NotBeNull("because has been included this time");
            firstBook.AuthorsLink.First().Author.Should().NotBeNull("because has been included as well");
            firstBook.Reviews.Should().NotBeNull("because have been included this time");
        }
        
        [Fact]
        public void BookAndItsRelatedReviewAreLoadedViaSelect()
        {
            using var context = CreateBookStoreContext();

            context
                .Invoking(ctx =>
                {
                    ctx
                        .Books
                        .Select(b => new
                        {
                            b.Title,
                            b.Price,
                            NumReviews = b.Reviews.Count()
                        })
                        .First();
                }).Should().NotThrow<NullReferenceException>("because Select() loads the needed references");
        }
    }
}
