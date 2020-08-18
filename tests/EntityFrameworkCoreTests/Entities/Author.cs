using System.Collections.Generic;

#nullable disable

namespace EntityFrameworkCoreTests.Entities
{
    public class Author
    {
        public int AuthorId { get; set; }
        public string Name { get; set; }

        public ICollection<BookAuthor> BooksLink { get; set; }
    }
}
