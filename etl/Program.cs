using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Json;

namespace etl
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var store = new DocumentStore { Urls = new string[] { "http://ravendb:8080" }, Database = "Digitalisert" })
            {
                store.Conventions.FindCollectionName = t => t.Name;
                store.Initialize();

                using (BulkInsertOperation bulkInsert = store.BulkInsert())
                {
                    JArray a = JArray.Parse(File.ReadAllText(@"export/resource.json"));

                    foreach(var node in a)
                    {
                        bulkInsert.Store(
                            node,
                            "Dataplattform/Drupal/Node/" + node["nid"][0]["value"],
                            new MetadataAsDictionary(new Dictionary<string, object> { { "@collection", "Dataplattform"}})
                        );
                    }
                }

                using (BulkInsertOperation bulkInsert = store.BulkInsert())
                {
                    JArray a = JArray.Parse(File.ReadAllText(@"export/property.json"));

                    foreach(var paragraph in a)
                    {
                        bulkInsert.Store(
                            paragraph,
                            "Dataplattform/Drupal/Paragraph/" + paragraph["id"][0]["value"],
                            new MetadataAsDictionary(new Dictionary<string, object> { { "@collection", "Dataplattform"}})
                        );
                    }
                }

                new DataplattformResourceModel.DataplattformResourceIndex().Execute(store);
            }
        }
    }
}
