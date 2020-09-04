using EntityFrameworkCoreTests.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EntityFrameworkCoreTests.Loaders
{
    public static class BookJsonLoader
    {
        private static DateTime DecodePublishDate(string publishedDate)
        {
            var split = publishedDate.Split('-');
            switch (split.Length)
            {
                case 1:
                    return new DateTime(int.Parse(split[0]), 1, 1);
                case 2:
                    return new DateTime(int.Parse(split[0]), int.Parse(split[1]), 1);
                case 3:
                    return new DateTime(int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]));
            }

            throw new InvalidOperationException($"The JSON publishedDate failed to decode: string was {publishedDate}");
        }

        /// <summary>
        /// This create the right number of reviews that add up to the average rating
        /// </summary>
        /// <param name="averageRating"></param>
        /// <param name="ratingsCount"></param>
        /// <returns></returns>
        private static ICollection<Review> CalculateReviewsToMatch(double averageRating, int ratingsCount)
        {
            var reviews = new List<Review>();
            var currentAve = averageRating;
            for (int i = 0; i < ratingsCount; i++)
            {
                reviews.Add(new Review
                {
                    VoterName = "anonymous",
                    NumStars = (int)(currentAve > averageRating ? Math.Truncate(averageRating) : Math.Ceiling(averageRating))
                });
                currentAve = reviews.Average(x => x.NumStars);
            }
            return reviews;
        }

        private static Book CreateBookWithRefs(BookInfoJson bookInfoJson, Dictionary<string, Author> authorDict)
        {
            var book = new Book
            {
                Title = bookInfoJson.title,
                Description = bookInfoJson.description,
                PublishedOn = DecodePublishDate(bookInfoJson.publishedDate),
                Publisher = bookInfoJson.publisher,
                Price = (decimal)(bookInfoJson.saleInfoListPriceAmount ?? -1),
                ImageUrl = bookInfoJson.imageLinksThumbnail
            };

            byte i = 0;
            book.AuthorsLink = new List<BookAuthor>();
            foreach (var author in bookInfoJson.authors)
            {
                book.AuthorsLink.Add(new BookAuthor { Book = book, Author = authorDict[author], Order = i++ });
            }

            if (bookInfoJson.averageRating != null)
                book.Reviews = CalculateReviewsToMatch((double)bookInfoJson.averageRating, bookInfoJson.ratingsCount ?? 0);

            return book;
        }

        public static IEnumerable<Book> LoadBooks(string filePath)
        {
            var jsonDecoded = JsonConvert.DeserializeObject<ICollection<BookInfoJson>>(File.ReadAllText(filePath));

            var authorDict = new Dictionary<string, Author>();
            foreach (var bookInfoJson in jsonDecoded)
            {
                foreach (var author in bookInfoJson.authors)
                {
                    if (!authorDict.ContainsKey(author))
                        authorDict[author] = new Author { Name = author };
                }
            }

            return jsonDecoded.Select(x => CreateBookWithRefs(x, authorDict));
        }
    }
}
