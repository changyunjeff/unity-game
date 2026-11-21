using System.Globalization;
using System.Text;

namespace Game.Core.Logging.CrashUpload
{
    /// <summary>把一段文字写成 JSON 字符串（带引号、转义）。拼 Sentry 的请求只用到这一点 JSON，不值得为它引一个库。</summary>
    public static class JsonText
    {
        public static string Quote(string text)
        {
            if (text == null)
            {
                return "null";
            }

            var builder = new StringBuilder(text.Length + 2);
            builder.Append('"');

            foreach (char c in text)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < ' ')
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }

                        break;
                }
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
