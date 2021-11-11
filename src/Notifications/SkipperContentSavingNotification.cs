using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Our.Umbraco.Skipper.Extensions;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Our.Umbraco.Skipper.Notifications
{
    public class SkipperContentSavingNotification : INotificationHandler<ContentSavingNotification>
    {
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;

        public SkipperContentSavingNotification(
            IUmbracoContextAccessor umbracoContextAccessor)
        {
            _umbracoContextAccessor = umbracoContextAccessor;            
        }

        public void Handle(ContentSavingNotification notification)
        {
            if (!_umbracoContextAccessor.TryGetUmbracoContext(out IUmbracoContext umbracoContext))
            {
                throw new ArgumentNullException("UmbracoContext");
            }

            // For each node that is being saved
            foreach (IContent node in notification.SavedEntities)
            {
                // Name or node hasn't changed, so skip the current iteration.
                if (node.HasIdentity && !node.IsPropertyDirty("Name")) { continue; }

                IPublishedContent parent;
                try
                {
                    IRememberBeingDirty dirty = node as IRememberBeingDirty;

                    // If node has no parent, we can skip the current iteration.
                    if (node.ParentId == 0 || (!dirty.WasPropertyDirty("Id") && node.Level == 1) || (node.HasIdentity && node.Level == 0)) { continue; }

                    parent = umbracoContext.Content.GetById(node.ParentId);

                    // If parent is home and is not a virtual node, we can skip the current iteration.
                    if (parent == null || parent.Level < 2 || !parent.SkipperWasHere()) { continue; }
                }
                catch { continue; }

                int duplicateNodes = 0;
                int maxNumber = 0;

                foreach (IPublishedContent sibling in parent.Siblings())
                {
                    duplicateNodes = CheckChildrenNameAndSelf(node, sibling, duplicateNodes, maxNumber, out maxNumber);
                }

                if (duplicateNodes > 0)
                {
                    // The final space at the end is to make sure that Umbraco doesn't think that this is a duplicate node
                    // If we remove it, the name will be {node.Name} + ({maxNumber + 1}) + (1)
                    // The last (1) is added by angular.js, presumably because Umbraco thinks it's a duplicate name
                    // This is a minor issue IMHO, because the content editor is supposed to modify the name to make it unique anyway afterwards.
                    node.Name += " (" + (maxNumber + 1).ToString() + ") ";
                }
            }
        }

        private static int CheckChildrenNameAndSelf(IContent node, IPublishedContent sibling, int duplicateNodes, int maxNumber, out int newMaxNumber)
        {
            newMaxNumber = maxNumber;

            newMaxNumber = CheckPublishedContentName(node, sibling, duplicateNodes, newMaxNumber, out duplicateNodes);
            
            if (sibling.SkipperWasHere())
            {
                // As this is skipper's work, we need to check if some of it's children's name is the same as some of it's siblings.
                foreach (IPublishedContent children in sibling.Children())
                {
                    newMaxNumber = CheckPublishedContentName(node, children, duplicateNodes, newMaxNumber, out duplicateNodes);
                }
            }

            return duplicateNodes;
        }

        private static int CheckPublishedContentName(IContent node, IPublishedContent content, int duplicateNodes, int maxNumber, out int newDuplicateNodes)
        {
            newDuplicateNodes = duplicateNodes;

            string childrenName = content.Name.ToLower();
            string nodeName = node.Name.ToLower();

            // We need only other nodes
            if (content.Id == node.Id) { return maxNumber; }

            // We found a duplicate!
            if (nodeName.Equals(childrenName))
            {
                newDuplicateNodes++;
                return maxNumber;
            }
            else if (nodeName.IsSkipperDuplicateOf(childrenName))
            {
                newDuplicateNodes++;
                return GetNodeNameMaxNumber(childrenName, nodeName, maxNumber);
            }

            return maxNumber;
        }

        private static int GetNodeNameMaxNumber(string childrenName, string nodeName, int maxNumber)
        {
            var rgName = new Regex(@"^.+ \((\d+)\)$");
            if (rgName.IsMatch(childrenName))
            {
                int newNumber = int.Parse(rgName.Replace(childrenName, "$1"));
                maxNumber = (maxNumber < newNumber) ? newNumber : maxNumber;
            }
            return maxNumber;
        }
    }
}