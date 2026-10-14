using System.Text;

namespace Xease
{
    public static partial class LnString
    {
        private static readonly StringBuilder Builder = new();

        private static StringBuilder GetStringBuilder(string str = null)
        {
            Builder.Clear();
            if (!string.IsNullOrEmpty(str))
                Builder.Append(str);

            return Builder;
        }
    }
}
