using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Session;

namespace Digitalisert.Dataplattform.Studio.Models
{
    public class ResourceModel
    {
        public class Resource
        {
            public Resource() { }
            public string Id { get; set; }
            public string Context { get; set; }
            public string ResourceId { get; set; }
            public IEnumerable<string> Type { get; set; }
            public IEnumerable<string> SubType { get; set; }
            public IEnumerable<string> Title { get; set; }
            public IEnumerable<string> SubTitle { get; set; }
            public IEnumerable<string> Code { get; set; }
            public IEnumerable<string> Body { get; set; }
            public IEnumerable<string> Status { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<string[]> Classification { get; set; }
            public IEnumerable<Property> Properties { get; set; }
            public IEnumerable<string> Source { get; set; }
            public IEnumerable<object> _ { get; set; }
        }

        public class Property
        {
            public string Name { get; set; }
            public IEnumerable<string> Value { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<Resource> Resources { get; set; }
            public IEnumerable<Property> Properties { get; set; }
            public IEnumerable<string> Source { get; set; }
        }

        public class ResourceMapping : Resource { }

        public class ResourceReferences {
            public string[] ReduceOutputs { get; set; }
        }

        public class ResourceIndex : AbstractMultiMapIndexCreationTask<Resource> { }

        public static List<Facet> Facets = new List<Facet>
        {
            new Facet { FieldName = "Context" },
            new Facet { FieldName = "Type" },
            new Facet { FieldName = "SubType" },
            new Facet { FieldName = "Tags" },
            new Facet { FieldName = "Status" },
            new Facet { FieldName = "Properties" }
        };

        public static IDocumentQuery<Resource> QueryByExample(IDocumentQuery<Resource> query, IEnumerable<Resource> examples)
        {
            foreach(var example in examples)
            {
                query.OrElse();
                query.OpenSubclause();

                var fields = new[] {
                    new { Name = "Context", Values = Enumerable.Repeat(example.Context, 1).Where(v => v != null) },
                    new { Name = "Type", Values = example.Type },
                    new { Name = "SubType", Values = example.SubType },
                    new { Name = "Code", Values = example.Code },
                    new { Name = "Status", Values = example.Status },
                    new { Name = "Tags", Values = example.Tags },
                    new { Name = "Properties", Values = (example.Properties ?? new Property[] { }).Select(p => p.Name) }
                };

                foreach(var field in fields)
                {
                    foreach (var value in (field.Values ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                    {
                        if (value.StartsWith("-"))
                        {
                            query.WhereNotEquals(field.Name, value.TrimStart('-'));
                        }
                        else if (value.EndsWith("*"))
                        {
                            query.WhereStartsWith(field.Name, value.TrimEnd('*'));
                        }
                        else
                        {
                            query.WhereEquals(field.Name, value, exact: true);
                        }
                    }
                }

                foreach(var property in example.Properties ?? new Property[] { })
                {
                    foreach(var value in (property.Value ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                    {
                        query.WhereEquals(property.Name, value, false);
                    }

                    foreach(var resource in property.Resources ?? new Resource[] { })
                    {
                        var resourcevalues = (new [] { resource.ResourceId ?? ""} )
                            .Union(resource.Code ?? new string[] { })
                            .Union(resource.Title ?? new string[] { });

                        foreach(var value in resourcevalues.Where(v => !String.IsNullOrWhiteSpace(v)))
                        {
                            query.WhereEquals(property.Name, value);
                        }
                    }
                }

                foreach(var title in (example.Title ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                {
                    query.Search("Title", title, SearchOperator.And);
                }

                foreach(var subTitle in (example.SubTitle ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                {
                    query.Search("SubTitle", subTitle, SearchOperator.And);
                }

                foreach(var body in (example.Body ?? new string[] { }).Where(v => !String.IsNullOrWhiteSpace(v)))
                {
                    query.Search("Body", body, SearchOperator.And);
                }

                query.CloseSubclause();
            }

            return query;
        }
    }
}