using EntityFrameworkCoreTests.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable disable

namespace EntityFrameworkCoreTests.Context
{
    public class BookStoreContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<Author> Authors { get; set; }
        public DbSet<PriceOffer> PriceOffers { get; set; }

        public BookStoreContext(DbContextOptions<BookStoreContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Since EfCore uses conventions when picking the right property as PK, I need to explicitly define them
            // in the linking table (a linking table is required to make the Many-to-Many relationship).
            modelBuilder
                .Entity<BookAuthor>()
                .HasKey(x => new { x.BookId, x.AuthorId });
        }
    }
}
