using Our.Umbraco.Skipper.Configuration;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.Skipper.Notifications
{
    public class SkipperContentPublishedNotification : INotificationHandler<ContentPublishedNotification>
    {
        private readonly IAppPolicyCache _runtimeCache;
        
        public SkipperContentPublishedNotification(
            IAppPolicyCache runtimeCache)
        {
            _runtimeCache = runtimeCache;
        }
        public void Handle(ContentPublishedNotification notification)
        {
            // This clears the Skipper cache every time a new entity is published
            // TODO: maybe optimize this? Instead of clearing the entire cache, check if the node is in the cache and reset that entry.
            _runtimeCache.ClearByKey(Constants.Cache.Key);
        }
    }
}