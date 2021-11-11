using Our.Umbraco.Skipper.Notifications;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.Skipper.Composers
{
    public class RegisterSkipperComposer : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Insert SkipperUrlSegmentProvider before the default one
            builder.UrlSegmentProviders().InsertBefore<DefaultUrlSegmentProvider, SkipperUrlSegmentProvider>();

            // Insert SkipperUrlProvider before the default one
            builder.UrlProviders().InsertBefore<DefaultUrlProvider, SkipperUrlProvider>();

            // Insert SkipperContentFinder before the default one
            builder.ContentFinders().InsertBefore<ContentFinderByUrl, SkipperContentFinder>();

            // Add notification handlers
            builder.AddNotificationHandler<ContentPublishedNotification, SkipperContentPublishedNotification>();
            builder.AddNotificationHandler<ContentSavingNotification, SkipperContentSavingNotification>();
        }
    }
}