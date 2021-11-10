using Microsoft.Extensions.Configuration;
using Umbraco.Cms.Core.Composing;
using Our.Umbraco.Skipper.Configuration;

namespace Our.Umbraco.Skipper.Components
{
    public class SkipperComposer : ComponentComposer<SkipperComponent> {}
    public class SkipperComponent : IComponent
    {
        private readonly IConfiguration _configuration;

        public SkipperComponent(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void Initialize() => SkipperConfiguration.Initialize(_configuration);

        public void Terminate() {}
    }
}