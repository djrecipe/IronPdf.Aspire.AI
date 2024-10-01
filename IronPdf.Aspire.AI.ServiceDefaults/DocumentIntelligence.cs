using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPdf.GrpcLayer;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Chroma;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Memory;
using IronPdf.AI;

namespace IronPdf.Aspire.AI.ServiceDefaults
{
    public class DocumentIntelligence(IConfiguration Configuration, NavigationManager NavigationManager, ILogger<PdfGenerator> Logger, PdfGenerator pdfgen)
    {
        internal const string ActivitySourceName = "DocumentIntelligence";

        internal static ActivitySource ActivitySource = new ActivitySource(ActivitySourceName, typeof(PdfGenerator).Assembly.GetName().Version.ToString());
        internal static Meter Meter = new Meter(ActivitySourceName, typeof(PdfGenerator).Assembly.GetName().Version.ToString());
        internal static Histogram<double> Counter = Meter.CreateHistogram<double>("pdfgenerator.generatepdf", unit: "Seconds", description: "PDFs Generated");

        public async Task<string> SummarizeUrl(string url)
        {
            using (var doc = await pdfgen.GeneratePdfForUrl(url))
            {
                var summary = await doc.Summarize(new int[]{0}); // optionally pass AI instance or use AI instance directly
                return summary;
            }
        }
    }
    public static class DocumentIntelligenceExtensions
    {

        public static IHostApplicationBuilder AddDocumentIntelligence(this IHostApplicationBuilder builder)
        {
            
// SETUP AI
            var azureEndpoint = "<<<your endpoint here>>";
            var apiKey = "<<<your key here>>>";
            var kernel_builder = Kernel.CreateBuilder()
                .AddAzureOpenAITextEmbeddingGeneration("oaiembed", azureEndpoint, apiKey)
                .AddAzureOpenAIChatCompletion("oaichat", azureEndpoint, apiKey);
            var kernel = kernel_builder.Build();

// SETUP MEMORY
            var dbUri = builder.Configuration.GetConnectionString("chromadb");
            var memory_builder = new MemoryBuilder()
                // optionally use new ChromaMemoryStore("http://127.0.0.1:8000") (see https://github.com/microsoft/semantic-kernel/blob/main/dotnet/notebooks/09-memory-with-chroma.ipynb)
                .WithMemoryStore(new ChromaMemoryStore(dbUri))
                .WithAzureOpenAITextEmbeddingGeneration("oaiembed", azureEndpoint, apiKey);
            var memory = memory_builder.Build();

// INITIALIZE IRONAI
            IronDocumentAI.Initialize(kernel, memory);

            // PDF Generator service
            builder.Services.AddTransient<DocumentIntelligence>();

            // OpenTelemetry for PDF Generator
            builder.Services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing.AddSource(DocumentIntelligence.ActivitySourceName);
                })
                .WithMetrics(metrics =>
                {
                    metrics.AddMeter(DocumentIntelligence.Meter.Name);
                });

            return builder;
        }

    }
}
