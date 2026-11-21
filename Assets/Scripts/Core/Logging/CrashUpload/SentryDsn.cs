using System;

namespace Game.Core.Logging.CrashUpload
{
    /// <summary>
    /// Sentry 项目的 DSN，例如 <c>https://公钥@o123.ingest.sentry.io/456</c>。
    /// 它说清了往哪发、用哪把钥匙；这把钥匙本来就是要放进游戏里发给玩家的，不是秘密。
    /// </summary>
    public sealed class SentryDsn
    {
        private SentryDsn(string original, string publicKey, string projectId, string envelopeUrl)
        {
            Original = original;
            PublicKey = publicKey;
            ProjectId = projectId;
            EnvelopeUrl = envelopeUrl;
        }

        /// <summary>原样的 DSN。</summary>
        public string Original { get; }

        public string PublicKey { get; }

        public string ProjectId { get; }

        /// <summary>收崩溃的地址（Sentry 的 envelope 接口）。</summary>
        public string EnvelopeUrl { get; }

        /// <summary>解析一个 DSN。写法不对就抛 <see cref="ArgumentException"/>。</summary>
        public static SentryDsn Parse(string dsn)
        {
            if (string.IsNullOrWhiteSpace(dsn) || !Uri.TryCreate(dsn.Trim(), UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException("Sentry DSN 写法不对：" + dsn, nameof(dsn));
            }

            string publicKey = uri.UserInfo.Split(':')[0];
            string path = uri.AbsolutePath.TrimEnd('/');
            int slash = path.LastIndexOf('/');
            string projectId = slash < 0 ? "" : path.Substring(slash + 1);

            if (publicKey.Length == 0 || projectId.Length == 0)
            {
                throw new ArgumentException("Sentry DSN 里缺公钥或项目编号：" + dsn, nameof(dsn));
            }

            // 自建的 Sentry 可能放在一个子路径下：https://key@host/sentry/456 → https://host/sentry/api/456/envelope/
            string prefix = path.Substring(0, slash);
            string authority = uri.Scheme + "://" + uri.Host + (uri.IsDefaultPort ? "" : ":" + uri.Port);
            return new SentryDsn(dsn.Trim(), publicKey, projectId, authority + prefix + "/api/" + projectId + "/envelope/");
        }

        /// <summary>每个请求都要带的 X-Sentry-Auth 头。</summary>
        public string AuthHeader(string clientName) => "Sentry sentry_version=7, sentry_key=" + PublicKey + ", sentry_client=" + clientName;
    }
}
