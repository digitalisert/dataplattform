using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using static Digitalisert.Dataplattform.ResourceModel;
using static Digitalisert.Dataplattform.ResourceModelExtensions;

namespace Digitalisert.Dataplattform
{
    public class ResourceOntologyIndex : AbstractMultiMapIndexCreationTask<Resource>
    {
        public ResourceOntologyIndex()
        {
            AddMap<ResourceMapping>(resources =>
                from resource in resources
                where resource.Type == null || !resource.Type.Any()
                select new Resource
                {
                    Context = resource.Context,
                    ResourceId = resource.ResourceId,
                    Tags = resource.Tags,
                    Properties = resource.Properties,
                    Source = resource.Source,
                    Modified = MetadataFor(resource).Value<DateTime>("@last-modified")
                }
            );

            AddMap<ResourceMapping>(resources =>
                from resource in resources
                where resource.Type == null || !resource.Type.Any()
                from property in resource.Properties
                from propertyresource in property.Resources
                from propertyresourcetype in propertyresource.Type
                from inverseproperty in propertyresource.Properties
                select new Resource
                {
                    Context = propertyresource.Context,
                    ResourceId = propertyresourcetype,
                    Tags = new string[] { },
                    Properties = new[] {
                        new Property {
                            Name = inverseproperty.Name,
                            Properties = new[] {
                                new Property {
                                    Name = property.Name,
                                    Tags = property.Tags,
                                    Resources = new[] {
                                        new Resource {
                                            Context = resource.Context,
                                            Type = new[] { resource.ResourceId }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    Source = resource.Source,
                    Modified = MetadataFor(resource).Value<DateTime>("@last-modified")
                }
            );

            AddMap<ResourceMapping>(resources =>
                from resource in resources
                from type in resource.Type
                from ontologyreference in LoadDocument<ResourceMappingReferences>("ResourceMappingReferences/" + resource.Context + "/" + type).ReduceOutputs
                let ontology = LoadDocument<ResourceMapping>(ontologyreference)
                where ontology != null

                from ontologyproperty in ontology.Properties.Where(p => !p.Name.StartsWith("@"))
                from ontologyresource in ontologyproperty.Resources

                let resourceIds =
                    from resourceIdProperty in ontologyresource.Properties.Where(p => p.Name == "@resourceId")
                    let derivedproperty =
                        from ontologyderivedproperty in resourceIdProperty.Properties
                        where resourceIdProperty.Tags.Contains("@derive")
                        from derivedproperty in resource.Properties
                        where ontologyderivedproperty.Name == derivedproperty.Name
                            && ontologyderivedproperty.Tags.All(t => derivedproperty.Tags.Contains(t))
                            && (ontologyderivedproperty.From == null || ontologyderivedproperty.From <= (derivedproperty.Thru ?? DateTime.MaxValue))
                            && (ontologyderivedproperty.Thru == null || ontologyderivedproperty.Thru >= (derivedproperty.From ?? DateTime.MinValue))
                        select derivedproperty
                    from resourceIdValue in resourceIdProperty.Value
                    from resourceIdFormattedValue in ResourceFormat(resourceIdValue, resource, derivedproperty)
                    select resourceIdFormattedValue

                from ontologypropertyresource in (
                    from tags in ontologyresource.Tags.Where(t => t == "@pull")
                    from property in resource.Properties.Where(p => p.Name == ontologyproperty.Name)
                    from propertyresource in property.Resources.Where(r => !String.IsNullOrEmpty(r.ResourceId))

                    select new Resource
                    {
                        Context = propertyresource.Context ?? ontology.Context,
                        ResourceId = propertyresource.ResourceId,
                        Tags = new[] { "@pull" },
                        Properties = new Property[] { },
                        Source = new string[] { }
                    }
                ).Union(
                    from tags in ontologyresource.Tags.Where(t => t == "@pull")
                    from resourceId in resourceIds

                    select new Resource
                    {
                        Context = ontologyresource.Context,
                        ResourceId = resourceId,
                        Tags = new[] { "@pull" },
                        Properties = new Property[] { },
                        Source = new string[] { }
                    }
                ).Union(
                    from property in ontologyresource.Properties.Take(1)
                    where ontologyresource.Tags.Contains("@push") || ontologyresource.Properties.Any(p => p.Tags.Contains("@push"))
                    from resourceId in resourceIds

                    select new Resource
                    {
                        Context = ontologyresource.Context,
                        ResourceId = resourceId,
                        Tags = new string[] { "@push" },
                        Properties = new Property[] { },
                        Source = new[] { MetadataFor(resource).Value<String>("@id") }
                    }
                )

                select new Resource
                {
                    Context = ontologypropertyresource.Context,
                    ResourceId = ontologypropertyresource.ResourceId,
                    Tags = ontologypropertyresource.Tags,
                    Properties = ontologypropertyresource.Properties,
                    Source = ontologypropertyresource.Source,
                    Modified = MetadataFor(resource).Value<DateTime>("@last-modified")
                }
            );

            AddMap<ResourceMapping>(resources =>
                from resource in resources
                from type in resource.Type
                from ontologyreference in LoadDocument<ResourceMappingReferences>("ResourceMappingReferences/" + resource.Context + "/" + type).ReduceOutputs
                let ontology = LoadDocument<ResourceMapping>(ontologyreference)
                where ontology != null

                from ontologypropertyresource in (
                    from aliasValue in ontology.Properties.Where(p => p.Name == "@alias").SelectMany(p => p.Value)
                    from aliasFormattedValue in ResourceFormat(aliasValue, resource, null)

                    select new Resource
                    {
                        Context = ontology.Context,
                        ResourceId = aliasFormattedValue,
                        Tags = new[] { "@alias" },
                        Properties = new[] {
                            new Property {
                                Name = "@resource",
                                Resources = new[] {
                                    new Resource {
                                        Context = resource.Context,
                                        ResourceId = resource.ResourceId
                                    }
                                },
                                Source = new[] { MetadataFor(resource).Value<String>("@id") }
                            }
                        },
                        Source = new string[] { }
                    }
                ).Union(
                    from aliasValue in ontology.Properties.Where(p => p.Name == "@alias").SelectMany(p => p.Value)
                    from aliasFormattedValue in ResourceFormat(aliasValue, resource, null)

                    select new Resource
                    {
                        Context = resource.Context,
                        ResourceId = resource.ResourceId,
                        Tags = new string[] { },
                        Properties = new[] {
                            new Property {
                                Name = "@alias",
                                Source = new[] { "ResourceOntologyReferences/" + resource.Context + "/" + aliasFormattedValue }
                            }
                        },
                        Source = new string[] { }
                    }
                )

                select new Resource
                {
                    Context = ontologypropertyresource.Context,
                    ResourceId = ontologypropertyresource.ResourceId,
                    Tags = ontologypropertyresource.Tags,
                    Properties = ontologypropertyresource.Properties,
                    Source = ontologypropertyresource.Source,
                    Modified = MetadataFor(resource).Value<DateTime>("@last-modified")
                }
            );

            Reduce = results =>
                from result in results
                group result by new { result.Context, result.ResourceId } into g
                select new Resource
                {
                    Context = g.Key.Context,
                    ResourceId = g.Key.ResourceId,
                    Tags = g.SelectMany(resource => resource.Tags).Distinct(),
                    Properties =
                        from property in g.SelectMany(r => r.Properties)
                        group property by property.Name into propertyG
                        select new Property {
                            Name = propertyG.Key,
                            Value = propertyG.SelectMany(p => p.Value).Distinct(),
                            Tags = propertyG.SelectMany(p => p.Tags).Distinct(),
                            Resources = propertyG.SelectMany(p => p.Resources).Distinct(),
                            Properties = propertyG.SelectMany(p => p.Properties).Distinct(),
                            Source = propertyG.SelectMany(p => p.Source).Distinct()
                        },
                    Source = g.SelectMany(resource => resource.Source).Distinct(),
                    Modified = g.Select(resource => resource.Modified).Max()
                };

            Index(Raven.Client.Constants.Documents.Indexing.Fields.AllFields, FieldIndexing.No);

            OutputReduceToCollection = "ResourceOntology";
            PatternReferencesCollectionName = "ResourceOntologyReferences";
            PatternForOutputReduceToCollectionReferences = r => $"ResourceOntologyReferences/{r.Context}/{r.ResourceId}";

            AdditionalAssemblies = new HashSet<AdditionalAssembly> {
                AdditionalAssembly.FromPath("Digitalisert.Dataplattform.ResourceModel.dll", new HashSet<string> { "Digitalisert.Dataplattform" })
            };
        }
    }
}
