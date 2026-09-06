using System.Globalization;

namespace PLAYERTWO.ARPGProject
{
    public static class NumberExtensions
    {
        /// <summary>
        /// Returns a given integer formatted with thousands separators (e.g. 13910 -> "13,910").
        /// </summary>
        public static string ToMoneyString(this int amount) =>
            amount.ToString("N0", CultureInfo.InvariantCulture);
    }
}
