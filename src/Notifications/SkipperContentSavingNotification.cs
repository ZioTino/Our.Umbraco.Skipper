using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Our.Umbraco.Skipper.Configuration;
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
                bool nameHasChanged = false;
                List<ContentCultureInfos> changedCultureInfos = new List<ContentCultureInfos>();

                // Node has invariant culture
                if (node.CultureInfos.Count == 0)
                {
                    nameHasChanged = node.IsPropertyDirty("Name");
                }
                else
                {
                    foreach (var culture in node.CultureInfos)
                    {
                        changedCultureInfos.Add(culture);
                    }

                    nameHasChanged = changedCultureInfos.Count() > 0;
                }

                // Name or node hasn't changed, so skip the current iteration
                if (node.HasIdentity && !nameHasChanged) { continue; }

                IPublishedContent baseNode;
                try
                {
                    // If node has no parent, we can skip the current iteration
                    // This is only for root nodes
                    if (node.HasIdentity && node.Level == 0 && node.ParentId == 0) { continue; }

                    baseNode = umbracoContext.Content.GetById(node.ParentId);

                    // Some best practice to avoid infinite loops
                    int count = 0;
                    // If baseNode is skipper's work, we need to find the first eligible node to be our rootNode
                    while (baseNode != null && baseNode.Parent != null && baseNode.Parent.Id != 0 && baseNode.SkipperWasHere())
                    {
                        baseNode.Parent.SkipperWasHere(out baseNode, true);

                        count++;
                        if (count >= SkipperConfiguration.WhileLoopMaxCount) { break; }
                    }
                }
                catch { continue; }

                int duplicateNodes = 0;
                int maxNumber = 0;

                foreach (IPublishedContent sibling in baseNode.SiblingsAndSelf())
                {
                    // If for some reasons the baseNode is still Skipper's work we need to check for duplicates anyway
                    if (baseNode.SkipperWasHere())
                    {
                        CheckPublishedContentName(node, sibling, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber);
                    }

                    // We need to check if some of it's children's name is the same as some of it's siblings
                    foreach (IPublishedContent children in sibling.Children())
                    {
                        if (children.SkipperWasHere())
                        {
                            CheckPublishedContentName(node, children, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber);
                        }
                    }
                }

                if (duplicateNodes > 0)
                {
                    // The final space at the end is to make sure that Umbraco doesn't think that this is a duplicate node
                    // If we remove it, the name will be {node.Name} + ({maxNumber + 1}) + (1)
                    // The last (1) is added by angular.js, maybe because Umbraco thinks it's a duplicate name
                    // This is a minor issue IMHO, because the content editor is supposed to modify the name to make it unique anyway afterwards
                    string sectionToAdd = " (" + (maxNumber + 1).ToString() + ") ";
                    if (changedCultureInfos.Count() == 0)
                    {
                        // Node is invariant, we can set name
                        node.Name += sectionToAdd;
                    }
                    else
                    {
                        // We change the culture Name for each culture that has changed
                        foreach (ContentCultureInfos changed in changedCultureInfos)
                        {
                            node.SetCultureName(node.GetCultureName(changed.Culture) + sectionToAdd, changed.Culture);
                        }
                    }
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
                // We have to trim end the name as it may contain spaces
                string childrenName = content.Name.ToLower().TrimEnd();
                string nodeName;
                if (node.CultureInfos.Count == 0)
                {
                    // Node is invariant
                    nodeName = node.Name.ToLower().TrimEnd();

                    // We found a duplicate!
                    if (nodeName.Equals(childrenName))
                    {
                        _duplicateNodes++;
                    }
                    else if (nodeName.IsSkipperDuplicateOf(childrenName))
                    {
                        _duplicateNodes++;
                        _maxNumber = GetNodeNameMaxNumber(childrenName, nodeName, maxNumber);
                    }

                    if (content.SkipperWasHere())
                    {
                        foreach (IPublishedContent children in content.Children())
                        {
                            CheckPublishedContentName(node, children, _duplicateNodes, _maxNumber, out _duplicateNodes, out _maxNumber);
                        }
                    }
                }
                else
                {
                    foreach (var culture in node.CultureInfos)
                    {
                        // We check only if this culture Name has been changed
                        if (!culture.IsPropertyDirty("Name")) { continue; }

                        nodeName = node.GetCultureName(culture.Culture).ToLower().TrimEnd();

                        // We found a duplicate!
                        if (nodeName.Equals(childrenName))
                        {
                            _duplicateNodes++;
                        }
                        else if (nodeName.IsSkipperDuplicateOf(childrenName))
                        {
                            _duplicateNodes++;
                            _maxNumber = GetNodeNameMaxNumber(childrenName, nodeName, maxNumber);
                        }

                        if (content.SkipperWasHere())
                        {
                            foreach (IPublishedContent children in content.Children())
                            {
                                CheckPublishedContentName(node, children, _duplicateNodes, _maxNumber, out _duplicateNodes, out _maxNumber);
                            }
                        }
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