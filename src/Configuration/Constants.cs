namespace Our.Umbraco.Skipper.Configuration
{
    public static class Constants
    {
        public const string PluginName = "Our.Umbraco.Skipper";

        public const string ReservedPropertyAlias = "umbracoUrlSkipper";

        public class Configuration
        {

            public const string BaseConfigPath = "Umbraco:Skipper";

            public const string HideSkipperWork = "HideSkipperWork";

            public const string Aliases = "Aliases";
        }

        public class DefaultValues
        {
            public const bool ReservedPropertyAlias = false;

            public const bool HideSkipperWork = false;

            public static readonly string[] DefaultAliases = new string[] {};

            public const string HiddenSegment = "#";
        }
        
        public class Cache
        {
            public const string Key = "cachedSkipperWork";
        }
    }
}