using System;
using System.Linq;
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
using Umbraco.Cms.Core.Services;

namespace Our.Umbraco.Skipper
{
	public class SkipperUrlProvider : DefaultUrlProvider
	{
		private readonly IUmbracoContextAccessor _umbracoContextAccessor;

		private readonly ISkipperConfiguration _skipperConfiguration;

		private readonly GlobalSettings _globalSettings;

		private readonly RequestHandlerSettings _requestSettings;

		private readonly IDomainService _domainService;

		public SkipperUrlProvider(
			IOptions<GlobalSettings> globalSettings,
	  ISkipperConfiguration skipperConfiguration,

	  IOptionsMonitor<RequestHandlerSettings> requestSettings,
			ILogger<DefaultUrlProvider> logger,
			ISiteDomainMapper siteDomainMapper,
			IUmbracoContextAccessor umbracoContextAccessor,
			UriUtility uriUtility,
	  ILocalizationService localizationService,
	  IDomainService domainService)
	  : base(requestSettings, logger, siteDomainMapper, umbracoContextAccessor, uriUtility, localizationService)
		{
			_requestSettings = requestSettings.CurrentValue;
			_globalSettings = globalSettings.Value;
			_umbracoContextAccessor = umbracoContextAccessor;
			_skipperConfiguration = skipperConfiguration;
			_domainService = domainService;
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
			IUmbracoContext umbracoContext = _umbracoContextAccessor.GetRequiredUmbracoContext();

			string[] pathIds = content.Path.Split(',')
				.Skip(_globalSettings.HideTopLevelNodeFromPath ? 2 : 1)
				.Reverse()
				.ToArray();

			// Starting from the base Url generated from DefaultUrlProvider
			UrlInfo url = base.GetUrl(content, mode, culture, current);
			if (url is null)
			{
				//throw new ArgumentNullException($"Base GetUrl for Id {content.Id}.");
				// if i cannot get defult url i cannot generate one
				return null;
			}

			// Parsing the host
			//Valid domain names are: "example.com", "www.example.com", "example.com:8080", or "https://www.example.com/".
			//Furthermore also one-level paths in domains are supported, eg. "example.com/en" or "/en".

			string result = string.Empty;
			string host = string.Empty;

			//Get the hostname assigned in Umbraco for the culture
			var cultureHost = RecurseHostnameForCulture(content, culture);

			//check for hostname with directory
			var hostHasDir = false;
			var hostDirectory = "";
			if (!string.IsNullOrEmpty(cultureHost))
			{
				if (cultureHost.StartsWith("/"))
				{
					hostHasDir = true;
					hostDirectory = cultureHost;
				}
				else
				{
					var cultureHostUri = cultureHost.StartsWith("http")
						? new Uri(cultureHost)
						: new Uri($"https://{cultureHost}");
					hostDirectory = cultureHostUri.AbsolutePath;
					hostHasDir = hostDirectory != "/";
				}
			}

			//Separate the "host" from the rest of the URL
			if (cultureHost != "")
			{
				if (url.Text.StartsWith("http"))
				{
					var fullUri = new Uri(url.Text);
					var fullCultureHost = $"{fullUri.Scheme}://{cultureHost}";

					result = fullUri.AbsoluteUri.Replace(fullCultureHost, "");
					host = fullCultureHost;
				}
				else if(hostHasDir)
				{
					result = url.Text.Replace(hostDirectory, "");
					host = cultureHost;
				}
			}
			else if (url.Text.StartsWith("http"))
			{
				Uri u = new Uri(url.Text);
				result = url.Text.Replace(u.GetLeftPart(UriPartial.Authority), "");
				host = u.GetLeftPart(UriPartial.Authority);
			}
			else
			{
				result = url.Text;
			}


			// Handling start/end slashes and HiddenSegment slashes
			// '///' happens when a node is being skipped and its content should return 404, so no URL must be given.
			// To accomplish this, I must replace the middle '/' with another character, no matter wich one because it will be removed after anyway.
			// TODO: Maybe find another way? This seems strange
			if (result.Contains("///"))
			{
				// Some best practice to avoid infinite loops
				int count = 0;
				while (result.Contains("///"))
				{
					result = result.ReplaceFirst("///", "/#/");

					count++;
					if (count >= _skipperConfiguration.WhileLoopMaxCount)
					{
						break;
					}
				}
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
			if (pathIds.Length == parts.Length) //If these don't match we get an IndexOutOfRangeException
			{
				foreach (string p in parts)
				{
					IPublishedContent item = umbracoContext.Content.GetById(int.Parse(pathIds[index]));
					// If the part we are looking is Skipper's work, and Id is NOT the content Id of the content we are building the Url for 
					// (or configuration says we should return 404)
					if (item.SkipperWasHere(_skipperConfiguration, culture) && (item.Id != content.Id ||
							item.SkipperIs404OrContent(_skipperConfiguration, culture)))
					{
						parts[index] = string.Empty;
					}

					index++;
				}
			}


			string finalUrl = string.Join("/", parts.Reverse().Where(x => !string.IsNullOrEmpty(x)).ToArray());
			finalUrl = _requestSettings.AddTrailingSlash
				? finalUrl.EnsureEndsWith("/").EnsureStartsWith("/")
				: finalUrl.EnsureStartsWith("/");

			finalUrl = string.Concat(host, finalUrl);

			return new UrlInfo(finalUrl, true, culture);
		}


		internal string GetHostnameForCulture(IPublishedContent content, string culture)
		{
			var domains = _domainService.GetAssignedDomains(content.Id, false);
			if (domains == null || !domains.Any())
			{
				return null;
			}

			var domain = domains.FirstOrDefault(d => d.LanguageIsoCode == culture);
			return domain?.DomainName;
		}

		internal string RecurseHostnameForCulture(IPublishedContent content, string culture)
		{
			var domain = GetHostnameForCulture(content, culture);
			if (domain !=null)
			{
				return domain;
			}
			else if (content.Parent != null)
			{
				return RecurseHostnameForCulture(content.Parent, culture);
			}
			else
			{
				return null;
			}

		}
	}
}