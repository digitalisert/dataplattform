using System;
using System.Linq;
using Raven.Client.Documents.Indexes;
using static Digitalisert.Dataplattform.ResourceModel;

namespace Digitalisert.Dataplattform
{
    public class ResourcePropertyAttributeIndex : AbstractMultiMapIndexCreationTask<ResourcePropertyAttribute>
    {
        public ResourcePropertyAttributeIndex()
        {
            AddMap<Resource>(resources =>
                from resource in resources
                from property in resource.Properties.Where(p => p.Tags.Contains("@query"))
                select new ResourcePropertyAttribute
                {
                    Context = resource.Context,
                    ResourceId = resource.ResourceId,
                    Name = property.Name,
                    Query = property.Value.FirstOrDefault()
                }
            );

            Reduce = results =>
                from result in results
                group result by new { result.Context, result.ResourceId, result.Name } into g
                select new ResourcePropertyAttribute
                {
                    Context = g.Key.Context,
                    ResourceId = g.Key.ResourceId,
                    Name = g.Key.Name,
                    Query = g.Select(r => r.Query).FirstOrDefault()
                };

            Index(Raven.Client.Constants.Documents.Indexing.Fields.AllFields, FieldIndexing.No);

            OutputReduceToCollection = "ResourcePropertyAttribute";
            PatternReferencesCollectionName = "ResourcePropertyAttributeReferences";
            PatternForOutputReduceToCollectionReferences = r => $"ResourcePropertyAttributeReferences/{r.Context}/{r.ResourceId}/{r.Name}";
        }
    }
}
