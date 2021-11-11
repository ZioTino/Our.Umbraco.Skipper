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

        public static bool SkipperWasHere(this IPublishedContent content, bool recursive = false)
        {
            bool result = content.SkipperWasHere();

            // If there is no recursion?
            // And if the result is false
            // We can return the result, as there is no recursion.
            if (!recursive && !result)
            {
                return result;
            }

            // Goes back to parents until it finds another Skipper's work
            while (content.Parent != null && content.Parent.Id != 0)
            {
                content = content.Parent;
                if (content.SkipperWasHere())
                {
                    return true;
                }
            }

            return result;
        }
    }
}