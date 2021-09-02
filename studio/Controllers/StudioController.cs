using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Queries.Facets;
using Raven.Client.Documents.Queries.Highlighting;
using Digitalisert.Dataplattform.Studio.Models;

namespace Digitalisert.Dataplattform.Studio.Controllers
{
    public class StudioController : Controller
    {
        [ViewData]
        public IEnumerable<ResourceModel.Resource> ResourceQuery { get; set; }
        [ViewData]
        public IEnumerable<ResourceModel.Resource> ResourceCurrent { get; set; }
        [ViewData]
        public string ResourceSearch { get; set; }
        [ViewData]
        public Dictionary<string, string[]> ResourceSearchHighlightings { get; set; }
        [ViewData]
        public Dictionary<string, FacetResult> ResourceFacet { get; set; }
        private readonly IDocumentStore _store;

        public StudioController()
        {
            _store = DocumentStoreHolder.Store;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Resource([FromQuery] Models.ResourceModel.Resource[] resources = null, string search = null)
        {
            using(var session = _store.OpenSession())
            {
                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                query = ResourceModel.QueryByExample(query, resources);
                Highlightings highlightings = null;

                if (!String.IsNullOrWhiteSpace(search))
                {
                    query.Highlight("Search", 128, 1, new HighlightingOptions { PreTags = new[] { "`" }, PostTags = new[] { "`" } }, out highlightings);
                    query.Search("Search", search, @operator: SearchOperator.And);
                    ResourceSearch = search;
                }

                var result = query.ToQueryable().ProjectInto<ResourceModel.Resource>().Take(100).ToList();

                ResourceSearchHighlightings = (highlightings ?? new Highlightings("")).ResultIndents.ToDictionary(r => r, r => highlightings.GetFragments(r));
                ResourceFacet = query.AggregateBy(ResourceModel.Facets).Execute();
                ResourceQuery = resources ?? new Models.ResourceModel.Resource[] {};

                return View(result);
            }
        }

        [Route("Studio/Resource/{context}/{*id}")]
        public IActionResult Resource(string context, string id, [FromQuery] Models.ResourceModel.Resource[] resources = null)
        {
            using(var session = _store.OpenSession())
            {
                var resource = session
                    .Query<ResourceModel.Resource, ResourceModel.ResourceIndex>()
                    .Include<ResourceModel.Resource>(r => r.Properties.SelectMany(p => p.Resources).SelectMany(re => re.Source))
                    .Where(r => r.Context == context && r.ResourceId == id).ProjectInto<ResourceModel.Resource>().ToList();

                var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();
                Highlightings highlightings = null;

                query = ResourceModel.QueryByExample(query, resources);
                query.WhereEquals("@resources", context + "/" + id);
                var result = (resources.Any()) ? query.ToQueryable().ProjectInto<ResourceModel.Resource>().Take(100).ToList() : new List<ResourceModel.Resource>();

                ResourceSearchHighlightings = (highlightings ?? new Highlightings("")).ResultIndents.ToDictionary(r => r, r => highlightings.GetFragments(r));
                ResourceFacet = query.AggregateBy(ResourceModel.Facets).Execute();
                ResourceQuery = resources ?? new Models.ResourceModel.Resource[] {};

                return View((resources.Any()) ? result : resource);
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
