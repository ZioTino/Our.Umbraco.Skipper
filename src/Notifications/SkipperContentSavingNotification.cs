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
                // Name or node hasn't changed, so skip the current iteration
                if (node.HasIdentity && !node.IsPropertyDirty("Name")) { continue; }

                IPublishedContent baseNode;
                try
                {
                    // If node has no parent, we can skip the current iteration
                    // This is only for root nodes
                    if (node.HasIdentity && node.Level == 0 && node.ParentId == 0) { continue; }

                    if (node.Id == 0) 
                    {
                        // Content is being saved for the first time, so it has still no Id
                        // so we set its parent id as the baseNode
                        baseNode = umbracoContext.Content.GetById(node.ParentId);
                    }
                    else
                    {
                        baseNode = umbracoContext.Content.GetById(node.Id);
                    }

                    // If parent is a virtual node, we must start to check for duplicates from the parent node of the parent
                    // We need this as if we save a content that's at level 2, and at level 2 we have another node that is treated by Skipper,
                    // and inside that node we have a duplicate of the node we are saving, we can't continue
                    if (baseNode != null && baseNode.Parent != null && baseNode.Parent.Id != 0)
                    {
                        // Best practice to prevent infinite loops
                        int count = 0;
                        
                        // With this do/while we search for the closest node eligible for being our rootNode
                        // This means we are searching for a node that is not Skipper's work
                        IPublishedContent parent;
                        do
                        {
                            parent = umbracoContext.Content.GetById(baseNode.Parent.Id);
                            if (parent != null)
                            {
                                baseNode = parent;

                                // If baseNode is not Skipper's work, it means that we have found the node we are looking for
                                if (!baseNode.SkipperWasHere()) { break; }
                            }

                            // Best practice to prevent infinite loops
                            // My guess is that there will never be an Umbraco instance with more than 50 levels of content 
                            count++;
                            if (count >= 50) { break; }
                        }
                        while (parent != null && parent.Parent != null && parent.Parent.Id != 0);
                    }
                }
                catch { continue; }

                int duplicateNodes = 0;
                int maxNumber = 0;

                Console.WriteLine($"Base Node: {baseNode.Name} ({baseNode.Id})");

                foreach (IPublishedContent sibling in baseNode.SiblingsAndSelf())
                {
                    if (baseNode.SkipperWasHere())
                    {
                        CheckPublishedContentName(node, sibling, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber);
                    }
                    // If sibling is a root node (AKA level 1) but it's NOT Skipper's work
                    // we need to enter here anyway to check the rest of Skipper's work if there are some duplicate names
                    else if (sibling.SkipperWasHere() || sibling.Level < 2) 
                    {
                        Console.WriteLine($"Found Skipper's work at node: {sibling.Name} ({sibling.Id})");
                        // As this is skipper's work, we need to check if some of it's children's name is the same as some of it's siblings
                        foreach (IPublishedContent children in sibling.Children())
                        {
                            CheckPublishedContentName(node, children, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber);
                        }
                    }
                }

                if (duplicateNodes > 0)
                {
                    // The final space at the end is to make sure that Umbraco doesn't think that this is a duplicate node
                    // If we remove it, the name will be {node.Name} + ({maxNumber + 1}) + (1)
                    // The last (1) is added by angular.js, presumably because Umbraco thinks it's a duplicate name
                    // This is a minor issue IMHO, because the content editor is supposed to modify the name to make it unique anyway afterwards
                    node.Name += " (" + (maxNumber + 1).ToString() + ") ";
                }
            }
        }

        private static void CheckPublishedContentName(IContent node, IPublishedContent content, int duplicateNodes, int maxNumber, out int _duplicateNodes, out int _maxNumber)
        {
            _duplicateNodes = duplicateNodes;
            _maxNumber = maxNumber;

            // We need only other nodes
            if (content.Id != node.Id)
            {
                // We have to trim end the name as it may contain spaces (see comment at #64)
                string childrenName = content.Name.ToLower().TrimEnd();
                string nodeName = node.Name.ToLower().TrimEnd();

                Console.WriteLine($"Checking if content {content.Name} ({content.Id}) is duplicate of {node.Name} ({node.Id})");

                // We found a duplicate!
                if (nodeName.Equals(childrenName))
                {
                    _duplicateNodes++;
                    Console.WriteLine($"Found duplicate with Equals! Duplicate nodes: {_duplicateNodes}");
                }
                else if (nodeName.IsSkipperDuplicateOf(childrenName))
                {
                    _duplicateNodes++;
                    _maxNumber = GetNodeNameMaxNumber(childrenName, nodeName, maxNumber);
                    Console.WriteLine($"Found duplicate with IsSkipperDuplicateOf! Duplicate nodes: {_duplicateNodes}. Max number: {_maxNumber}.");
                }

                if (content.SkipperWasHere())
                {
                    Console.WriteLine($"Found additional Skipper's work: {content.Name} ({content.Id})");
                    foreach (IPublishedContent children in content.Children())
                    {
                        CheckPublishedContentName(node, children, _duplicateNodes, _maxNumber, out _duplicateNodes, out _maxNumber);
                    }
                }
            }
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