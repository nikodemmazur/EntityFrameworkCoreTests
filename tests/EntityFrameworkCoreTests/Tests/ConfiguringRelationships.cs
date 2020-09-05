using EntityFrameworkCoreTests.Context;
using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using EntityFrameworkCoreTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable disable

namespace EntityFrameworkCoreTests.Tests
{
    public class ConfiguringRelationships
    {
        /// <summary>
        /// One-to-one relationship in which <see cref="Attendee"/> is dependent on <see cref="Ticket"/>.
        /// Notice that "one-to-one" applies to <see cref="Attendee"/> as <see cref="Ticket"/> doesn't require <see cref="Attendee"/> to exist.
        /// The manner in how <see cref="Ticket"/> is related to <see cref="Attendee"/> should be rather defined as "one-to-zero-or-one".
        /// </summary>
        public class OneToOneOptionOne : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public OneToOneOptionOne(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class Ticket
            {
                public int TicketId { get; set; }
                public string Type { get; set; }
            }

            public class Attendee
            {
                public int AttendeeId { get; set; }
                public string Name { get; set; }
                public int ConventionTicketId { get; set; }
            }

            public class ConventionContext : DbContext
            {
                public DbSet<Attendee> Attendees { get; set; }

                public ConventionContext(DbContextOptions<ConventionContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<Attendee>()
                                .HasOne<Ticket>() // Required because Attendee doesn't have a navigation property to Ticket.
                                .WithOne() // Required by HasOne() as a subsequent call.
                                .HasForeignKey<Attendee>(a => a.ConventionTicketId); // Required because 'ConventionTicketId' doesn't meet 
                                                                                     // EF Core convention (it's expected to be as simple as 'TicketId').
                }
            }

            [Fact]
            public void CreatesOptionOneOneToOneRelationship()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<ConventionContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var newTicket = new Ticket { Type = "normal" };
                context.Add(newTicket);
                context.SaveChanges();
                newTicket.TicketId.Should().NotBe(0, "because new id has been assigned by the DB");

                var newAttendee = new Attendee { Name = "John Smith", ConventionTicketId = newTicket.TicketId };
                context.Add(newAttendee);
                context.SaveChanges();

                // Inner join as an equivalent of EF Core Include() for an entity with no navigation property.
                var nameTicketTypePair =
                    context
                        .Attendees
                        .Join(context.Set<Ticket>(), a => a.ConventionTicketId, t => t.TicketId, (a, t) => new { a.Name, t.Type })
                        .First();

                nameTicketTypePair.Should().BeEquivalentTo(new { Name = "John Smith", Type = "normal" }, "because that's the expected output" +
                    " of the above query");
            }
        }

