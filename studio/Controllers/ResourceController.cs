using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Queries.Facets;
using Digitalisert.Dataplattform.Studio.Models;

namespace Digitalisert.Dataplattform.Studio.Controllers
{
    [Route("Studio/api/[controller]")]
    public class ResourceController : Controller
    {
        private readonly IDocumentStore _store;

        public ResourceController()
        {
            _store = DocumentStoreHolder.Store;
        }

        [HttpGet]
        public IEnumerable<object> Get([FromQuery] Models.ResourceModel.Resource[] resources)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);

                return query.ToQueryable().ProjectInto<ResourceModel.Resource>().Take(100).ToList();
            }
        }

        [HttpGet("{context}/{*id}")]
        public IEnumerable<object> Resource(string context, string id)
        {
            using(var session = _store.OpenSession())
            {
                var query = session
                    .Query<ResourceModel.Resource, ResourceModel.ResourceIndex>()
                    .Include<ResourceModel.Resource>(r => r.Properties.SelectMany(p => p.Resources).SelectMany(re => re.Source))
                    .Where(r => r.Context == context && r.ResourceId == id);

                foreach(var resource in query.ProjectInto<ResourceModel.Resource>())
                {
                    yield return resource;
                }
            }
        }

        [HttpGet("Facet")]
        public Dictionary<string, FacetResult> Facet([FromQuery] Models.ResourceModel.Resource[] resources)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);

                return query
                    .AggregateBy(ResourceModel.Facets)
                    .Execute();
            }
        }
    }
}
