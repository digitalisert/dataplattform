using System;
using Raven.Client.Documents;

namespace Digitalisert.Dataplattform.Studio
{
	public class DocumentStoreHolder
	{
		private static Lazy<IDocumentStore> store = new Lazy<IDocumentStore>(CreateStore);

		public static IDocumentStore Store => store.Value;

		private static IDocumentStore CreateStore()
		{
			IDocumentStore store = new DocumentStore()
			{
				Urls = new[] { "http://ravendb:8080" },
				Database = "Digitalisert",
			};

			return store.Initialize();
		}
	}
}