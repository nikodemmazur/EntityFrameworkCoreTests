using System.Collections.Generic;

#nullable disable

namespace EntityFrameworkCoreTests.Entities
{
    /// <summary>
    /// The Many-to-Many linking table between the <see cref="Entities.Book"/>s and <see cref="Entities.Author"/>s tables.
    /// </summary>
    public class BookAuthor
    {
        public int BookId { get; set; }
        public int AuthorId { get; set; }
        public byte Order { get; set; }

        public Book Book { get; set; }
        public Author Author { get; set; }
    }
}
