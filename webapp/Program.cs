using System.IO;
using Markdig;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Register services
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

var app = builder.Build();

app.UseStaticFiles(); // Enables serving static files

string resumePath = builder.Configuration.GetValue<string>("ResumeFilePath") ?? "resume.md";

async Task<IResult> GetResumeHtml()
{
    if (!File.Exists(resumePath))
        return Results.NotFound("Resume file not found.");

    var markdown = await File.ReadAllTextAsync(resumePath, System.Text.Encoding.UTF8);
    markdown = markdown.Normalize(System.Text.NormalizationForm.FormC);
    var html = Markdown.ToHtml(markdown, new MarkdownPipelineBuilder().UseAdvancedExtensions().Build());
    return Results.Content($"<html><body>{html}</body></html>", "text/html");
}

// Serve Resume as HTML
app.MapGet("/", GetResumeHtml);
app.MapGet("/resume", GetResumeHtml);

// Download Resume as PDF
app.MapGet("/resume/pdf", async (HttpContext context) =>
{
    if (!File.Exists(resumePath))
        return Results.NotFound("Resume file not found.");

    var markdown = await File.ReadAllTextAsync(resumePath);
    var html = Markdown.ToHtml(markdown);

    using var stream = new MemoryStream();
    var document = new Document();
    var writer = PdfWriter.GetInstance(document, stream);
    document.Open();
    using (var sr = new StringReader(html))
    {
        XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, sr);
    }
    document.Close();
    writer.Close();

    return Results.File(stream.ToArray(), "application/pdf", "resume.pdf");
});

app.Run();
