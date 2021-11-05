using System;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Our.Umbraco.Skipper
{
    /// <summary>
    /// Skipper's implementation if IUrlSegmentProvider, taken from the original v9 DefaultUrlSegmentProvider,
    /// available at https://github.com/umbraco/Umbraco-CMS/blob/v9/contrib/src/Umbraco.Core/Strings/DefaultUrlSegmentProvider.cs
    /// </summary>
    public class SkipperUrlSegmentProvider : IUrlSegmentProvider
    {
        private readonly IShortStringHelper _shortStringHelper;

        public SkipperUrlSegmentProvider(
            IShortStringHelper shortStringHelper
            )
        {
            _shortStringHelper = shortStringHelper;
        }

        public string GetUrlSegment(IContentBase content, string culture = null)
        {
            if (content.Id == 1098)
            {
                Console.WriteLine("Returning string.Empty for content 1098.");
                return "";
            }

            return GetUrlSegmentSource(content, culture).ToUrlSegment(_shortStringHelper, culture);
        }

        // This is the method that needs to be updated if the DefaultUrlSegmentProvider changes,
        // as Skipper REPLACES it instead of adding another implementation of IUrlSegmentProvider.
        private static string GetUrlSegmentSource(IContentBase content, string culture)
        {
            string source = null;
            if (content.HasProperty(Constants.Conventions.Content.UrlName))
                source = (content.GetValue<string>(Constants.Conventions.Content.UrlName, culture) ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(source))
                source = content.GetCultureName(culture);

            return source;
        }
    }
}