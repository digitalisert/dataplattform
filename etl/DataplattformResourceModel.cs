using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq.Indexing;
using static Digitalisert.Dataplattform.ResourceModel;
using static Digitalisert.Dataplattform.ResourceModelExtensions;

namespace etl
{
    public class DataplattformResourceModel
    {
        public class DataplattformResourceIndex : AbstractMultiMapIndexCreationTask<Resource>
        {
            public class Drupal : Dictionary<string, Dictionary<string, object>[]> { }

            public DataplattformResourceIndex()
            {
                AddMap<Drupal>(noder =>
                    from node in noder.WhereEntityIs<Drupal>("Dataplattform")
                    let metadata = MetadataFor(node)
                    where metadata.Value<string>("@id").StartsWith("Dataplattform/Drupal/Node")
                    from resource in (
                        from resourceid in node["resourceid"]
                        select new Resource {
                            Context = (node["context"].Length > 0) ? node["context"][0]["value"].ToString() : "Dataplattform",
                            ResourceId = resourceid["value"].ToString()
                        }
                    ).Union(
                        from resource in node["resource"]
                        let uuid = resource["target_uuid"].ToString()
                        select new Resource {
                            Context = uuid.Substring(0, uuid.IndexOf('/')),
                            ResourceId = uuid.Substring(uuid.IndexOf('/') + 1)
                        }
                    )
                    select new Resource
                    {
                        Context = resource.Context,
                        ResourceId = resource.ResourceId,
                        Type = node["resourcetype"].Select(t => t["value"].ToString()),
                        Title = node["title"].Select(t => t["value"].ToString()),
                        Body = node["body"].Select(b => b["value"].ToString()),
                        Properties =
                            from property in node["properties"]
                            let paragraph = LoadDocument<Drupal>("Dataplattform/Drupal/Paragraph/" + property["target_id"], "Dataplattform")
                            from name in paragraph["name"]
                            select new Property {
                                Name = name["value"].ToString(),
                                Value = paragraph["value"].Select(v => v["value"].ToString())
                            },
                        Source = new[] { metadata.Value<string>("@id") },
                        Modified = DateTime.MinValue
                    }
                );

                Reduce = results  =>
                    from result in results
                    group result by new { result.Context, result.ResourceId } into g
                    select new Resource
                    {
                        Context = g.Key.Context,
                        ResourceId = g.Key.ResourceId,
                        Type = g.SelectMany(r => r.Type).Distinct(),
                        Title = g.SelectMany(r => r.Title).Distinct(),
                        Body = g.SelectMany(r => r.Body).Distinct(),
                        Properties = (IEnumerable<Property>)Properties(g.SelectMany(r => r.Properties)),
                        Source = g.SelectMany(resource => resource.Source).Distinct(),
                        Modified = g.Select(resource => resource.Modified).Max()
                    };

                Index(Raven.Client.Constants.Documents.Indexing.Fields.AllFields, FieldIndexing.No);

                OutputReduceToCollection = "DataplattformResource";

                AdditionalAssemblies = new HashSet<AdditionalAssembly> {
                    AdditionalAssembly.FromPath("Digitalisert.Dataplattform.ResourceModel.dll", new HashSet<string> { "Digitalisert.Dataplattform" })
                };
            }
        }
    }
}