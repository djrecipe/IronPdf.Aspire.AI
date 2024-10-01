using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronPdf.Aspire.AI.AppHost
{
    public class ChromaResource(string name) : ContainerResource(name), IResourceWithConnectionString
    {

        internal const string PrimaryEndpointName = "http";
        private EndpointReference? _primaryEndpoint;

        internal bool IsSslEnabled { get; set; } = false;

        public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);
        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create(
                $"{(IsSslEnabled ? "https" : "http")}://{PrimaryEndpoint.Property(EndpointProperty.Host)}:{PrimaryEndpoint.Property(EndpointProperty.Port)}"
            );

    }


    public static class ChromaResourceBuilderExtensions
    {
        public static IResourceBuilder<ChromaResource> AddChromaDb(
            this IDistributedApplicationBuilder builder,
            string name,
            int? port = null)
        {
            var resource = new ChromaResource(name);

            resource.IsSslEnabled = builder.ExecutionContext.IsPublishMode;

            return builder.AddResource(resource)
                .WithImage("chromadb/chroma")
                .WithImageTag("0.5.12.dev13")
                .WithEndpoint(port: port, name: IronEngineResource.PrimaryEndpointName, targetPort: 8000, scheme: "http");
        }
    }
}
