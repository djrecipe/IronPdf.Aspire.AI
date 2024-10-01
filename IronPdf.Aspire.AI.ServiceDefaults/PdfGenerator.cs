using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPdf.GrpcLayer;
using Microsoft.Extensions.DependencyInjection;

namespace IronPdf.Aspire.AI.ServiceDefaults
{

    public class PdfGenerator(IConfiguration Configuration, NavigationManager NavigationManager, ILogger<PdfGenerator> Logger)
    {
        internal const string ActivitySourceName = "PdfGenerator";

	internal static ActivitySource ActivitySource = new ActivitySource(ActivitySourceName, typeof(PdfGenerator).Assembly.GetName().Version.ToString());
	internal static Meter Meter = new Meter(ActivitySourceName, typeof(PdfGenerator).Assembly.GetName().Version.ToString());
	internal static Histogram<double> Counter = Meter.CreateHistogram<double>("pdfgenerator.generatepdf", unit: "Seconds", description: "PDFs Generated");

		
    public async Task<PdfDocument> GeneratePdfForUrl(string url)
    {

        var sw = Stopwatch.StartNew();
        Logger.LogInformation($"Beginning render for '{url}'");
        using var activity = ActivitySource.StartActivity(ActivityKind.Client, tags: new ActivityTagsCollection { { "url", url.ToString() } });
            
        IronPdf.License.LicenseKey = "<<<your key here>>>";

        ChromePdfRenderer renderer = new ChromePdfRenderer();

        renderer.RenderingOptions = new ChromePdfRenderOptions
        {
            EnableJavaScript = true,
            PrintHtmlBackgrounds = true,
        };

        renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;
        renderer.RenderingOptions.WaitFor.RenderDelay(3000);
        renderer.RenderingOptions.PaperFit.UseResponsiveCssRendering();

        PdfDocument? pdf = null;
        try
        {
            pdf = await renderer.RenderUrlAsPdfAsync(url);
        } catch (Exception ex)
        {
            Logger.LogError(ex, $"Error rendering '{url}'");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
		
        Logger.LogInformation($"Completed render for '{url}' in {sw.Elapsed}");
        if (Counter.Enabled) Counter.Record(sw.Elapsed.TotalSeconds, new Dictionary<string, object?> { {"url", url.ToString() } }.ToArray() );

        return pdf;

    }
    public async Task<Stream> GeneratePdfForUrl(Uri relativeUrlToRender, HttpContext context = null)
    {

        var sw = Stopwatch.StartNew();
        Logger.LogInformation($"Beginning render for '{relativeUrlToRender}'");
        using var activity = ActivitySource.StartActivity(ActivityKind.Client, tags: new ActivityTagsCollection { { "url", relativeUrlToRender.ToString() } });
            
        IronPdf.License.LicenseKey = "<<<your key here>>>";

#if DEBUG
        // hard coded as we're running the IronPDF engine in Docker
        var baseUrl = "https://host.docker.internal:8199";
#else
		var baseUrl = NavigationManager.BaseUri;
#endif

        ChromePdfRenderer renderer = new ChromePdfRenderer();

        renderer.RenderingOptions = new ChromePdfRenderOptions
        {
            EnableJavaScript = true,
            PrintHtmlBackgrounds = true,
        };

        if (context != null)
        {
            renderer.RenderingOptions.CustomCookies = context.Request.Cookies.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;
        renderer.RenderingOptions.WaitFor.RenderDelay(3000);
        renderer.RenderingOptions.PaperFit.UseResponsiveCssRendering();

        PdfDocument? pdf = null;
        try
        {
            pdf = await renderer.RenderUrlAsPdfAsync(new Uri(new Uri(baseUrl), relativeUrlToRender));
        } catch (Exception ex)
        {
            Logger.LogError(ex, $"Error rendering '{relativeUrlToRender}'");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
		
        Logger.LogInformation($"Completed render for '{relativeUrlToRender}' in {sw.Elapsed}");
        if (Counter.Enabled) Counter.Record(sw.Elapsed.TotalSeconds, new Dictionary<string, object?> { {"url", relativeUrlToRender.ToString() } }.ToArray() );

        return pdf.Stream;

    }
    public async Task<Stream> GeneratePdfFromHtml(string html)
    {

        var sw = Stopwatch.StartNew();
        Logger.LogInformation($"Beginning render for '{html}'");
        using var activity = ActivitySource.StartActivity(ActivityKind.Client, tags: new ActivityTagsCollection { { "html", html.ToString() } });

        IronPdf.License.LicenseKey = "<<<your key here>>>";

#if DEBUG
        // hard coded as we're running the IronPDF engine in Docker
        var baseUrl = "https://host.docker.internal:8199";
#else
		var baseUrl = NavigationManager.BaseUri;
#endif

        ChromePdfRenderer renderer = new ChromePdfRenderer();

        renderer.RenderingOptions = new ChromePdfRenderOptions
        {
            EnableJavaScript = true,
            PrintHtmlBackgrounds = true,
        };

        renderer.RenderingOptions.CssMediaType = IronPdf.Rendering.PdfCssMediaType.Print;
        renderer.RenderingOptions.WaitFor.RenderDelay(3000);
        renderer.RenderingOptions.PaperFit.UseResponsiveCssRendering();

        PdfDocument? pdf = null;
        try
        {
            pdf = await renderer.RenderHtmlAsPdfAsync(html);
        } catch (Exception ex)
        {
            Logger.LogError(ex, $"Error rendering '{html}'");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
		
        Logger.LogInformation($"Completed render for '{html}' in {sw.Elapsed}");
        if (Counter.Enabled) Counter.Record(sw.Elapsed.TotalSeconds, new Dictionary<string, object?> { {"html", html.ToString() } }.ToArray() );

        return pdf.Stream;

    }

}

public static class PdfGeneratorExtensions
{

	public static IHostApplicationBuilder AddPdfGenerator(this IHostApplicationBuilder builder)
	{

		// Connection information for the IronPDF engine
		var ironUri = builder.Configuration.GetConnectionString("pdfengine");
		var ironConfig = IronPdfConnectionConfiguration.RemoteServer(ironUri);
		IronPdf.Installation.ConnectToIronPdfHost(ironConfig);

		// PDF Generator service
		builder.Services.AddTransient<PdfGenerator>();

		// OpenTelemetry for PDF Generator
		builder.Services.AddOpenTelemetry()
			.WithTracing(tracing =>
			{
				tracing.AddSource(PdfGenerator.ActivitySourceName);
			})
			.WithMetrics(metrics =>
			{
				metrics.AddMeter(PdfGenerator.Meter.Name);
			});

		return builder;
	}

}
}
