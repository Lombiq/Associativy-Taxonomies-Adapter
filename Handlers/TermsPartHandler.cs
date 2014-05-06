using System.Collections.Generic;
using System.Linq;
using Associativy.GraphDiscovery;
using Associativy.Taxonomies.Adapter.Settings;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment;
using Orchard.Taxonomies.Models;

namespace Associativy.Taxonomies.Adapter.Handlers
{
    public class TermsPartHandler : ContentHandler
    {
        private readonly Work<IGraphManager> _graphManagerWork;


        public TermsPartHandler(Work<IGraphManager> graphManagerWork)
        {
            _graphManagerWork = graphManagerWork;

            // No need to think about removal as built-in Associativy handlers will take care of removing connections then.

            IEnumerable<int> termIdsBeforeUpdate = Enumerable.Empty<int>();

            OnUpdating<TermsPart>((context, part) =>
                {
                    if (!IsTermGraphBuildingEnabled(part)) return;

                    // Enforcing enumeration with ToArry(). Otherwise OnUpdated() would see the values with the new part.Terms.
                    termIdsBeforeUpdate = part.Terms.Select(term => term.TermRecord.ContentItemRecord.Id).ToArray();
                });

            OnUpdated<TermsPart>((context, part) =>
                {
                    if (!IsTermGraphBuildingEnabled(part)) return;

                    var settings = part.ContentItem.TypeDefinition.Settings.GetModel<AssociativyTaxonomiesAdapterTypeSettings>();
                    var graphManager = _graphManagerWork.Value;

                    var graphs = new List<IGraphDescriptor>();

                    foreach (var graphName in settings.GraphNames)
                    {
                        var graph = graphManager.FindGraphByName(graphName);
                        if (graph != null) graphs.Add(graph);
                    }

                    if (!graphs.Any()) return;

                    var itemId = part.ContentItem.Id;

                    var termIdsAfterUpdate = part.Terms.Select(term => term.TermRecord.ContentItemRecord.Id);

                    var removedTermIds = termIdsBeforeUpdate.Except(termIdsAfterUpdate);
                    foreach (var removedTermId in removedTermIds)
                    {
                        foreach (var graph in graphs)
                        {
                            graph.Services.ConnectionManager.Disconnect(itemId, removedTermId);
                        }
                    }

                    // Here we don't care if a term was already connected, we connect it again. Reason is that it might be that the terms
                    // where added but this item's content type was not set up for term graph building before.
                    foreach (var termPart in part.TermParts)
                    {
                        foreach (var graph in graphs.Where(graph => graph.ContentTypes.Contains(termPart.TermPart.ContentItem.ContentType)))
                        {
                            graph.Services.ConnectionManager.Connect(itemId, termPart.TermPart.ContentItem.Id);
                        }
                    }
                });
        }


        private bool IsTermGraphBuildingEnabled(TermsPart part)
        {
            var settings = part.ContentItem.TypeDefinition.Settings.GetModel<AssociativyTaxonomiesAdapterTypeSettings>();
            return settings != null && settings.GraphNames.Any();
        }
    }
}