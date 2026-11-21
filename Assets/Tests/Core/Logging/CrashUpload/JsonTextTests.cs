using Game.Core.Logging.CrashUpload;
using NUnit.Framework;

namespace Game.Core.Tests.Logging.CrashUpload
{
    public class JsonTextTests
    {
        [Test]
        public void 引号_反斜杠_换行和控制字符都转义_中文原样()
        {
            Assert.That(JsonText.Quote("读档\"失败\"\\a\n\r\t\u0001"), Is.EqualTo("\"读档\\\"失败\\\"\\\\a\\n\\r\\t\\u0001\""));
        }

        [Test]
        public void 空的是null()
        {
            Assert.That(JsonText.Quote(null), Is.EqualTo("null"));
            Assert.That(JsonText.Quote(""), Is.EqualTo("\"\""));
        }
    }
}
