using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using static Digitalisert.Dataplattform.ResourceModel;
using static Digitalisert.Dataplattform.ResourceModelExtensions;

namespace Digitalisert.Dataplattform
{
    public class ResourcePropertyIndex : AbstractMultiMapIndexCreationTask<Resource>
    {
        public ResourcePropertyIndex()
        {
            AddMap<ResourceMapping>(resources =>
                from resource in resources
                from ontology in
                    from type in resource.Type
                    from ontologyreference in LoadDocument<ResourceMappingReferences>("ResourceMappingReferences/" + resource.Context + "/" + type).ReduceOutputs
                    select LoadDocument<ResourceMapping>(ontologyreference)
                where !ontology.Tags.Contains("@fetch") || LoadDocument<ResourceOntologyReferences>("ResourceOntologyReferences/" + resource.Context + "/" + resource.ResourceId) != null
                select new Resource {
                    Context = resource.Context,
                    ResourceId = resource.ResourceId,
                    Type = resource.Type,
                    SubType = resource.SubType,
                    Title = resource.Title,
                    SubTitle = resource.SubTitle,
                    Code = resource.Code,
                    Status = resource.Status,
                    Tags = resource.Tags,
                    Properties = (
                        from ontologyproperty in ontology.Properties.Where(p => !p.Name.StartsWith("@"))
                        let property = (!ontologyproperty.Tags.Contains("@derive")) ? resource.Properties.Where(p => p.Name == ontologyproperty.Name) :
                            from ontologyderivedproperty in ontologyproperty.Properties
                            from derivedproperty in resource.Properties
                            where ontologyderivedproperty.Name == derivedproperty.Name
                                && ontologyderivedproperty.Tags.All(t => derivedproperty.Tags.Contains(t))
                                && (ontologyderivedproperty.From == null || ontologyderivedproperty.From <= (derivedproperty.Thru ?? DateTime.MaxValue))
                                && (ontologyderivedproperty.Thru == null || ontologyderivedproperty.Thru >= (derivedproperty.From ?? DateTime.MinValue))
                            select derivedproperty

                        select new Property {
                            Name = ontologyproperty.Name,
                            Value = property.SelectMany(p => p.Value).Concat(ontologyproperty.Value.SelectMany(v => ResourceFormat(v, resource, property))),
                            Tags = property.SelectMany(p => p.Tags).Union(ontologyproperty.Tags).Select(v => v).Distinct(),
                            Resources = (
                                from propertyresource in property.SelectMany(p => p.Resources)
                                where String.IsNullOrEmpty(propertyresource.ResourceId)
                                select propertyresource
                            ).Union(
                                from source in (
                                    from propertyresource in property.SelectMany(p => p.Resources)
                                    where !String.IsNullOrEmpty(propertyresource.ResourceId)
                                    select LoadDocument<ResourceMappingReferences>("ResourceMappingReferences/" + (propertyresource.Context ?? ontology.Context) + "/" + propertyresource.ResourceId).ReduceOutputs
                                ).Union(
                                    from ontologyresource in ontologyproperty.Resources
                                    from resourceId in
                                        from resourceIdValue in ontologyresource.Properties.Where(p => p.Name == "@resourceId").SelectMany(p => p.Value)
                                        from resourceIdFormattedValue in ResourceFormat(resourceIdValue, resource, property)
                                        select resourceIdFormattedValue
                                    select LoadDocument<ResourceMappingReferences>("ResourceMappingReferences/" + (ontologyresource.Context ?? ontology.Context) + "/" + resourceId).ReduceOutputs
                                ).Union(
                                    from ontologyresource in ontologyproperty.Resources
                                    from resourceId in
                                        from resourceIdValue in ontologyresource.Properties.Where(p => p.Name == "@resourceId").SelectMany(p => p.Value)
                                        from resourceIdFormattedValue in ResourceFormat(resourceIdValue, resource, property)
                                        select resourceIdFormattedValue
                                    let aliasreference = LoadDocument<ResourceOntologyReferences>("ResourceOntologyReferences/" + (ontologyresource.Context ?? ontology.Context) + "/" + resourceId)
                                    from alias in LoadDocument<ResourceOntology>(aliasreference.ReduceOutputs).Where(r => r.Tags.Contains("@alias"))
                                    from resourceproperty in alias.Properties.Where(p => p.Name == "@resource")
                                    select resourceproperty.Source
                                ).Union(
                                    from ontologyresource in ontologyproperty.Resources
                                    from resourceId in
                                        from resourceIdValue in ontologyresource.Properties.Where(p => p.Name == "@resourceId").SelectMany(p => p.Value)
                                        from resourceIdFormattedValue in ResourceFormat(resourceIdValue, resource, property)
                                        select resourceIdFormattedValue
                                    let aliasreference = LoadDocument<ResourceOntologyReferences>("ResourceOntologyReferences/" + (ontologyresource.Context ?? ontology.Context) + "/" + resourceId)
                                    from alias in LoadDocument<ResourceOntology>(aliasreference.ReduceOutputs)
                                    from aliasproperty in alias.Properties.Where(p => p.Name == "@alias")
                                    from aliaspropertyreference in LoadDocument<ResourceOntologyReferences>(aliasproperty.Source)
                                    from aliaspropertyresource in LoadDocument<ResourceOntology>(aliaspropertyreference.ReduceOutputs)
                                    from resourceproperty in aliaspropertyresource.Properties.Where(p => p.Name == "@resource")
                                    select resourceproperty.Source
                                )
                                from propertyresource in
                                    from resourcemapping in LoadDocument<ResourceMapping>(source)
                                    let propertyresourceontologyreference = LoadDocument<ResourceMappingReferences>(resourcemapping.Type.Select(type => "ResourceMappingReferences/" + resourcemapping.Context + "/" + type))
                                    let propertyresourceontology = LoadDocument<ResourceMapping>(propertyresourceontologyreference.SelectMany(r => r.ReduceOutputs))

                                    select new Resource {
                                        Context = resourcemapping.Context,
                                        ResourceId = resourcemapping.ResourceId,
                                        Type = resourcemapping.Type,
                                        SubType = resourcemapping.SubType,
                                        Title = resourcemapping.Title,
                                        SubTitle = resourcemapping.SubTitle,
                                        Code = resourcemapping.Code,
                                        Status = resourcemapping.Status,
                                        Tags = resourcemapping.Tags,
                                        Properties =
                                            from resourcemappingontologyproperty in propertyresourceontology.SelectMany(r => r.Properties).Where(p => p.Name.StartsWith("@"))
                                            let derivedproperties =
                                                from derivedproperty in resourcemapping.Properties
                                                where resourcemappingontologyproperty.Tags.Contains("@derive")
                                                from ontologyderivedproperty in resourcemappingontologyproperty.Properties
                                                where ontologyderivedproperty.Name == derivedproperty.Name
                                                    && ontologyderivedproperty.Tags.All(t => derivedproperty.Tags.Contains(t))
                                                    && (ontologyderivedproperty.From == null || ontologyderivedproperty.From <= (derivedproperty.Thru ?? DateTime.MaxValue))
                                                    && (ontologyderivedproperty.Thru == null || ontologyderivedproperty.Thru >= (derivedproperty.From ?? DateTime.MinValue))
                                                select derivedproperty
                                            select new Property {
                                                Name = resourcemappingontologyproperty.Name,
                                                Value = (
                                                    from value in resourcemappingontologyproperty.Value
                                                    from formattedvalue in ResourceFormat(value, resourcemapping, derivedproperties)
                                                    select formattedvalue
                                                ).Where(v => !String.IsNullOrWhiteSpace(v))
                                            },
                                        Source = resourcemapping.Source
                                    }
                                select new Resource {
                                    Context = propertyresource.Context,
                                    ResourceId = propertyresource.ResourceId,
                                    Type = propertyresource.Type.Union(propertyresource.Properties.Where(p => p.Name == "@type").SelectMany(p => p.Value)).Distinct(),
                                    SubType = propertyresource.SubType.Union(propertyresource.Properties.Where(p => p.Name == "@subtype").SelectMany(p => p.Value)).Distinct(),
                                    Title = propertyresource.Title.Union(propertyresource.Properties.Where(p => p.Name == "@title").SelectMany(p => p.Value)).Distinct(),
                                    SubTitle = propertyresource.SubTitle.Union(propertyresource.Properties.Where(p => p.Name == "@subtitle").SelectMany(p => p.Value)).Distinct(),
                                    Code = propertyresource.Code.Union(propertyresource.Properties.Where(p => p.Name == "@code").SelectMany(p => p.Value)).Distinct(),
                                    Status = propertyresource.Status.Union(propertyresource.Properties.Where(p => p.Name == "@status").SelectMany(p => p.Value)).Distinct(),
                                    Tags = propertyresource.Tags.Union(propertyresource.Properties.Where(p => p.Name == "@tags").SelectMany(p => p.Value)).Distinct(),
                                    Source = propertyresource.Source
                                }
                            ).Union(
                                ontologyproperty.Resources.Where(r => !r.Properties.Any(p => p.Name == "@resourceId"))
                            ),
                            Properties = property.SelectMany(p => p.Properties).Union(ontologyproperty.Properties)
                        }).Where(p => p.Value.Any() || p.Resources.Any()).Union(ontology.Properties.Where(p => p.Name.StartsWith("@"))),
                    Source = new[] { MetadataFor(resource).Value<String>("@id")},
                    Modified = MetadataFor(resource).Value<DateTime>("@last-modified")
                }
            );

            AddMap<ResourceOntology>(ontologies =>
                from ontology in ontologies.Where(r => r.Tags.Contains("@push") || r.Properties.Any(p => p.Tags.Contains("@push")))
                from pushresource in (
                    from tags in ontology.Tags.Where(t => t == "@push" || t == "@pull")
                    select new Resource {
                        Context = ontology.Context,
                        ResourceId = ontology.ResourceId
                    }
                ).Union(
                    from tags in ontology.Tags.Where(t => t == "@alias")
                    from resourceproperty in ontology.Properties.Where(p => p.Name == "@resource")
                    from aliasresource in resourceproperty.Resources
                    select new Resource {
                        Context = aliasresource.Context,
                        ResourceId = aliasresource.ResourceId
                    }
                ).Union(
                    from tags in ontology.Tags.Where(t => t == "@pull")
                    from aliasproperty in ontology.Properties.Where(p => p.Name == "@alias")
                    from aliasreference in LoadDocument<ResourceOntologyReferences>(aliasproperty.Source).Where(r => r != null)
                    from aliasresource in LoadDocument<ResourceOntology>(aliasreference.ReduceOutputs).Where(r => r != null)
                    from resourceproperty in aliasresource.Properties.Where(p => p.Name == "@resource")
                    from resourcealias in resourceproperty.Resources
                    select new Resource {
                        Context = resourcealias.Context,
                        ResourceId = resourcealias.ResourceId
                    }
                )

                from resource in LoadDocument<ResourceMapping>(ontology.Source).Where(r => r != null)
                select new Resource {
                    Context = pushresource.Context,
                    ResourceId = pushresource.ResourceId,
                    Type = ontology.Properties.Where(p => p.Name == "@type").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    SubType = ontology.Properties.Where(p => p.Name == "@subtype").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    Title = ontology.Properties.Where(p => p.Name == "@title").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    SubTitle = ontology.Properties.Where(p => p.Name == "@subtitle").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    Code = ontology.Properties.Where(p => p.Name == "@code").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    Status = ontology.Properties.Where(p => p.Name == "@status").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    Tags = ontology.Properties.Where(p => p.Name == "@tags").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null)).Distinct(),
                    Properties = (
                        from ontologyproperty in ontology.Properties.Where(p => !p.Name.StartsWith("@"))
                        let property = (!ontologyproperty.Tags.Contains("@derive")) ? resource.Properties.Where(p => p.Name == ontologyproperty.Name) :
                            from ontologyderivedproperty in ontologyproperty.Properties
                            from derivedproperty in resource.Properties
                            where ontologyderivedproperty.Name == derivedproperty.Name
                                && ontologyderivedproperty.Tags.All(t => derivedproperty.Tags.Contains(t))
                                && (ontologyderivedproperty.From == null || ontologyderivedproperty.From <= (derivedproperty.Thru ?? DateTime.MaxValue))
                                && (ontologyderivedproperty.Thru == null || ontologyderivedproperty.Thru >= (derivedproperty.From ?? DateTime.MinValue))
                            select derivedproperty

                        select new Property {
                            Name = ontologyproperty.Name,
                            Value = property.SelectMany(p => p.Value).Concat(ontologyproperty.Value.SelectMany(v => ResourceFormat(v, resource, property))),
                            Tags = property.SelectMany(p => p.Tags).Union(ontologyproperty.Tags).Select(v => v).Distinct(),
                            Resources = (
                                from propertyresource in property.SelectMany(p => p.Resources)
                                where String.IsNullOrEmpty(propertyresource.ResourceId)
                                select propertyresource
                            ).Union(
                                from propertyresource in property.SelectMany(p => p.Resources)
                                where !String.IsNullOrEmpty(propertyresource.ResourceId)
                                select new Resource {
                                    Context = propertyresource.Context ?? ontology.Context,
                                    ResourceId = propertyresource.ResourceId
                                }
                            ).Union(
                                from ontologyresource in ontologyproperty.Resources
                                from resourceId in 
                                    from resourceIdValue in ontologyresource.Properties.Where(p => p.Name == "@resourceId").SelectMany(p => p.Value)
                                    from resourceIdFormattedValue in ResourceFormat(resourceIdValue, resource, property)
                                    select resourceIdFormattedValue
                                select new Resource {
                                    Context = ontologyresource.Context ?? ontology.Context,
                                    ResourceId = resourceId
                                }
                            ).Union(
                                ontologyproperty.Resources.Where(r => !r.Properties.Any(p => p.Name == "@resourceId"))
                            ),
                            Properties = property.SelectMany(p => p.Properties).Union(ontologyproperty.Properties),
                            Source = (ontologyproperty.Tags.Contains("@push")) ? new[] { MetadataFor(resource).Value<String>("@id") } : new string[] { },
                        }).Where(p => p.Value.Any() || p.Resources.Any()).Union(ontology.Properties.Where(p => p.Name.StartsWith("@") && p.Name != "@resource")),
                    Source = (ontology.Tags.Contains("@push")) ? new[] { MetadataFor(resource).Value<String>("@id")} : new string[] { },
                    Modified = MetadataFor(resource).Value<DateTime>("@last-modified")
                }
            );

