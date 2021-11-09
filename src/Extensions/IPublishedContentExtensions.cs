using Umbraco.Cms.Core.Models.PublishedContent;

namespace Our.Umbraco.Skipper.Extensions
{
    public static class IPublishedContentExtensions
    {
        public static bool SkipperWasHere(this IPublishedContent content)
        {
            if (content.Id == 1098)
                return true;
                
            return false;   
        }
    }
}