using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
                    FileInfo resource = new FileInfo(@"export/resource.json");
                    if (resource.Exists && resource.Length > 0)
                    {
                        JArray a = JArray.Parse(File.ReadAllText(resource.FullName));

                        foreach(var node in a)
                        {
                            bulkInsert.Store(
                                node,
                                "Dataplattform/Drupal/Node/" + node["nid"][0]["value"],
                                new MetadataAsDictionary(new Dictionary<string, object> {
                                    { "@collection", "Dataplattform" },
                                    { "@expires", DateTime.UtcNow.AddSeconds(60) }
                                })
                            );
                        }
                    }
                }

                using (BulkInsertOperation bulkInsert = store.BulkInsert())
                {
                    FileInfo property = new FileInfo(@"export/property.json");
                    if (property.Exists && property.Length > 0)
                    {
                        JArray a = JArray.Parse(File.ReadAllText(@"export/property.json"));

                        foreach(var paragraph in a)
                        {
                            bulkInsert.Store(
                                paragraph,
                                "Dataplattform/Drupal/Paragraph/" + paragraph["id"][0]["value"],
                                new MetadataAsDictionary(new Dictionary<string, object> {
                                    { "@collection", "Dataplattform" },
                                    { "@expires", DateTime.UtcNow.AddSeconds(60) }
                                })
                            );
                        }
                    }
                }

                using (BulkInsertOperation bulkInsert = store.BulkInsert())
                {
                    foreach(var fileInfo in new DirectoryInfo("export").GetFiles("webform-*.zip"))
                    {
                        using (ZipArchive archive = ZipFile.OpenRead(fileInfo.FullName))
                        {
                            foreach (ZipArchiveEntry entry in archive.Entries)
                            {
                                using (StreamReader streamreader = new StreamReader(entry.Open()))
                                {
                                    JObject webform = JObject.Parse(streamreader.ReadToEnd());

                                    bulkInsert.Store(
                                        webform,
                                        "Dataplattform/Drupal/Webform/" + webform["serial"],
                                        new MetadataAsDictionary(new Dictionary<string, object> {
                                            { "@collection", "Dataplattform" },
                                            { "@expires", DateTime.UtcNow.AddSeconds(60) }
                                        })
                                    );

                                }
                            }
                        }
                    }
                }

                new DataplattformResourceModel.DataplattformResourceIndex().Execute(store);
            }
        }
    }
}
