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
    }
}
