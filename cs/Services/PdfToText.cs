using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Text;

public interface IPdfConverter
{
    Task ConvertAsync(Stream input,
                      Stream output,
                      CancellationToken cancellationToken);
}

public class PdfToTxtFileConverter : IPdfConverter
{
    private readonly ILogger<PdfToTxtFileConverter> _logger;

    public string[] HandledMimeTypes => ["application/pdf"];
    public string OutputMimeType => "text/plain";

    public PdfToTxtFileConverter(ILogger<PdfToTxtFileConverter> logger)
    {
        _logger = logger;
    }

    public async Task ConvertAsync(Stream input,
                                   Stream output,
                                   CancellationToken cancellationToken)
    {
        using (PdfDocument document = PdfDocument.Open(input))
        {
            foreach (Page page in document.GetPages())
            {
                await output.WriteAsync(Encoding.UTF8.GetBytes(page.Text), cancellationToken);
            }
        }
    }
}