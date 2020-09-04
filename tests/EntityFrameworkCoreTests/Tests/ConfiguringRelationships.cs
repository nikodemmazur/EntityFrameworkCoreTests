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
                                .HasForeignKey(u => u.HostId);
                }
            }
        }
    }
}
