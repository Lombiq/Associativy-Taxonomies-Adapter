using System;
using System.Collections.Generic;
using System.Linq;
using Associativy.GraphDiscovery;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;

namespace Associativy.Taxonomies.Adapter.Settings
{
    public class AssociativyTaxonomiesAdapterTypeSettings
    {
        public string GraphNamesSerialized { get; set; }

        public string[] GraphNames
        {
            get { return string.IsNullOrEmpty(GraphNamesSerialized) ? new string[0] : GraphNamesSerialized.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries); }
            set { GraphNamesSerialized = string.Join(",", value); }
        }

        public IEnumerable<IGraph> AvailableGraphs { get; set; }
    }


    public class SettingsHooks : ContentDefinitionEditorEventsBase
    {
        private readonly IGraphManager _graphManager;


        public SettingsHooks(IGraphManager graphManager)
        {
            _graphManager = graphManager;
        }
        
    
        public override IEnumerable<TemplateViewModel> TypeEditor(ContentTypeDefinition definition)
        {
            var shadowPart = definition.Parts.SingleOrDefault(part => part.PartDefinition.Name == definition.Name);

            if (shadowPart == null) yield break;

            if (!shadowPart.PartDefinition.Fields.Any(field => field.FieldDefinition.Name == "TaxonomyField"))
            {
                yield break;
            }

            var model = definition.Settings.GetModel<AssociativyTaxonomiesAdapterTypeSettings>();
            model.AvailableGraphs = _graphManager.FindGraphsByContentTypes(definition.Name);
            yield return DefinitionTemplate(model);
        }

        public override IEnumerable<TemplateViewModel> TypeEditorUpdate(ContentTypeDefinitionBuilder builder, IUpdateModel updateModel)
        {
            var model = new AssociativyTaxonomiesAdapterTypeSettings();
            updateModel.TryUpdateModel(model, "AssociativyTaxonomiesAdapterTypeSettings", null, null);
            builder.WithSetting("AssociativyTaxonomiesAdapterTypeSettings.GraphNamesSerialized", model.GraphNamesSerialized);

            yield return DefinitionTemplate(model);
        }
    }
}