        /// <summary>
        /// This class differs from <see cref="OneToOneOptionOne"/> in that it inverted the relationship and let entities have navigation properties.
        /// </summary>
        public class OneToOneOptionTwo : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public OneToOneOptionTwo(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class Ticket
            {
                public int TicketId { get; set; }
                public string Type { get; set; }
                public int AttendeeId { get; set; }
                public Attendee Attendee { get; set; }
            }

            public class Attendee
            {
                public int AttendeeId { get; set; }
                public string Name { get; set; }
                public Ticket Ticket { get; set; }
            }

            public class ConventionContext : DbContext
            {
                public DbSet<Attendee> Attendees { get; set; }

                public ConventionContext(DbContextOptions<ConventionContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    // Everything is handled by EF Core conventions.
                }
            }

            [Fact]
            public void CreatesOptionTwoOneToOneRelationship()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<ConventionContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var newAttendee = new Attendee { Name = "John Smith" }; // Attendee can exist without Ticket.
                context.Add(newAttendee); // var newAttendee is now being tracked.
                context.SaveChanges();
                newAttendee.Ticket.Should().BeNull($"because it has not been provided and {nameof(Attendee)} can exist without {nameof(Ticket)}");

                newAttendee.Ticket = new Ticket { Type = "normal" }; // Properties Attendee, AttendeeId and TicketId are handled by EF Core.
                context.SaveChanges();

                var nameTicketTypePair =
                    context
                        .Attendees
                        .Include(a => a.Ticket) // Now, Include() handles joining in the DB.
                        .Select(a => new { a.Name, a.Ticket.Type }) // Much simpler than the one in OptionOne.
                        .First();

                nameTicketTypePair.Should().BeEquivalentTo(new { Name = "John Smith", Type = "normal" }, "because that's the expected output" +
                    " of the above query");
            }
        }

        /// <summary>
        /// The recommended way to create dependent tables.
        /// </summary>
        public class OneToOneOptionThree : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public OneToOneOptionThree(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class Ticket
            {
                public int TicketId { get; set; }
                public string Type { get; set; }
                public int AttendeeId { get; set; }
                public Attendee Attendee { get; set; }
            }

            public class Attendee
            {
                public int AttendeeId { get; set; }
                public string Name { get; set; }
                public Ticket Ticket { get; set; }
            }

            public class ConventionContext : DbContext
            {
                public DbSet<Attendee> Attendees { get; set; }

                public ConventionContext(DbContextOptions<ConventionContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<Ticket>()
                                .HasKey(t => new { t.TicketId, t.AttendeeId }); // Required because not supported by conventions.
                }
            }

            [Fact]
            public void CreatesOptionThreeOneToOneRelationship()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<ConventionContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var newAttendee = new Attendee { Name = "John Smith" }; // Attendee can exist without Ticket.
                context.Add(newAttendee); // var newAttendee is now being tracked.
                context.SaveChanges();
                newAttendee.Ticket.Should().BeNull($"because it has not been provided and {nameof(Attendee)} can exist without {nameof(Ticket)}");

                var newAttendeeEntry = context.Entry(newAttendee);

                newAttendeeEntry
                    .Invoking(ae => ae.Property($"{nameof(Attendee.Ticket)}{nameof(Ticket.TicketId)}").CurrentValue)
                    .Should()
                    .Throw<Exception>("because there's no need for a shadow property to determine the relationship by EF Core");

                newAttendee.Ticket = new Ticket { Type = "normal" }; // Properties Attendee, AttendeeId and TicketId are handled by EF Core.
                context.SaveChanges();

                var nameTicketTypePair =
                    context
                        .Attendees
                        .Include(a => a.Ticket)
                        .Select(a => new { a.Name, a.Ticket.Type })
                        .First();

                nameTicketTypePair.Should().BeEquivalentTo(new { Name = "John Smith", Type = "normal" }, "because that's the expected output" +
                    " of the above query");
            }
        }

        public class OneToMany : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public OneToMany(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class Notebook
            {
                public int NotebookId { get; set; }
                public string Name { get; set; }
                public ICollection<UsbSlot> Usbs { get; set; }
            }

            public class UsbSlot
            {
                public int UsbSlotId { get; set; }
                public string UsbVersion { get; set; }
                public int HostId { get; set; }
            }

            public class ManufacturerContext : DbContext
            {
                public DbSet<Notebook> Notebooks { get; set; }

                public ManufacturerContext(DbContextOptions<ManufacturerContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<Notebook>()
                                .HasMany(n => n.Usbs)
                                .WithOne()
                                .HasForeignKey(u => u.HostId); // Required because 'HostId' doesn't meet the naming convention.
                }
            }

            [Fact]
            public void CreatesOneToManyRelationship()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<ManufacturerContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var newUsbs = new[] { new UsbSlot { UsbVersion = "3.0" }, new UsbSlot { UsbVersion = "2.0" } };
                var newNotebook = new Notebook { Name = "Pear", Usbs = newUsbs };
                context.Add(newNotebook);
                context.SaveChanges();

                context
                    .Notebooks
                    .Include(n => n.Usbs)
                    .SingleOrDefault()
                    .Should().NotBeNull("because has been successfully added");
            }
        }

        public class ManyToManyByConvention : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public ManyToManyByConvention(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class Book
            {
                public int BookId { get; set; }
                public string Title { get; set; }
                public ICollection<BookAuthor> AuthorLinks { get; set; }
            }

            public class Author
            {
                public int AuthorId { get; set; }
                public string Name { get; set; }
                public ICollection<BookAuthor> BookLinks { get; set; }
            }

            public class BookAuthor
            {
                public int BookId { get; set; }
                public int AuthorId { get; set; }
                public Book Book { get; set; }
                public Author Author { get; set; }
            }

            public class BookStoreContext : DbContext
            {
                public DbSet<Book> Books { get; set; }

                public BookStoreContext(DbContextOptions<BookStoreContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder
                        .Entity<BookAuthor>()
                        .HasKey(ba => new { ba.BookId, ba.AuthorId });
                }
            }

            [Fact]
            public void CreatesManyToManyRelationshipByConvention()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<BookStoreContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var aFreemanAuthor = new Author { Name = "Adam Freeman" };
                var eEvansAuthor = new Author { Name = "Eric Evans" };
                var aspBook = new Book { Title = "...ASP.NET CORE..." };
                var dddBook = new Book { Title = "...DDD..." };
                context
                    .Invoking(ctx =>
                        {
                            ctx.AddRange(new[] { aFreemanAuthor, eEvansAuthor });
                            ctx.AddRange(new[] { aspBook, dddBook });
                            ctx.SaveChanges();
                        })
                    .Should().NotThrow("because it's actually the 'zero-or-one-or-many' relationship.");
                context.Entry(aspBook).State.Should().Be(EntityState.Unchanged, "because it has already been saved and" +
                    " has not been modified afterwards");
                aspBook.AuthorLinks.Should().BeNull("because tracking doesn't automatically loads navigation properties");
                context.Entry(aspBook).Collection(a => a.AuthorLinks).Load(); // Load navigation property explicitly.
                context.Entry(aspBook).State.Should().Be(EntityState.Unchanged, "because it's still unchanged - just its property has been loaded");
                aspBook.AuthorLinks.Should().NotBeNull("because already loaded");
                aspBook.AuthorLinks.Count().Should().Be(0, "because no links have been specified");
                aspBook.AuthorLinks = aspBook.AuthorLinks.Append(new BookAuthor { Author = aFreemanAuthor }).ToList();
                context.Invoking(ctx => ctx.SaveChanges()).Should().NotThrow("because just Author must be specified in BookAuthor - the rest is" +
                    " handled by EF Core");
                aspBook.AuthorLinks = aspBook.AuthorLinks.Append(new BookAuthor { Author = eEvansAuthor }).ToList();
                dddBook.AuthorLinks = new List<BookAuthor> { new BookAuthor { Author = eEvansAuthor }, new BookAuthor { Author = aFreemanAuthor } };
                context.SaveChanges();
                context.Set<BookAuthor>().Count().Should().Be(4, "because 2 authors * 2 books");
            }
        }

        public class AlternateUniqueKey : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public AlternateUniqueKey(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class Person
            {
                public int PersonId { get; set; }
                public string Name { get; set; }
                public string UserId { get; set; } // which is the person's email address and therefore unique
                public ContactInfo Contact { get; set; }
            }

            public class ContactInfo
            {
                public int ContactInfoId { get; set; }
                public string MobileNumber { get; set; }
                public string EmailAddress { get; set; }
            }

            public class ContactBookContext : DbContext
            {
                public DbSet<Person> People { get; set; }

                public ContactBookContext(DbContextOptions<ContactBookContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    //modelBuilder.Entity<Person>()
                                //.HasAlternateKey(p => p.UserId); // no need to do that because of the below HasPrincipalKey().

                    modelBuilder.Entity<Person>()
                                .Property(p => p.UserId)
                                .IsRequired();

                    modelBuilder.Entity<ContactInfo>()
                                .Property(ci => ci.EmailAddress)
                                .IsRequired();

                    modelBuilder.Entity<Person>()
                                .HasOne(p => p.Contact)
                                .WithOne()
                                .HasForeignKey<ContactInfo>(ci => ci.EmailAddress)
                                .HasPrincipalKey<Person>(p => p.UserId);
                }
            }

            [Fact]
            public void CreatesRelationshipUsingAlternateUniqueKey()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<ContactBookContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var newContact = new ContactInfo { MobileNumber = "1234567890" };
                var newPerson = new Person { Name = "John Smith", UserId = "jsmith@gmail.com", Contact = newContact };
                context.Add(newPerson);
                context.SaveChanges();
                newContact.EmailAddress.Should().Be(newPerson.UserId, "because it's the foreign key linking to Person by its UserId" +
                    " which has already been set by EF Core on SaveChanges()");
            }
        }

        public class OwnedTypes : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public OwnedTypes(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class OrderInfo
            {
                public int OrderInfoId { get; set; }
                public string OrderNumber { get; set; }
                public Address DeliveryAddress { get; set; }
                public Address BillingAddress { get; set; }
            }

            public class Address
            {
                public string ZipPostCode { get; set; }
                public string City { get; set; }
            }

            public class ShopContext : DbContext
            {
                public DbSet<OrderInfo> OrderInfos { get; set; }

                public ShopContext(DbContextOptions<ShopContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<OrderInfo>()
                                .OwnsOne(oi => oi.DeliveryAddress);

                    modelBuilder.Entity<OrderInfo>()
                                .OwnsOne(oi => oi.BillingAddress);
                }
            }

            [Fact]
            public void OwnedTypesDoNotNeedToBeIncluded()
            {
                var newBillingAddress = new Address { City = "Krakow", ZipPostCode = "04-218" };
                var newDeliveryAddress = new Address { City = "Warsaw", ZipPostCode = "00-001" };
                var newOrderInfo = new OrderInfo { BillingAddress = newBillingAddress, DeliveryAddress = newDeliveryAddress, OrderNumber = "#1" };

                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                var dbCtxOptions = dbConnectionString
                    .AsSqlConnectionString<ShopContext>()
                    .EnsureDb();
                using (var context = dbCtxOptions.BuildDbContext().StartLogging(_testOutput.AsLineWriter()))
                {
                    context.Add(newOrderInfo);
                    context.SaveChanges();
                }

                using (var context = dbCtxOptions.BuildDbContext().StartLogging(_testOutput.AsLineWriter()))
                {
                    var oi =
                        context
                            .OrderInfos
                            .Single();

                    oi.BillingAddress.Should().NotBeNull("because it's an owned type");
                    oi.DeliveryAddress.Should().NotBeNull("because it's also an owned type");
                }
            }
        }

        public class TablePerHierarchy : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public TablePerHierarchy(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public abstract class Payment
            {
                public int PaymentId { get; set; }
                public decimal Amount { get; set; }
                public string Type { get; set; }
            }

            public class PaymentCash : Payment
            {
                // Nothing here
            }

            public class PaymentCard : Payment
            {
                public string Receipt { get; set; }
            }

            public class SoldIt
            {
                public int SoldItId { get; set; }
                public string WhatSold { get; set; }
                public int PaymentId { get; set; }
                public Payment Payment { get; set; }
            }

            public class ShippingContext : DbContext
            {
                public DbSet<SoldIt> SoldIts { get; set; }

                public ShippingContext(DbContextOptions<ShippingContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<Payment>()
                                .HasDiscriminator(p => p.Type)
                                .HasValue<PaymentCash>("cash")
                                .HasValue<PaymentCard>("card");
                }
            }

            [Fact]
            public void TablePerHierarchySupportsInheritance()
            {
                var cardPayment = new PaymentCard { Amount = 2000M, Receipt = "ęśąćż" };
                var cashPayment = new PaymentCash { Amount = 100M };
                var notebookSoldIt = new SoldIt { WhatSold = "Notebook", Payment = cardPayment };
                var keyboardSoldIt = new SoldIt { WhatSold = "Keyboard", Payment = cashPayment };

                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                var dbCtxOptions = dbConnectionString
                    .AsSqlConnectionString<ShippingContext>()
                    .EnsureDb();
                using (var context = dbCtxOptions.BuildDbContext().StartLogging(_testOutput.AsLineWriter()))
                {
                    context.AddRange(new[] { notebookSoldIt, keyboardSoldIt });
                    context.SaveChanges();
                }

                using (var context = dbCtxOptions.BuildDbContext().StartLogging(_testOutput.AsLineWriter()))
                {
                    var notebookPayment =
                        context
                            .SoldIts
                            .Where(si => si.WhatSold == "Notebook")
                            .Select(si => si.Payment)
                            .Single();

                    var keyboardPayment =
                        context
                            .SoldIts
                            .Where(si => si.WhatSold == "Keyboard")
                            .Select(si => si.Payment)
                            .Single();

                    notebookPayment.Should().BeOfType<PaymentCard>("because the original type has been preserved");
                    notebookPayment.Type.Should().Be("card", $"because that's a discriminator value indicating {nameof(PaymentCard)} type");

                    keyboardPayment.Should().BeOfType<PaymentCash>("because the original type has been preserved");
                    keyboardPayment.Type.Should().Be("cash", $"because that's a discriminator value indicating {nameof(PaymentCash)} type");

                    context
                        .Set<Payment>()
                        .OfType<PaymentCard>()
                        .First().Receipt
                        .Should()
                        .Be("ęśąćż", "because that's another way to handle a hierarchical table");
                }
            }
        }

        public class TableSplitting : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public TableSplitting(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            public class BookSummary
            {
                public int BookSummaryId { get; set; }
                public string Title { get; set; }
                public BookDetail Details { get; set; }
            }

            public class BookDetail
            {
                public int BookDetailId { get; set; }
                public decimal Price { get; set; }
            }

            public class BookStoreContext : DbContext
            {
                public DbSet<BookSummary> BookSummaries { get; set; }
                public DbSet<BookDetail> BookDetails { get; set; }

                public BookStoreContext(DbContextOptions<BookStoreContext> options) : base(options) { }

                protected override void OnModelCreating(ModelBuilder modelBuilder)
                {
                    modelBuilder.Entity<BookSummary>()
                                .HasOne(bs => bs.Details)
                                .WithOne()
                                .HasForeignKey<BookDetail>(bd => bd.BookDetailId); // Tell EF Core that this entity is the dependent one

                    modelBuilder.Entity<BookSummary>()
                                .ToTable("Books");

                    modelBuilder.Entity<BookDetail>()
                                .ToTable("Books");
                }
            }

            [Fact]
            public void TableIsSplitIntoTwoEntities()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringRelationships), GetCallerName());
                using var context =
                    dbConnectionString
                        .AsSqlConnectionString<BookStoreContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                var newDetail = new BookDetail { Price = 49M };
                var newSummary = new BookSummary { Title = "Entity Framework Core IN ACTION", Details = newDetail };

                context.Add(newSummary);
                context.SaveChanges();

                context
                    .BookSummaries
                    .Single()
                    .Title
                    .Should().Contain("Entity Framework");

                var aloneSummary = new BookSummary { Title = "C# 8.0 in a Nutshell" };
                context.Add(aloneSummary);
                context.Invoking(ctx => ctx.SaveChanges()).Should().NotThrow<Exception>("because not all parts of a split table must be saved at once");

                context.Invoking(ctx =>
                {
                    ctx
                    .BookSummaries
                    .Include(bs => bs.Details)
                    .Where(bs => bs.Title.ToLower().Contains("c#"))
                    .Select(bs => bs.Details.Price)
                    .Single();
                }).Should().NotThrow("because EF Core returns the default values in this case");
            }
        }
    }
}
