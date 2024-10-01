using IronPdf.Aspire.AI.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var pdfEngine = builder.AddIronEngine("pdfengine");

var chromadb = builder.AddChromaDb("chromadb");

var apiService = builder.AddProject<Projects.IronPdf_Aspire_AI_ApiService>("apiservice");

builder.AddProject<Projects.IronPdf_Aspire_AI_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WithReference(pdfEngine)
    .WithReference(chromadb);

builder.Build().Run();
