using System;
using System.Linq;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Digitalisert.Dataplattform
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var store = new DocumentStore { Urls = new string[] { "http://ravendb:8080" }, Database = "Digitalisert" })
            {
                store.Conventions.Serialization = new NewtonsoftJsonSerializationConventions { CustomizeJsonSerializer = s => s.NullValueHandling = NullValueHandling.Ignore };
                store.Conventions.FindCollectionName = t => t.Name;
                store.Initialize();

                if (!store.Maintenance.Server.Send(new GetDatabaseNamesOperation(0, 25)).Contains(store.Database))
                {
                    store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(store.Database)));

                    store.Maintenance.Send(new ConfigureExpirationOperation(new ExpirationConfiguration
                    {
                                Disabled = false,
                                DeleteFrequencyInSec = 60
                    }));
                }

                new ResourceMappingIndex().Execute(store);
                new ResourceOntologyIndex().Execute(store);
                new ResourcePropertyIndex().Execute(store);
                new ResourceClusterIndex().Execute(store);
                new ResourceDerivedPropertyIndex().Execute(store);
                new ResourceReasonerIndex().Execute(store);
                new ResourcePropertyAttributeIndex().Execute(store);
                new ResourceIndex().Execute(store);
            }
        }
    }
}
