using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Our.Umbraco.Skipper.Extensions;
using Our.Umbraco.Skipper.Configuration;
using System.Threading.Tasks;

namespace Our.Umbraco.Skipper
{
    public class SkipperContentFinder : IContentFinder
    {
        private readonly IAppPolicyCache _runtimeCache;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        private readonly ISkipperConfiguration _skipperConfiguration;
        
        public SkipperContentFinder(
            IAppPolicyCache runtimeCache,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISkipperConfiguration skipperConfiguration)
        {
            _runtimeCache = runtimeCache;
            _umbracoContextAccessor = umbracoContextAccessor;
            _skipperConfiguration = skipperConfiguration;
        }
        public Task<bool> TryFindContent(IPublishedRequestBuilder request)
        {
            // Getting the IUmbracoContext
            IUmbracoContext umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

            var cache = _runtimeCache.GetCacheItem<Dictionary<string, int>>(Constants.Cache.Key);
            
            string path = request.Uri.AbsoluteUri;
            if (path.IndexOf('?') != -1)
            {
                path = path.Substring(0, path.IndexOf('?'));
            }
            
            if (cache != null && cache.ContainsKey(path))
            {
                int nodeId = cache[path];
                request.SetPublishedContent(umbracoContext.Content.GetById(nodeId));
                return Task.FromResult(true);
            }

            string culture = request.Culture ?? null;
            var rootNodes = umbracoContext.Content.GetAtRoot(culture: culture);
            
            // We have to check both paths, with and without ending slash
            IPublishedContent item = null;
            item = rootNodes
                    .DescendantsOrSelf<IPublishedContent>(culture: culture)
                    .FirstOrDefault(x => x.Url(culture: culture, mode: UrlMode.Absolute).TrimEnd("/") == path.TrimEnd("/"));

            if (item != null)
            {
                // If skipper was here
                // And the configuration says that skipper's work must return 404
                if (item.SkipperWasHere(_skipperConfiguration, culture) && item.SkipperIs404OrContent(_skipperConfiguration, culture))
                {
                    request.SetIs404();
                    return Task.FromResult(true); // We have to return true in order to stop the contentfinder
                }

                if (cache == null)
                {
                    cache = new Dictionary<string, int>();
                }

                if (!cache.ContainsKey(path))
                {
                    cache.Add(path, item.Id);
                }

                // Inserting skipper's current work into Umbraco's cache
                _runtimeCache.InsertCacheItem<Dictionary<string, int>>(
                    Constants.Cache.Key,
                    () => cache,
                    null,
                    false);

                request.SetPublishedContent(item);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }
    }
}