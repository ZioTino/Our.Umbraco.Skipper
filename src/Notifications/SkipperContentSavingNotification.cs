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

        private readonly ISkipperConfiguration _skipperConfiguration;

        public SkipperContentSavingNotification(
            IUmbracoContextAccessor umbracoContextAccessor,
            ISkipperConfiguration skipperConfiguration)
        {
            _umbracoContextAccessor = umbracoContextAccessor;
            _skipperConfiguration = skipperConfiguration;
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
                    foreach (var c in node.CultureInfos)
                    {
                        changedCultureInfos.Add(c);
                    }

                    nameHasChanged = changedCultureInfos.Count() > 0;
                }

                // Name or node hasn't changed, so skip the current iteration
                if (node.HasIdentity && !nameHasChanged) { continue; }

                // Node has invariant culture
                if (node.CultureInfos.Count == 0)
                {
                    IPublishedContent baseNode;
                    bool anyInPathIsSkipperWork = false;
                    try
                    {
                        baseNode = FindRootNode(umbracoContext, node, out anyInPathIsSkipperWork, null);
                        if (baseNode == null) continue;
                    }
                    catch { continue; }

                    int duplicateNodes = 0;
                    int maxNumber = 0;

                    IEnumerable<IPublishedContent> siblingsAndSelf = new List<IPublishedContent>() { baseNode };
                    if (anyInPathIsSkipperWork) siblingsAndSelf = baseNode.SiblingsAndSelf();

                    if (siblingsAndSelf.Any())
                    {
                        foreach (IPublishedContent sibling in siblingsAndSelf)
                        {
                            // If for some reasons the baseNode is still Skipper's work we need to check for duplicates at his same level
                            if (baseNode.SkipperWasHere(_skipperConfiguration, culture: null))
                            {
                                CheckPublishedContentName(node, sibling, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber, null);
                            }

                            // We need to check if some of it's children's is the same as some of it's siblings
                            foreach (IPublishedContent children in sibling.Children())
                            {
                                CheckPublishedContentName(node, children, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber, null);
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
                        node.Name += sectionToAdd;
                    }
                }
                else
                {
                    foreach (ContentCultureInfos cultureInfos in node.CultureInfos)
                    {
                        IPublishedContent baseNode;
                        string culture = null;
                        bool anyInPathIsSkipperWork = false;
                        if (cultureInfos is not null) // Just in case
                        {
                            culture = cultureInfos.Culture;
                        }

                        try
                        {
                            baseNode = FindRootNode(umbracoContext, node, out anyInPathIsSkipperWork, culture);
                            if (baseNode == null) continue;
                        }
                        catch { continue; }

                        int duplicateNodes = 0;
                        int maxNumber = 0;

                        IEnumerable<IPublishedContent> siblingsAndSelf = new List<IPublishedContent>() { baseNode };
                        if (anyInPathIsSkipperWork) siblingsAndSelf = baseNode.SiblingsAndSelf();

                        if (siblingsAndSelf.Any())
                        {
                            foreach (IPublishedContent sibling in siblingsAndSelf)
                            {
                                // If for some reasons the baseNode is still Skipper's work we need to check for duplicates at the same level
                                if (baseNode.SkipperWasHere(_skipperConfiguration, culture))
                                {
                                    CheckPublishedContentName(node, sibling, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber, cultureInfos);
                                }

                                // We need to check if some of it's children's is the same as some of it's siblings
                                foreach (IPublishedContent children in sibling.Children(culture))
                                {
                                    CheckPublishedContentName(node, children, duplicateNodes, maxNumber, out duplicateNodes, out maxNumber, cultureInfos);
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

                            // We change the culture Name for each culture that has changed
                            node.SetCultureName(node.GetCultureName(culture) + sectionToAdd, culture);
                        }
                    }
                }

            }
        }

        private IPublishedContent FindRootNode(IUmbracoContext umbracoContext, IContent node, out bool anyInPathIsSkipperWork, string culture = null)
        {
            IPublishedContent baseNode;
            anyInPathIsSkipperWork = false;
            // If node has no parent, we can skip the current iteration
            // This is only for root nodes
            if (node.HasIdentity && node.Level == 0 && node.ParentId == 0) { return null; }

            baseNode = umbracoContext.Content.GetById(node.ParentId);
            anyInPathIsSkipperWork = baseNode.SkipperWasHere(_skipperConfiguration, culture);

            // Some best practice to avoid infinite loops
            int count = 0;
            // If baseNode is skipper's work, we need to find the first eligible node to be our rootNode
            while (baseNode != null && baseNode.Parent != null && baseNode.Parent.Id != 0 && baseNode.SkipperWasHere(_skipperConfiguration, culture))
            {
                baseNode = baseNode.Parent;
                if (baseNode.SkipperWasHere(_skipperConfiguration, culture)) anyInPathIsSkipperWork = true;

                count++;
                if (count >= _skipperConfiguration.WhileLoopMaxCount) { break; }
            }

            return baseNode;
        }

        private void CheckPublishedContentName(IContent node, IPublishedContent content, int duplicateNodes, int maxNumber, out int _duplicateNodes, out int _maxNumber, ContentCultureInfos cultureInfos = null)
        {
            _duplicateNodes = duplicateNodes;
            _maxNumber = maxNumber;

            // We need only other nodes
            if (content.Id != node.Id)
            {
                // We have to trim end the name as it may contain spaces
                string childrenName = content.Name.ToLower().TrimEnd();
                string nodeName;

                string culture = null;
                if (cultureInfos is not null)
                {
                    culture = cultureInfos.Culture;
                }

                // If node is NOT invariant
                if (!string.IsNullOrEmpty(culture))
                {
                    // We check if this culture name has been changed
                    if (!cultureInfos.IsPropertyDirty("Name")) { return; }

                    nodeName = node.GetCultureName(culture).ToLower().TrimEnd();
                }
                // Else if node IS invariant
                else
                {
                    nodeName = node.Name.ToLower().TrimEnd();
                }

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
                
                if (content.SkipperWasHere(_skipperConfiguration, culture))
                {
                    foreach (IPublishedContent children in content.Children(culture))
                    {
                        CheckPublishedContentName(node, children, _duplicateNodes, _maxNumber, out _duplicateNodes, out _maxNumber, cultureInfos);
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