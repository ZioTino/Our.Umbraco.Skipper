using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis.Hunspell;
using Our.Umbraco.Skipper.Extensions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Our.Umbraco.Skipper
{
    public class SkipperContentFinder : IContentFinder
    {
        private readonly IAppPolicyCache _runtimeCache;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        
        public SkipperContentFinder(
            IAppPolicyCache runtimeCache,
            IUmbracoContextAccessor umbracoContextAccessor)
        {
            _runtimeCache = runtimeCache;
            _umbracoContextAccessor = umbracoContextAccessor;
        }
        public bool TryFindContent(IPublishedRequestBuilder request)
        {
            IUmbracoContext umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

            var cache = _runtimeCache.GetCacheItem<Dictionary<string, int>>("cachedSkipperWork");
            
            string path = request.Uri.AbsoluteUri;
            if (path.IndexOf('?') != -1)
            {
                path = path.Substring(0, path.IndexOf('?'));
            }
            
            if (cache != null && cache.ContainsKey("path"))
            {
                int nodeId = cache[path];
                request.SetPublishedContent(umbracoContext.Content.GetById(nodeId));
                return true;
            }

            string culture = request.Culture;

            var rootNodes = umbracoContext.Content.GetAtRoot(culture: culture);
            IPublishedContent item = null;
            item = rootNodes
                    .DescendantsOrSelf<IPublishedContent>(culture: culture)
                    .FirstOrDefault(x => x.Url(culture: culture, mode: UrlMode.Absolute) == (path + "/") || x.Url(culture: culture, mode: UrlMode.Absolute) == path);

            if (item != null)
            {
                if (item.SkipperWasHere())
                {
                    request.SetIs404();
                    return true;
                }

                if (cache == null)
                {
                    cache = new Dictionary<string, int>();
                }

                if (!cache.ContainsKey(path))
                {
                    cache.Add(path, item.Id);
                }

                _runtimeCache.InsertCacheItem<Dictionary<string, int>>(
                    "cachedSkipperWork",
                    () => cache,
                    null,
                    false);

                request.SetPublishedContent(item);
                return true;
            }

            return false;
        }
    }
}