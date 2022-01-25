using Umbraco.Extensions;
using Umbraco.Cms.Core.Models.PublishedContent;
using Our.Umbraco.Skipper.Configuration;

namespace Our.Umbraco.Skipper.Extensions
{
    public static class IPublishedContentExtensions
    {
        public static bool SkipperWasHere(this IPublishedContent content, ISkipperConfiguration _skipperConfiguration, string culture = null)
        {
            if (_skipperConfiguration.Aliases != null)
            {
                // Check is made always to lower
                if (_skipperConfiguration.Aliases.Contains(content.ContentType.Alias.ToLower()))
                {
                    return true;
                }
            }

            // This is the reserved property alias
            if (content.HasProperty(Constants.ReservedPropertyAlias))
            {    
                if (content.Value<bool>(Constants.ReservedPropertyAlias, culture, defaultValue: false))
                {
                    return true;
                }

                // Maybe we have invariant value?
                if (content.Value<bool>(Constants.ReservedPropertyAlias, defaultValue: false))
                {
                    return true;
                }    
            }
            
            return false;   
        }

        public static bool SkipperWasHere(this IPublishedContent content, ISkipperConfiguration _skipperConfiguration, string culture = null, bool recursive = false)
        {
            return content.SkipperWasHere(out _, _skipperConfiguration, culture, recursive);
        }
        
        public static bool SkipperWasHere(this IPublishedContent content, out IPublishedContent _content, ISkipperConfiguration _skipperConfiguration, string culture = null, bool recursive = false)
        {
            // We can return the immediate result, as there is no need for recursion.
            if (content.SkipperWasHere(_skipperConfiguration, culture))
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
                if (content.SkipperWasHere(_skipperConfiguration, culture))
                {
                    _content = content;
                    return true;
                }

                count++;
                if (count >= _skipperConfiguration.WhileLoopMaxCount) { break; }
            }

            _content = content;
            return content.SkipperWasHere(_skipperConfiguration, culture);
        }

        public static bool SkipperIs404OrContent(this IPublishedContent content, ISkipperConfiguration _skipperConfiguration, string culture = null)
        {
            if (_skipperConfiguration.SkipperWorkReturns404)
            {
                return _skipperConfiguration.SkipperWorkReturns404;
            }

            if (content.HasProperty(Constants.ReservedSkipPropertyAlias))
            {
                if (content.Value<bool>(Constants.ReservedSkipPropertyAlias, culture, defaultValue: false))
                {
                    return true;
                }

                // Maybe we have invariant value?
                if (content.Value<bool>(Constants.ReservedSkipPropertyAlias, defaultValue: false))
                {
                    return true;
                }
            }

            return false;
        }
    }
}