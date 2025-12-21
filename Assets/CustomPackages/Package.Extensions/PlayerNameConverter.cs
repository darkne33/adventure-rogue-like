namespace CustomPackages.Package.Extensions
{
    public static class PlayerNameConverter
    {
        private const int MAX_COUNT_SYMBOLS_NICKNAME = 10;

        public static string FormatName(string name, int countSymbols = MAX_COUNT_SYMBOLS_NICKNAME)
        {
            string nickName = name.Length > countSymbols
                ? name.Substring(0, countSymbols) + "..."
                : name;

            return nickName;
        }
    }
}