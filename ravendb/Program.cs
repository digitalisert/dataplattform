using System;
using System.Diagnostics;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Json.Serialization.NewtonsoftJson;

namespace Digitalisert.Dataplattform
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var store = new DocumentStore { Urls = new string[] { "http://localhost:8080" }, Database = "Digitalisert" })
            {
                store.Conventions.Serialization = new NewtonsoftJsonSerializationConventions { CustomizeJsonSerializer = s => s.NullValueHandling = NullValueHandling.Ignore };
                store.Conventions.FindCollectionName = t => t.Name;
                store.Initialize();

                var stopwatch = Stopwatch.StartNew();

                new ResourceMappingIndex().Execute(store);
                new ResourceOntologyIndex().Execute(store);
                new ResourcePropertyIndex().Execute(store);
                new ResourceClusterIndex().Execute(store);
                new ResourceDerivedPropertyIndex().Execute(store);
                new ResourceReasonerIndex().Execute(store);
                new ResourceIndex().Execute(store);

                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed);
            }
        }
    }
}
