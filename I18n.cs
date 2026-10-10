using StardewModdingAPI;

namespace TaskManager
{
    internal static class I18n
    {
        private static ITranslationHelper helper = null!;

        public static void Init(ITranslationHelper translation) => helper = translation;

        public static string Get(string key, object? tokens = null) => helper.Get(key, tokens).ToString();
    }
}
