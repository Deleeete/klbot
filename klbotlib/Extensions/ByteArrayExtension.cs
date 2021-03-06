#pragma warning disable CS1591
using System.Text;

namespace klbotlib.Extensions
{
    public static class ByteArrayExtension
    {
        private static readonly StringBuilder _sb = new StringBuilder();
        public static string ToHexString(this byte[] buffer)
        {
            lock (_sb)
            {
                _sb.Clear();
                foreach (byte b in buffer)
                {
                    _sb.Append(b.ToString("x2"));
                }
                return _sb.ToString();
            }
        }
    }
}
