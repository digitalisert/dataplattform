using System;
using System.Collections.Generic;

namespace Digitalisert.Dataplattform
{
    public class ResourceModel
    {
        public class Resource
        {
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
            public IEnumerable<Property> Properties { get; set; }
            public IEnumerable<string> Source { get; set; }
            public DateTime? Modified { get; set; }
            public IEnumerable<object> _ { get; set; }
        }

        public class ResourceReferences {
            public string[] ReduceOutputs { get; set; }
        }

        public class Property
        {
            public string Name { get; set; }
            public IEnumerable<string> Value { get; set; }
            public IEnumerable<string> Tags { get; set; }
            public IEnumerable<Resource> Resources { get; set; }
            public IEnumerable<Property> Properties { get; set; }
            public DateTime? From { get; set; }
            public DateTime? Thru { get; set; }
            public IEnumerable<string> Source { get; set; }
        }

        public class ResourceProperty : Resource {
            public string Name { get; set; }
        }

        public class ResourcePropertyReferences : ResourceReferences { }
        public class ResourceCluster : ResourceProperty { }
        public class ResourceClusterReferences : ResourcePropertyReferences { }
        public class ResourceMapping : Resource { }
        public class ResourceMappingReferences : ResourcePropertyReferences { }
        public class ResourceOntology : Resource { }
        public class ResourceOntologyReferences : ResourcePropertyReferences { }
        public class ResourceDerivedProperty : ResourceProperty { }
        public class ResourceDerivedPropertyReferences : ResourcePropertyReferences { }
        public class ResourceMapped : Resource { }
        public class DataplattformResource : ResourceMapped { }
        public class OntologyResource : ResourceMapped { }
        public class N50KartdataResource : ResourceMapped { }
        public class SSBResource : ResourceMapped { }
    }
}
