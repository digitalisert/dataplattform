using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using static Digitalisert.Dataplattform.ResourceModel;
using static Digitalisert.Dataplattform.ResourceModelExtensions;

namespace Digitalisert.Dataplattform
{
    public class ResourceClusterIndex : AbstractMultiMapIndexCreationTask<ResourceProperty>
    {
        public ResourceClusterIndex()
        {
            AddMap<ResourceProperty>(resources =>
                from resource in resources
                from type in resource.Type
                from property in resource.Properties.Where(p => p.Tags.Contains("@wkt"))
                from wkt in property.Value
                from encodegeohash in WKTEncodeGeohash(wkt)
                let geohash = encodegeohash.Substring(0, encodegeohash.IndexOf('|'))

                select new ResourceProperty
                {
                    Context = resource.Context,
                    ResourceId = type + "/" + geohash,
                    Name = property.Name,
                    Properties = new[] {
                        new Property {
                            Name = property.Name,
                            Value = new[] { encodegeohash },
                            Resources = new[] {
                                new Resource {
                                    Context = resource.Context,
                                    ResourceId = resource.ResourceId,
                                    Source = new[] { MetadataFor(resource).Value<String>("@id") }
                                }
                            }
                        }
                    },
                    Source = (
                        from ontologyresource in property.Resources
                        from ontologytype in ontologyresource.Type
                        from ontologyresourceproperty in ontologyresource.Properties
                        from i in Enumerable.Range(0, geohash.Length)
                        let parentgeohash = geohash.Substring(0, geohash.Length - i)
                        where !(ontologyresource.Context == resource.Context && ontologytype == type && geohash == parentgeohash && property.Name == ontologyresourceproperty.Name)
                        select "ResourceClusterReferences/" + ontologyresource.Context + "/" + ontologytype + "/" + geohash.Substring(0, geohash.Length - i) + "/" + ontologyresourceproperty.Name
                    ).Union(
                        from ontologyresourceproperty in property.Properties
                        from ontologyresource in ontologyresourceproperty.Resources
                        from ontologytype in ontologyresource.Type
                        from i in Enumerable.Range(0, geohash.Length)
                        let parentgeohash = geohash.Substring(0, geohash.Length - i)
                        where !(ontologyresource.Context == resource.Context && ontologytype == type && geohash == parentgeohash && property.Name == ontologyresourceproperty.Name)
                        select "ResourceClusterReferences/" + ontologyresource.Context + "/" + ontologytype + "/" + geohash.Substring(0, geohash.Length - i) + "/" + ontologyresourceproperty.Name

                    )
                }
            );

            Reduce = results =>
                from result in results
                group result by new { result.Context, result.ResourceId, result.Name } into g
                select new ResourceProperty
                {
                    Context = g.Key.Context,
                    ResourceId = g.Key.ResourceId,
                    Name = g.Key.Name,
                    Properties = g.SelectMany(r => r.Properties),
                    Source = g.SelectMany(r => r.Source).Distinct()
                };

            Index(Raven.Client.Constants.Documents.Indexing.Fields.AllFields, FieldIndexing.No);

            OutputReduceToCollection = "ResourceCluster";
            PatternReferencesCollectionName = "ResourceClusterReferences";
            PatternForOutputReduceToCollectionReferences = r => $"ResourceClusterReferences/{r.Context}/{r.ResourceId}/{r.Name}";

            AdditionalAssemblies = new HashSet<AdditionalAssembly> {
                AdditionalAssembly.FromPath("Digitalisert.Dataplattform.ResourceModel.dll", new HashSet<string> { "Digitalisert.Dataplattform" })
            };
        }
    }
}
