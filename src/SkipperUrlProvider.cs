using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;
using Our.Umbraco.Skipper.Configuration;
using Our.Umbraco.Skipper.Extensions;

namespace Our.Umbraco.Skipper
{
    public class SkipperUrlProvider : DefaultUrlProvider
    {
        private readonly IConfiguration _configuration;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public SkipperUrlProvider(
            IOptions<RequestHandlerSettings> requestSettings, 
            ILogger<DefaultUrlProvider> logger, 
            ISiteDomainMapper siteDomainMapper, 
            IUmbracoContextAccessor umbracoContextAccessor, 
            UriUtility uriUtility,
            IConfiguration configuration) 
            : base(requestSettings, logger, siteDomainMapper, umbracoContextAccessor, uriUtility)
        {
            _configuration = configuration;
            _umbracoContextAccessor = umbracoContextAccessor;
        }

        public override UrlInfo GetUrl(IPublishedContent content, UrlMode mode, string culture, Uri current)
        {
            if (content == null)
            {
                return base.GetUrl(content, mode, culture, current);
            }

            // If Skipper worked directly into this node
            if (content.SkipperWasHere())
            {
                if (SkipperConfiguration.SkipperWorkReturns404)
                {
                    // I can return an empty UrlInfo
                    // And since i cannot simply return new UrlInfo(string.Empty, false, culture);
                    // I have to use a placeholder
                    return new UrlInfo("#", false, culture);   
                }

                // I can return null as it should return normal URL.
                return null;
            }

            bool skipperWasInAncestor = false;
            foreach (IPublishedContent item in content.Ancestors())
            {
                if (item.SkipperWasHere())
                {
                    skipperWasInAncestor = true;
                    break;
                }
            }            
            return skipperWasInAncestor ? BuildUrl(content, current, mode, culture) : null;
        }

        private UrlInfo BuildUrl(IPublishedContent content, Uri current, UrlMode mode, string culture)
        {
            IUmbracoContext umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

            bool hideTopLevelNode = _configuration.GetValue<bool>("Umbraco:CMS:Global:HideTopLevelNodeFromPath", false);
            string[] pathIds = content.Path.Split(',').Skip(hideTopLevelNode ? 2 : 1).Reverse().ToArray();

            UrlInfo url = base.GetUrl(content, mode, culture, current);
            if (url is null)
                throw new ArgumentNullException($"Could not call base GetUrl for content Id {content.Id}.");

            string result = string.Empty;
            string host = string.Empty;
            if (url.Text.StartsWith("http"))
            {
                Uri u = new Uri(url.Text);
                result = url.Text.Replace(u.GetLeftPart(UriPartial.Authority), "");
                host = u.GetLeftPart(UriPartial.Authority);
            }
            else
            {
                result = url.Text;
            }

            if (result.EndsWith("/"))
            {
                result = result.Substring(0, result.Length - 1);
            }
            if (result.StartsWith("/"))
            {
                result = result.Substring(1, result.Length - 1);
            }

            string[] parts = result.Split('/').Reverse().ToArray();

            int index = 0;
            foreach (string p in parts)
            {
                IPublishedContent item = umbracoContext.Content.GetById(int.Parse(pathIds[index]));

                if (item.SkipperWasHere())
                {
                    parts[index] = string.Empty;
                }
                index++;
            }
            bool isTrailingSlashActive = _configuration.GetValue<bool>("Umbraco:CMS:RequestHandler:AddTrailingSlash", true);
            string finalUrl = string.Join("/", parts.Reverse().Where(x => !string.IsNullOrWhiteSpace(x)).ToArray());

            finalUrl = isTrailingSlashActive
                ? finalUrl.EnsureEndsWith("/").EnsureStartsWith("/")
                : finalUrl.EnsureStartsWith("/");

            finalUrl = string.Concat(host, finalUrl);

            return new UrlInfo(finalUrl, true, culture);
        }
    }
}