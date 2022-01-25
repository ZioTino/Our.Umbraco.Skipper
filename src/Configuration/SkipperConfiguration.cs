using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Our.Umbraco.Skipper.Configuration
{
    public class SkipperConfiguration : ISkipperConfiguration
    {
        private readonly IConfiguration _configuration;

        public SkipperConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<string> Aliases => getAliases();

        public bool SkipperWorkReturns404 => _configuration.GetValue<bool>($"{Constants.Configuration.BaseConfigPath}:{Constants.Configuration.HideSkipperWork}", defaultValue: Constants.DefaultValues.HideSkipperWork);

        public int WhileLoopMaxCount => _configuration.GetValue<int>($"{Constants.Configuration.BaseConfigPath}:{Constants.Configuration.WhileLoopMaxCount}", defaultValue: Constants.DefaultValues.WhileLoopMaxCount);

        private List<string> getAliases()
        {
            IConfigurationSection aliasesSection = _configuration.GetSection($"{Constants.Configuration.BaseConfigPath}:{Constants.Configuration.Aliases}");
            if (aliasesSection == null)
            {
                return new List<string>();
            }

            IEnumerable<IConfigurationSection> values = aliasesSection.GetChildren();
            if (values.Count() == 0)
            {
                return new List<string>();
            }

            return values.Select(x => x.Value.ToLower()).ToList();
        }
    }
}