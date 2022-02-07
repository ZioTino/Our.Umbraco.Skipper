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
using System.Globalization;

namespace Our.Umbraco.Skipper
{
    public class SkipperUrlProvider : DefaultUrlProvider
    {
        private readonly IConfiguration _configuration;

        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        private readonly ISkipperConfiguration _skipperConfiguration;

        public SkipperUrlProvider(
            IOptions<RequestHandlerSettings> requestSettings, 
            ILogger<DefaultUrlProvider> logger, 
            ISiteDomainMapper siteDomainMapper, 
            IUmbracoContextAccessor umbracoContextAccessor, 
            UriUtility uriUtility,
            IConfiguration configuration,
            ISkipperConfiguration skipperConfiguration) 
            : base(requestSettings, logger, siteDomainMapper, umbracoContextAccessor, uriUtility)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _configuration = configuration;
            _skipperConfiguration = skipperConfiguration;
        }

        public override UrlInfo GetUrl(IPublishedContent content, UrlMode mode, string culture, Uri current)
        {
            // Culture sometimes can be lowercase, which leads to problems further into the process.
            // I.E. PublishedCultureInfo inside IPublishedContent.Cultures has lowercase culture
            // Checking anyway if it's empty, just in case
            if (!string.IsNullOrEmpty(culture))
            {
                culture = new CultureInfo(culture).Name;
            }

            // Just in case the content is null, we return to the DefaultUrlProvider
            if (content == null)
            {
                return base.GetUrl(content, mode, culture, current);
            }

            // If Skipper worked directly into this node
            if (content.SkipperWasHere(_skipperConfiguration, culture))
            {
                if (content.SkipperIs404OrContent(_skipperConfiguration, culture))
                {
                    // I can return an empty UrlInfo
                    // And since i cannot simply return new UrlInfo(string.Empty, false, culture);
                    // I have to use a placeholder
                    return new UrlInfo(Constants.DefaultValues.HiddenSegment, false, culture);   
                }

                // As there might be a multi-level Skipper work, we need to check it here.
                // If there are no other nodes in the path that Skipper worked into
                if (!content.Parent.SkipperWasHere(_skipperConfiguration, culture, recursive: true))
                {                    
                    // I can return null as it should return normal URL.
                    return null;
                }
            }

            // We still need to handle self for Url building
            bool skipperWasInAncestor = false;
            foreach (IPublishedContent item in content.AncestorsOrSelf())
            {
                if (item.SkipperWasHere(_skipperConfiguration, culture))
                {
                    skipperWasInAncestor = true;
                    break;
                }
            }
            return skipperWasInAncestor ? BuildUrl(content, current, mode, culture) : null;
        }

        private UrlInfo BuildUrl(IPublishedContent content, Uri current, UrlMode mode, string culture)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext umbracoContext))
            {
                throw new ArgumentNullException("UmbracoContext");
            }

            bool hideTopLevelNode = _configuration.GetValue<bool>("Umbraco:CMS:Global:HideTopLevelNodeFromPath", false);
            string[] pathIds = content.Path.Split(',').Skip(hideTopLevelNode ? 2 : 1).Reverse().ToArray();

            // Starting from the base Url generated from DefaultUrlProvider
            UrlInfo url = base.GetUrl(content, mode, culture, current);
            if (url is null)
                throw new ArgumentNullException($"Base GetUrl for Id {content.Id}.");

            // Parsing the host
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

            // Handling start/end slashes
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
                // If the part we are looking is Skipper's work, and Id is NOT the content Id of the content we are building the Url for 
                // (or configuration says we should return 404)
                if (item.SkipperWasHere(_skipperConfiguration, culture) && (item.Id != content.Id || item.SkipperIs404OrContent(_skipperConfiguration, culture)))
                {
                    parts[index] = string.Empty;
                }
                index++;
            }

            bool isTrailingSlashActive = _configuration.GetValue<bool>("Umbraco:CMS:RequestHandler:AddTrailingSlash", true);
            string finalUrl = string.Join("/", parts.Reverse().Where(x => !string.IsNullOrEmpty(x)).ToArray());

            finalUrl = isTrailingSlashActive
                ? finalUrl.EnsureEndsWith("/").EnsureStartsWith("/")
                : finalUrl.EnsureStartsWith("/");

            finalUrl = string.Concat(host, finalUrl);

            return new UrlInfo(finalUrl, true, culture);
        }
    }
}