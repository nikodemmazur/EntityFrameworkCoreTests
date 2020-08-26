using System.Text;

namespace EntityFrameworkCoreTests.Logging
{
    public class SimpleLineWriter : ILineWritable
    {
        private readonly StringBuilder _sb = new StringBuilder();

        public string GetString() => _sb.ToString();

        public void WriteLine(string line) => _sb.AppendLine(line);
    }
}
