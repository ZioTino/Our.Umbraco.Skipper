using System;
using System.Linq;
using Our.Umbraco.Skipper;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.Skipper.Composers
{
    public class RegisterSkipperSegmentProviderComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Insert SkipperUrlProvider before the default one
            builder.UrlProviders().InsertBefore<DefaultUrlProvider, SkipperUrlProvider>();

            // Insert SkipperContentFinder before the default one
            builder.ContentFinders().InsertBefore<ContentFinderByUrl, SkipperContentFinder>();
        }
    }
}