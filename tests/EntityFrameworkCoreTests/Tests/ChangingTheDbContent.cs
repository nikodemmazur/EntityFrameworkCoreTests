using EntityFrameworkCoreTests.Context;
using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Entities;
using EntityFrameworkCoreTests.Logging;
using EntityFrameworkCoreTests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Tests
{
    public class ChangingTheDbContent : TestClassBase
    {
        private readonly ITestOutputHelper _testOutput;
        private readonly DbContextFactory<BookStoreContext> _fact;

        private BookStoreContext CreateBookStoreContext()
        {
            var dbConnectionString = DbConnectionString.Create(GetType().Name, GetCallerName(1));
            return _fact.Create(dbConnectionString, _testOutput.AsLineWriter());
        }

        public ChangingTheDbContent(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
            _fact = DbContextFactoryManager<BookStoreContext>.Instance.GetDbContextFactory(nameof(ChangingTheDbContent));

            _fact.RegisterOnInit(dbCtxOpts =>
            {
                dbCtxOpts.SeedWith(@"TestData\RawTestData1.json");
            });
            var dbConnectionStrings = ListFactMethodNames().Select(str => DbConnectionString.Create(GetType().Name, str));
            _fact.InitDbAsync(dbConnectionStrings).Wait();
        }

        [Fact]
        public void CreatesNewBookWithTheContextAddMethod()
        {
            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Where(b => b.Title == "New Book")
                    .Count()
                    .Should().Be(0, $"because there's no such {nameof(Book)} in the seed data");

                var newBook = new Book
                {
                    Title = "New Book",
                    PublishedOn = DateTime.Today,
                    Reviews = new List<Review>
                    {
                        new Review
                        {
                            NumStars = 5,
                            Comment = "Great new book!",
                            VoterName = "Mr Tester"
                        }
                    }
                };

                context.Add(newBook).Entity.BookId.Should().Be(0, "because the Db has not yet created the primary key");
                context.SaveChanges().Should().BeGreaterThan(0, "because at least 1 state entry should be written");
                context.Entry(newBook).Entity.BookId.Should().Be(5, "because the Db should already have created the primary key");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .SingleOrDefault(b => b.Title == "New Book")
                    .Should().NotBeNull($"because the {nameof(Book)} has already been added");
            }
        }

        [Fact]
        public void PerformsTheConntectedUpdateOfTheBookPublishDate()
        {
            using (var context = CreateBookStoreContext())
            {
                var book =
                    context
                        .Books
                        .Single(b => b.Title.ToLower().Contains("pro asp.net"));

                book.PublishedOn.Should().NotBeAfter(DateTime.Today, $"because the {nameof(Book)} was published in 2010");
                book.PublishedOn = 18.April(2038);
                context.SaveChanges().Should().Be(1, "because only one entry has been modified");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Single(b => b.Title.ToLower().Contains("pro asp.net"))
                    .PublishedOn
                    .Should().BeAfter(DateTime.Today, "because I shouldn't be running these tests after 18 years");
            }
        }

        /// <summary>
        /// Helper class for <see cref="PerformsTheDisconnectedUpdateOfTheBookPublishDate"/>.
        /// </summary>
        private class ChangePubDateDto
        {
            public ChangePubDateDto(int bookId, DateTime newDateTime)
            {
                BookId = bookId;
                NewDateTime = newDateTime;
            }

            public int BookId { get; }
            public DateTime NewDateTime { get; }
        }

        [Fact]
        public void PerformsTheDisconnectedUpdateOfTheBookPublishDate()
        {
            int bookIdToChange;

            // The first connection - pick the book id based on the title.
            using (var context = CreateBookStoreContext())
            {
                var book =
                    context
                        .Books
                        .Single(b => b.Title == "Pro PayPal E-Commerce");

                bookIdToChange = book.BookId;

                book.PublishedOn.Should().NotBeAfter(DateTime.Today, $"because the {nameof(Book)} was published in 2010");
            }

            // Prepare the DTO that the entry will be updated with.
            var dto = new ChangePubDateDto(bookIdToChange, 18.April(2038));

            // The second connection - update the publish date.
            using (var context = CreateBookStoreContext())
            {
                var book = context.Find<Book>(dto.BookId);
                book.PublishedOn = dto.NewDateTime;
                context.SaveChanges().Should().Be(1, "because only one entry has been modified");
            }

            // The third connection - verification.
            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Where(b => b.Title == "Pro PayPal E-Commerce")
                    .Select(b => b.PublishedOn)
                    .Single()
                    .Should().Be(dto.NewDateTime, "because has been updated with the DTO");
            }
        }

        [Fact]
        public void PerformsTheDisconnectedUpdateOfTheEntireAuthor()
        {
            string json;
            using (var context = CreateBookStoreContext())
            {
                var authorToUpdate =
                    context
                        .Books
                        .Where(b => b.Title == "Advanced Android 4 Games")
                        .Select(b => b.AuthorsLink.First().Author)
                        .Single();

                authorToUpdate.Name = "Katy Perry";
                json = JsonConvert.SerializeObject(authorToUpdate);
            }

            using (var context = CreateBookStoreContext())
            {
                var authorToUpdate = JsonConvert.DeserializeObject<Author>(json);

                _ = context.Authors.Update(authorToUpdate);
                context.SaveChanges().Should().Be(1, $"because only one {nameof(Author)} has been updated");
            }
        }

        [Fact]
        public void UpdatingABookPromotionWithoutLoadingThePromotionThrowsAnEx()
        {
            using (var context = CreateBookStoreContext())
            {
                var book =
                    context
                        .Books
                        .Include(b => b.Promotion)
                        .Where(b => b.Title.Contains("pro asp.net"))
                        .Single();

                book.Promotion = new PriceOffer
                {
                    NewPrice = 100M,
                    PromotionalText = "The everlasting promotion!"
                };

                context.SaveChanges();
            }

            using (var context = CreateBookStoreContext())
            {
                var book =
                    context
                        .Books
                        //.Include(b => b.Promotion)
                        .Where(b => b.Title.ToLower().Contains("pro asp.net"))
                        .Single();

                book.Promotion = new PriceOffer
                {
                    NewPrice = 50M,
                    PromotionalText = "Half price!"
                };

                context
                    .Invoking(ctx => ctx.SaveChanges())
                    .Should()
                    .Throw<DbUpdateException>()
                    .WithInnerException<Microsoft.Data.SqlClient.SqlException>($"because of the {nameof(PriceOffer.BookId)} foreign key duplication");
            }
        }

        [Fact]
        public void PerformsTheDisconnectedUpdateOfThePriceOfferViaTheBookProperty()
        {
            int bookId;
            using (var context = CreateBookStoreContext())
            {
                bookId =
                    context
                        .Books
                        .Where(b => b.Title.Contains("Google Maps"))
                        .Select(b => b.BookId)
                        .First();
            }

            var newPrice = 1M;
            var text = "1 Dollar sale!";

            using (var context = CreateBookStoreContext())
            {
                var book =
                    context
                        .Books
                        .Include(b => b.Promotion)
                        .Single(b => b.BookId == bookId);

                if (book.Promotion is null)
                {
                    book.Promotion = new PriceOffer
                    {
                        BookId = bookId,
                        NewPrice = newPrice,
                        PromotionalText = text
                    };
                }
                else
                {
                    book.Promotion.NewPrice = newPrice;
                    book.Promotion.PromotionalText = text;
                }

                context.SaveChanges().Should().Be(1, $"because the {nameof(PriceOffer)}s table changed only - " +
                    $"the {nameof(Book)} table has no relational link (property) to track {nameof(PriceOffer)}s");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Include(b => b.Promotion)
                    .Single(b => b.BookId == bookId)
                    .Promotion
                    .Should()
                    .BeEquivalentTo(new PriceOffer
                    { 
                        NewPrice = newPrice, 
                        PromotionalText = text,
                        BookId = bookId
                    }, options =>
                        options.Excluding(o => o.PriceOfferId), "because the changes should already be applied");
            }
        }

        [Fact]
        public void RetrievesNonModeledEntities()
        {
            Review review;
            using (var context = CreateBookStoreContext())
            {
                review =
                    context
                        .Books
                        .Include(b => b.Reviews)
                        .Where(b => b.BookId == 1)
                        .Select(b => b.Reviews.First())
                        .Single();
            }

            Review anemicReview = new Review
            {
                NumStars = review.NumStars,
                BookId = review.BookId,
                VoterName = review.VoterName
            };

            using (var context = CreateBookStoreContext())
            {
                context
                    .Set<Review>()
                    .Should()
                    .Contain(r => r.ReviewId == review.ReviewId, "because the retrieved set represents the whole table in the db");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Find<Review>(review.ReviewId)
                    .Should()
                    .NotBeNull()
                    .And
                    .BeEquivalentTo(review, "because those two instances have the same primary key");
            }
        }

        [Fact]
        public void AddsNewPriceOfferDirectlyByCreatingANewRow()
        {
            using (var context = CreateBookStoreContext())
            {
                context
                    .Add(new PriceOffer
                    {
                        BookId = 1,
                        NewPrice = 2.99M,
                        PromotionalText = "Black Friday madness"
                    });

                _ = context.SaveChanges();
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .PriceOffers
                    .SingleOrDefault(po => po.NewPrice == 2.99M && po.PromotionalText == "Black Friday madness")
                    .Should().NotBeNull("because has just been added");
            }
        }

        [Fact]
        public void TheFindMethodDoesNotIncludeRelatedObjects()
        {
            using (var context = CreateBookStoreContext())
            {
                context
                    .Find<Book>(1)
                    .Reviews
                    .Should().BeNull("because Find() does not include related objects");

                context
                    .Books
                    .Include(b => b.Reviews)
                    .Single(b => b.BookId == 1)
                    .Reviews
                    .Should().NotBeNull("because that's the proper way to include related objects");
            }
        }

        [Fact]
        public void AddsAnExistingAuthorToTheBook()
        {
            Author authorToAdd;

            using (var context = CreateBookStoreContext())
            {
                authorToAdd =
                    context
                        .Authors
                        .Include(a => a.BooksLink)
                        .ThenInclude(bl => bl.Book)
                        .Where(a => a.BooksLink
                                     .Select(bl => bl.Book)
                                     .Where(b => b.Title.ToLower().Contains("pro asp.net"))
                                     .Count() == 0)
                        .First();

                var bookToUpdate =
                    context
                        .Books
                        .Include(b => b.AuthorsLink)
                        .Single(b => b.Title.ToLower().StartsWith("pro asp.net"));

                bookToUpdate.AuthorsLink =
                    bookToUpdate
                        .AuthorsLink
                        .Append(new BookAuthor
                            {
                                Book = bookToUpdate,
                                Author = authorToAdd,
                                Order = 2
                            })
                        .ToList();

                context.SaveChanges().Should().Be(1, $"because the {nameof(BookAuthor)} table has been updated");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Include(b => b.AuthorsLink)
                    .ThenInclude(al => al.Author)
                    .Single(b => b.Title.ToLower().StartsWith("pro asp.net"))
                    .AuthorsLink.Select(al => al.Author)
                    .Should().HaveCount(3)
                    .And
                    .Contain(a => a.Name == authorToAdd.Name, $"because the {nameof(Author)} has been assigned to the book");
            }
        }

        [Fact]
        public void ChangesReviewAndThenAssignsItToADifferentBook()
        {
            int bookId;
            int revCountBefore;

            using (var context = CreateBookStoreContext())
            {
                var book =
                    context
                        .Books
                        .Include(b => b.Reviews)
                        .Single(b => b.Title.ToLower().StartsWith("pro asp.net"));

                bookId = book.BookId;
                revCountBefore = book.Reviews.Count();

                var reviewToChange =
                    context
                        .Set<Review>()
                        .Where(r => r.BookId != bookId)
                        .First();

                reviewToChange.Comment = "Great book!";
                reviewToChange.BookId = bookId;

                context.SaveChanges().Should().Be(1, $"because only one entry of the {nameof(Review)}s table has changed");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Include(b => b.Reviews)
                    .Single(b => b.BookId == bookId)
                    .Reviews
                    .Should()
                    .Contain(r => r.Comment == "Great book!", $"because the {nameof(Review)} has been transfered from a different Book to this one " +
                        "and its comment has been updated");
            }
        }

        [Fact]
        public void RemovesBook()
        {
            int bookCountBefore;

            using (var context = CreateBookStoreContext())
            {
                bookCountBefore =
                    context
                        .Books
                        .Count();

                var bookToRemove =
                    context
                        .Books
                        .First();

                context.Remove(bookToRemove); // Starts tracking with all related objects so that the cascade delete is possible. 
                context.SaveChanges().Should().Be(1, "because one change has been applied - notice that the cascade deletes don't count here");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Count()
                    .Should().Be(bookCountBefore - 1, $"because one {nameof(Book)} has been removed");
            }
        }

        [Fact]
        public void HidesBook()
        {
            int bookCountBefore;

            using (var context = CreateBookStoreContext())
            {
                bookCountBefore =
                    context
                        .Books
                        .Count();

                context
                    .Books
                    .First()
                    .SoftDeleted = true; // "... data never stops being data: it transforms into another state."
                                         // That's the motivation for introducing the 'SoftDeleted' status.

                context.SaveChanges().Should().Be(1, $"because one {nameof(Book)} entry has been modified");
            }

            using (var context = CreateBookStoreContext())
            {
                context
                    .Books
                    .Count()
                    .Should().Be(bookCountBefore - 1, $"because {nameof(BookStoreContext)} has {nameof(Book.SoftDeleted)} query filter");
            }
        }
    }
}
