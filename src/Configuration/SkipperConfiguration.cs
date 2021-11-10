using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Our.Umbraco.Skipper.Configuration
{
    public static class SkipperConfiguration
    {
        private static IConfiguration _configuration;

        public static List<string> Aliases;

        public static bool SkipperWorkReturns404;

        internal static void Initialize(
            IConfiguration configuration)
        {
            _configuration = configuration;
            Aliases = getAliases();
            SkipperWorkReturns404 = _configuration.GetValue<bool>($"{Constants.Configuration.BaseConfigPath}:{Constants.Configuration.HideSkipperWork}", defaultValue: Constants.DefaultValues.HideSkipperWork);
        }

        private static List<string> getAliases()
        {
            var values = _configuration.GetSection($"{Constants.Configuration.BaseConfigPath}:{Constants.Configuration.Aliases}").GetChildren().Select(x => x.Value).ToArray();
            return values.ToList();
        }
    }
}