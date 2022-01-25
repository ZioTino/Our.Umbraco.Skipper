using System;
using Our.Umbraco.Skipper.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Strings;

namespace Our.Umbraco.Skipper
{
    public class SkipperUrlSegmentProvider : IUrlSegmentProvider
    {
        private readonly IUrlSegmentProvider _urlSegmentProvider;

        private readonly ISkipperConfiguration _skipperConfiguration;

        public SkipperUrlSegmentProvider(
            IShortStringHelper shortStringHelper,
            ISkipperConfiguration skipperConfiguration)
        {
            _urlSegmentProvider = new DefaultUrlSegmentProvider(shortStringHelper);
            _skipperConfiguration = skipperConfiguration;
        }

        public string GetUrlSegment(IContentBase content, string culture = null)
        {
            // Only apply this UrlSegmentProvider if the setting SkipperWorkReturns404 is set to true
            // So this way we can set even in the index (i guess?) a value that's not the url it's supposed to build.

            bool reservedPropertyAliasValue = false; 
            if (content.HasProperty(Constants.ReservedPropertyAlias))
            {
                // Try to set culture variant
                reservedPropertyAliasValue = content.GetValue<bool>(Constants.ReservedPropertyAlias, culture);
                
                // Value is still false, maybe there's an invariant value?
                if (!reservedPropertyAliasValue)
                {
                    reservedPropertyAliasValue = content.GetValue<bool>(Constants.ReservedPropertyAlias);
                }
            }

            bool reservedSkipPropertyAliasValue = false;
            if (content.HasProperty(Constants.ReservedSkipPropertyAlias))
            {
                // Try to set culture variant
                reservedSkipPropertyAliasValue = content.GetValue<bool>(Constants.ReservedSkipPropertyAlias, culture);

                // Value is still false, maybe there's an invariant value?
                if (!reservedSkipPropertyAliasValue)
                {
                    reservedSkipPropertyAliasValue = content.GetValue<bool>(Constants.ReservedSkipPropertyAlias);
                }
            }

            if ((_skipperConfiguration.Aliases.Contains(content.ContentType.Alias.ToLower()) || reservedPropertyAliasValue) 
                && (_skipperConfiguration.SkipperWorkReturns404 || reservedSkipPropertyAliasValue))
            {
                return Constants.DefaultValues.HiddenSegment;                
            }

            // This goes to the DefaultUrlSegmentProvider
            return null;
        }
    }
}