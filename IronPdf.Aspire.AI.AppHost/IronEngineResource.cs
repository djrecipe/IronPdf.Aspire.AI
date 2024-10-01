using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IronPdf.Aspire.AI.AppHost
{
    public class IronEngineResource(string name) : ContainerResource(name), IResourceWithConnectionString
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


    public static class IronEngineResourceBuilderExtensions
    {
        public static IResourceBuilder<IronEngineResource> AddIronEngine(
            this IDistributedApplicationBuilder builder,
            string name,
            int? port = null)
        {
            var resource = new IronEngineResource(name);

            resource.IsSslEnabled = builder.ExecutionContext.IsPublishMode;

            return builder.AddResource(resource)
                .WithImage("ironsoftwareofficial/ironpdfengine")
                .WithImageTag("2024.10.3")
                .WithEndpoint(port: port, name: IronEngineResource.PrimaryEndpointName, targetPort: 33350, scheme: "http");
        }

    }
}
