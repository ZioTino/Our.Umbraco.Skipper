using System;
using System.Linq;
using Our.Umbraco.Skipper;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.Skipper.Composers
{
    public class RegisterSkipperSegmentProviderComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            builder.UrlSegmentProviders().InsertBefore<DefaultUrlSegmentProvider, SkipperUrlSegmentProvider>();
            builder.UrlSegmentProviders().Remove<DefaultUrlSegmentProvider>();
        }
    }
}