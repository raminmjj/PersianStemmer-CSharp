using System.Text.RegularExpressions;

namespace PersianStemmer.Core.Stemming
{
    internal static class Utils
    {
        public static bool IsEnglish(string input)
        {
            return Regex.IsMatch(input, "[a-z,:/`;'\\?A-Z*+~!@#=\\[\\]{}\\$%^&*().0-9]+");
        }

        public static bool IsNumber(string input)
        {
            return Regex.IsMatch(input, "[0-9,.]+"); // what about "^[-+]?[0-9]*\.?[0-9]*$"   ?
        }
    }
}
