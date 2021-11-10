using Umbraco.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Our.Umbraco.Skipper.Configuration;

namespace Our.Umbraco.Skipper.Extensions
{
    public static class IPublishedContentExtensions
    {
        public static bool SkipperWasHere(this IPublishedContent content)
        {
            if (SkipperConfiguration.Aliases != null)
            {
                // Check is made always to lower
                if (SkipperConfiguration.Aliases.Contains(content.ContentType.Alias.ToLower()))
                {
                    return true;
                }
            }

            // This is the reserved property alias
            if (content.Value<bool>(Constants.ReservedPropertyAlias, defaultValue: false))
            {
                return true;
            }    
            
            return false;   
        }
    }
}