            Reduce = results =>
                from result in results
                group result by new { result.Context, result.ResourceId } into g

                let computedProperties =
                    from property in g.SelectMany(r => r.Properties).Where(p => p.Name.StartsWith("@") && !p.Tags.Contains("@reasoning"))
                    select new Property {
                        Name = property.Name,
                        Value = (
                            from value in property.Value
                            from resource in g.ToList()
                            from formattedvalue in ResourceFormat(value, resource, null)
                            select formattedvalue
                        ).Where(v => !String.IsNullOrWhiteSpace(v))
                    }

                select new Resource {
                    Context = g.Key.Context,
                    ResourceId = g.Key.ResourceId,
                    Type = g.SelectMany(r => r.Type).Union(computedProperties.Where(p => p.Name == "@type").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    SubType = g.SelectMany(r => r.SubType).Union(computedProperties.Where(p => p.Name == "@subtype").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    Title = g.SelectMany(r => r.Title).Union(computedProperties.Where(p => p.Name == "@title").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    SubTitle = g.SelectMany(r => r.SubTitle).Union(computedProperties.Where(p => p.Name == "@subtitle").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    Code = g.SelectMany(r => r.Code).Union(computedProperties.Where(p => p.Name == "@code").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    Status = g.SelectMany(r => r.Status).Union(computedProperties.Where(p => p.Name == "@status").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    Tags = g.SelectMany(r => r.Tags).Union(computedProperties.Where(p => p.Name == "@tags").SelectMany(p => p.Value)).Select(v => v.ToString()).Distinct(),
                    Properties = (IEnumerable<Property>)Properties(g.SelectMany(r => r.Properties)),
                    Source = g.SelectMany(r => r.Source).Distinct(),
                    Modified = g.Select(r => r.Modified).Max()
                };

            Index(Raven.Client.Constants.Documents.Indexing.Fields.AllFields, FieldIndexing.No);

            OutputReduceToCollection = "ResourceProperty";
            PatternReferencesCollectionName = "ResourcePropertyReferences";
            PatternForOutputReduceToCollectionReferences = r => $"ResourcePropertyReferences/{r.Context}/{r.ResourceId}";

            AdditionalAssemblies = new HashSet<AdditionalAssembly> {
                AdditionalAssembly.FromPath("Digitalisert.Dataplattform.ResourceModel.dll", new HashSet<string> { "Digitalisert.Dataplattform" })
            };
        }
    }
}
