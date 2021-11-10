using Our.Umbraco.Skipper.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.Skipper
{
    public class SkipperUrlSegmentProvider : IUrlSegmentProvider
    {
        private readonly IShortStringHelper _shortStringHelper;

        public SkipperUrlSegmentProvider(
            IShortStringHelper shortStringHelper)
        {
            _shortStringHelper = shortStringHelper;
        }

        public string GetUrlSegment(IContentBase content, string culture = null)
        {
            // Only apply this UrlSegmentProvider if the setting SkipperWorkReturns404 is set to true
            // So this way we can set even in the index (i guess?) a value that's not the url it's supposed to build.
            if ((SkipperConfiguration.Aliases.Contains(content.ContentType.Alias.ToLower()) || content.GetValue<bool>(Constants.ReservedPropertyAlias)) 
                && SkipperConfiguration.SkipperWorkReturns404)
            {
                return Constants.DefaultValues.HiddenSegment;                
            }

            // This goes to the DefaultUrlSegmentProvider
            return null;
        }
    }
}