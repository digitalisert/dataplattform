using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Digitalisert.Dataplattform.Studio.Models;

namespace Digitalisert.Dataplattform.Studio.Controllers
{
    [Route("Studio/api/[controller]")]
    public class SearchController : Controller
    {
        private readonly IDocumentStore _store;

        public SearchController()
        {
            _store = DocumentStoreHolder.Store;
        }

        [HttpGet("search/{*pathid}")]
        public dynamic Search(string pathid, string urlprefix = null, string defaulttitle = null)
        {
            using(var session = _store.OpenSession())
            {
                var contextq = (pathid).Split(new[] { '/', '_' }).FirstOrDefault();
                var resourceId = String.Join("/", (pathid).Split(new[] { '/', '_' }).Skip(1));
                var query = session
                    .Query<ResourceModel.Resource, ResourceModel.ResourceIndex>()
                    .Where(r => r.Context == contextq && r.ResourceId == resourceId)
                    .Select(r => new {
                        id = r.Context + "_" + r.ResourceId.Replace("/", "_"),
                        uuid = r.Context + "/" + r.ResourceId,
                        title = r.Code.Select(c => c + " ").FirstOrDefault() + r.Title.FirstOrDefault(),
                        url = (urlprefix ?? "") + r.Context + "/" + r.ResourceId
                    });

                return query.FirstOrDefault() ?? new { id = contextq + "_" + resourceId.Replace("/", "_"), uuid = "", title = defaulttitle ?? "", url = "" };
            }
        }

        [HttpGet("search")]
        public dynamic Search(string id, string title, string context, string type)
        {
            using(var session = _store.OpenSession())
            {
                if (!String.IsNullOrWhiteSpace(id))
                {
                    var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();
                    foreach(var qid in id.Split(new[] { ','}, StringSplitOptions.RemoveEmptyEntries).Select((v, i) => new { v, i} ))
                    {
                        if (qid.i > 0)
                            query = query.OrElse();

                        query = query.OpenSubclause();
                        query = query.WhereEquals("Context", (qid.v).Split(new[] { '/', '_' }).FirstOrDefault());
                        query = query.WhereEquals("ResourceId", String.Join("/", (qid.v).Split(new[] { '/', '_' }).Skip(1)));
                        query = query.CloseSubclause();
                    }

                    return query.ToQueryable()
                        .Select(r => new {
                            id = r.Context + "_" + r.ResourceId.Replace("/", "_"),
                            title = r.Title.FirstOrDefault()
                        }).ToList();
                }
                else {
                    var query = session.Advanced.DocumentQuery<ResourceModel.Resource, ResourceModel.ResourceIndex>();

                    if (!String.IsNullOrWhiteSpace(context))
                        query = query.WhereEquals("Context", context);

                    if (!String.IsNullOrWhiteSpace(type))
                        query = query.WhereEquals("Type", type);

                    if (!String.IsNullOrWhiteSpace(title))
                        query = query.WhereStartsWith("Title", title);

                    return query.ToQueryable()
                        .Select(r => new {
                            id = r.Context + "_" + r.ResourceId.Replace("/", "_"),
                            title = r.Title.FirstOrDefault()
                        }).ToList();
                }
            }
        }
    }
}
