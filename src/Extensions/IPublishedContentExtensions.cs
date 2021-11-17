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
            return content.SkipperWasHere(out _, recursive);
        }
        
        public static bool SkipperWasHere(this IPublishedContent content, out IPublishedContent _content, bool recursive = false)
        {
            // We can return the immediate result, as there is no need for recursion.
            if (content.SkipperWasHere())
            {
                _content = content;
                return true;
            }

            // Some best practice to avoid infinite loops
            int count = 0;
            // Goes back to parents until it finds another Skipper's work
            while (content.Parent != null && content.Parent.Id != 0)
            {
                content = content.Parent;
                if (content.SkipperWasHere())
                {
                    _content = content;
                    return true;
                }

                count++;
                if (count >= SkipperConfiguration.WhileLoopMaxCount) { break; }
            }

            _content = content;
            return content.SkipperWasHere();
        }

        public static bool SkipperIs404OrContent(this IPublishedContent content)
        {
            if (SkipperConfiguration.SkipperWorkReturns404)
            {
                return SkipperConfiguration.SkipperWorkReturns404;
            }

            if (content.Value<bool>(Constants.ReservedSkipPropertyAlias, defaultValue: false))
            {
                return true;
            }

            return false;
        }
    }
}