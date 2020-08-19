using EntityFrameworkCoreTests.Entities;
using EntityFrameworkCoreTests.Loaders;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace EntityFrameworkCoreTests.Context
{
    public static class BookStoreContextOptionsExtensions
    {
        /// <summary>
        /// Saves books.
        /// </summary>
        /// <param name="books"></param>
        /// <returns></returns>
        public static DbContextOptions<BookStoreContext> SeedWith(this DbContextOptions<BookStoreContext> source, IEnumerable<Book> books)
        {
            using var context = source.ToDbContext();
            context.Books.AddRange(books);
            context.SaveChanges();
            return source;
        }

        /// <summary>
        /// Saves books from the raw test data file.
        /// </summary>
        /// <param name="books"></param>
        /// <returns></returns>
        public static DbContextOptions<BookStoreContext> SeedWith(this DbContextOptions<BookStoreContext> source, string filePath)
        {
            var loadedBooks = BookJsonLoader.LoadBooks(filePath);
            source.SeedWith(loadedBooks);
            return source;
        }
    }
}
