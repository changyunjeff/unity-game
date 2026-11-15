using System;
using System.Globalization;

namespace Game.Core.Versioning
{
    /// <summary>
    /// 游戏的版本号：<c>主.次.修</c> 三个不带前导零的非负整数，例如 <c>0.3.1</c>。
    /// 它只认这一种写法，所以一个版本号只有一种写出来的样子。
    /// </summary>
    public readonly struct GameVersion : IEquatable<GameVersion>
    {
        public GameVersion(int major, int minor, int patch)
        {
            if (major < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major));
            }

            if (minor < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minor));
            }

            if (patch < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(patch));
            }

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>主：发售那天从 0 变成 1。</summary>
        public int Major { get; }

        /// <summary>次：多出一样能玩到或能用上的东西时加 1。</summary>
        public int Minor { get; }

        /// <summary>修：只修错、没加东西时加 1。</summary>
        public int Patch { get; }

        public static bool operator ==(GameVersion left, GameVersion right) => left.Equals(right);

        public static bool operator !=(GameVersion left, GameVersion right) => !left.Equals(right);

        /// <summary>把文字读成版本号。前后的空白和换行不算；写法不对就抛 <see cref="FormatException"/>。</summary>
        public static GameVersion Parse(string text)
        {
            if (TryParse(text, out GameVersion version))
            {
                return version;
            }

            throw new FormatException("版本号要写成 主.次.修 三个数，例如 0.3.1。现在写的是：" + (text ?? "（空）"));
        }

        /// <summary>把文字读成版本号。写法不对返回 false。</summary>
        public static bool TryParse(string text, out GameVersion version)
        {
            version = default;

            if (text == null)
            {
                return false;
            }

            string[] parts = text.Trim().Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            if (!TryParsePart(parts[0], out int major) || !TryParsePart(parts[1], out int minor) || !TryParsePart(parts[2], out int patch))
            {
                return false;
            }

            version = new GameVersion(major, minor, patch);
            return true;
        }

        public bool Equals(GameVersion other) => Major == other.Major && Minor == other.Minor && Patch == other.Patch;

        public override bool Equals(object obj) => obj is GameVersion other && Equals(other);

        public override int GetHashCode() => (((Major * 397) ^ Minor) * 397) ^ Patch;

        public override string ToString()
        {
            return Major.ToString(CultureInfo.InvariantCulture) + "." +
                   Minor.ToString(CultureInfo.InvariantCulture) + "." +
                   Patch.ToString(CultureInfo.InvariantCulture);
        }

        // 一位只能是纯数字，而且不带前导零（"0" 可以，"01" 不行）。
        private static bool TryParsePart(string part, out int value)
        {
            value = 0;

            if (part.Length == 0 || (part.Length > 1 && part[0] == '0'))
            {
                return false;
            }

            foreach (char c in part)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }
    }
}
