#nullable disable

namespace EntityFrameworkCoreTests.Entities
{
    public class Review
    {
        public int ReviewId { get; set; }
        public string VoterName { get; set; }
        public int NumStars { get; set; }
        public string Connect { get; set; }

        public int BookId { get; set; }
    }
}
