using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using static Digitalisert.Dataplattform.ResourceModel;
using static Digitalisert.Dataplattform.ResourceModelExtensions;

namespace Digitalisert.Dataplattform
{
    public class ResourceIndex : AbstractMultiMapIndexCreationTask<Resource>
    {
        public ResourceIndex()
        {
            AddMap<Resource>(resources =>
                from resource in resources
                let source = LoadDocument<ResourceMapping>(resource.Source).Where(r => r != null)
                let properties = 
                    from property in resource.Properties
                    select new Property {
                        Name = property.Name,
                        Value = property.Value,
                        Tags = property.Tags,
                        Resources = (
                            from propertyresource in property.Resources
                            where propertyresource.ResourceId == null
                            select propertyresource
                        ).Union(
                            from propertyresource in property.Resources
                            where propertyresource.ResourceId != null
                            let propertyresourcereduceoutputs = LoadDocument<ResourceReferences>("ResourceReferences/" + propertyresource.Context + "/" + propertyresource.ResourceId).ReduceOutputs
                            let propertyresourceoutputs = LoadDocument<Resource>(propertyresourcereduceoutputs)
                            select new Resource {
                                Context = propertyresource.Context,
                                ResourceId = propertyresource.ResourceId,
                                Type = propertyresourceoutputs.SelectMany(r => r.Type).Distinct(),
                                SubType = propertyresourceoutputs.SelectMany(r => r.SubType).Distinct(),
                                Title = propertyresourceoutputs.SelectMany(r => r.Title).Distinct(),
                                SubTitle = propertyresourceoutputs.SelectMany(r => r.SubTitle).Distinct(),
                                Code = propertyresourceoutputs.SelectMany(r => r.Code).Distinct(),
                                Status = propertyresourceoutputs.SelectMany(r => r.Status).Distinct(),
                                Tags = propertyresourceoutputs.SelectMany(r => r.Tags).Distinct()
                            }
                        )
                    }
                let body = source.SelectMany(r => r.Body ?? new string[] { }).Union(properties.Where(p => p.Name == "@body").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, null))).Distinct()
                select new Resource
                {
                    Context = resource.Context,
                    ResourceId = resource.ResourceId,
                    Type = resource.Type,
                    SubType = resource.SubType,
                    Title = resource.Title,
                    SubTitle = resource.SubTitle,
                    Code = resource.Code,
                    Body = body,
                    Status = resource.Status,
                    Tags = resource.Tags.Union(properties.Where(p => p.Name == "@tags").SelectMany(p => p.Value).SelectMany(v => ResourceFormat(v, resource, properties))).Select(v => v.ToString()).Distinct(),
                    Properties = properties.Where(p => !p.Name.StartsWith("@")),
                    _ = (
                        from property in properties
                        group property by property.Name into propertyG
                        select CreateField(
                            propertyG.Key,
                            propertyG.Where(p => !p.Tags.Contains("@wkt")).SelectMany(p => p.Value).Select(v => v.ToString()).Union(
                                from propertyresource in propertyG.SelectMany(p => p.Resources)
                                from fieldvalue in new[] { propertyresource.ResourceId }.Union(propertyresource.Code).Union(propertyresource.Title)
                                select fieldvalue
                            ).Where(v => !String.IsNullOrWhiteSpace(v)).Distinct()
                        )
                    ).Union(
                        from property in properties
                        group property by property.Name into propertyG
                        from resourcetype in propertyG.SelectMany(p => p.Resources).SelectMany(r => r.Type).Distinct()
                        select CreateField(
                            propertyG.Key + "." + resourcetype,
                            (
                                from propertyresource in propertyG.SelectMany(p => p.Resources).Where(r => r.Type.Contains(resourcetype))
                                from fieldvalue in new[] { propertyresource.ResourceId }.Union(propertyresource.Code).Union(propertyresource.Title)
                                select fieldvalue
                            ).Where(v => !String.IsNullOrWhiteSpace(v)).Distinct()
                        )
                    ).Union(
                        new object[] {
                            CreateField(
                                "@resources",
                                properties.SelectMany(p => p.Resources).Select(r => r.Context + "/" + r.ResourceId).Distinct()
                            )
                        }
                    ).Union(
                        new object[] {
                            CreateField(
                                "Properties",
                                properties.Select(p => p.Name).Where(n => !n.StartsWith("@")).Distinct(),
                                new CreateFieldOptions { Indexing = FieldIndexing.Exact }
                            )
                        }
                    ).Union(
                        new object[] {
                            CreateField(
                                "Search",
                                resource.Title.Union(resource.SubTitle).Union(resource.Code).Union(body).Distinct(),
                                new CreateFieldOptions { Indexing = FieldIndexing.Search, Storage = FieldStorage.Yes, TermVector = FieldTermVector.WithPositionsAndOffsets }
                            )
                        }
                    )
                }
            );

            Index(r => r.Context, FieldIndexing.Exact);
            Index(r => r.Type, FieldIndexing.Exact);
            Index(r => r.SubType, FieldIndexing.Exact);
            Index(r => r.Code, FieldIndexing.Exact);
            Index(r => r.Status, FieldIndexing.Exact);
            Index(r => r.Tags, FieldIndexing.Exact);
            Index(r => r.Properties, FieldIndexing.No);

            Index(r => r.Title, FieldIndexing.Search);
            Index(r => r.SubTitle, FieldIndexing.Search);
            Index(r => r.Body, FieldIndexing.Search);

            Store(r => r.Context, FieldStorage.Yes);
            Store(r => r.Type, FieldStorage.Yes);
            Store(r => r.SubType, FieldStorage.Yes);
            Store(r => r.Title, FieldStorage.Yes);
            Store(r => r.SubTitle, FieldStorage.Yes);
            Store(r => r.Code, FieldStorage.Yes);
            Store(r => r.Body, FieldStorage.Yes);
            Store(r => r.Status, FieldStorage.Yes);
            Store(r => r.Tags, FieldStorage.Yes);
            Store(r => r.Properties, FieldStorage.Yes);

            Analyzers.Add(x => x.Title, "SimpleAnalyzer");
            Analyzers.Add(x => x.SubTitle, "SimpleAnalyzer");
            Analyzers.Add(x => x.Body, "SimpleAnalyzer");

            AdditionalAssemblies = new HashSet<AdditionalAssembly> {
                AdditionalAssembly.FromPath("Digitalisert.Dataplattform.ResourceModel.dll", new HashSet<string> { "Digitalisert.Dataplattform" })
            };
        }
    }
}
