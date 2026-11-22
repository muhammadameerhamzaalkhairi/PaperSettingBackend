using System.Globalization;
using System.Security.Cryptography;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using Row = System.Collections.Generic.Dictionary<string, object?>;

namespace BIITPaperSettingAPI.Services;

public class ApprovedPaperPdf(PrivateDiagrams diagrams)
{
    public async Task<byte[]> RenderAsync(Dictionary<string,List<Row>> content, CancellationToken cancellation)
    {
        // =====================================================
        // STEP 1: PRINT THE APPROVED EXAM HEADER
        // =====================================================
        var header = content["header"].Single();
        var document = new Document();
        var normal = document.Styles["Normal"]!;
        normal.Font.Name = "Arial";
        normal.Font.Size = 11;
        normal.ParagraphFormat.SpaceAfter = 4;
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(2.2);
        section.PageSetup.TopMargin = section.PageSetup.BottomMargin = Unit.FromCentimeter(2);
        var title = section.AddParagraph("BIIT");
        title.Format.Font.Size = 18;
        title.Format.Font.Bold = true;
        title.Format.Alignment = ParagraphAlignment.Center;
        var exam = section.AddParagraph($"{header["exam_type"]} Examination  |  {header["session_name"]}");
        exam.Format.Alignment = ParagraphAlignment.Center;
        var course = section.AddParagraph($"{header["course_code"]}  {header["course_title"]}");
        course.Format.Font.Bold = true;
        course.Format.SpaceBefore = 12;
        section.AddParagraph($"Duration  {header["minutes"]} minutes     Total marks  {Marks(header["marks"])}");
        if (header["instructions"] is string instructions && !string.IsNullOrWhiteSpace(instructions))
            section.AddParagraph(instructions);
        var footer = section.Footers.Primary.AddParagraph("Page ");
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.AddPageField();

        // =====================================================
        // STEP 2: PRINT MAIN QUESTIONS AND THEIR OWN DIAGRAMS
        // =====================================================
        int number = 0;
        foreach (var question in content["questions"])
        {
            cancellation.ThrowIfCancellationRequested();
            int id = (int)question["question_version_id"]!;
            var parts = content["parts"].Where(part => Equals(part["question_version_id"],id)).ToList();
            decimal total = parts.Sum(part => (decimal?)part["marks"] ?? 0);
            number++;
            var stem = section.AddParagraph($"Q{number}.  {question["prompt_text"]}  [{Marks(total)} marks]");
            stem.Format.SpaceBefore = 10;
            stem.Format.Font.Bold = true;
            await AddDiagrams(section, content["diagrams"], id, null, cancellation);
            foreach (var part in parts)
            {
                string label = (string)part["part_label"]!;
                string body = (string)part["part_text"]!;
                if (label != "MAIN" || !string.IsNullOrWhiteSpace(body))
                {
                    string line = body;
                    if (label != "MAIN") line = $"{label}  {body}";
                    if (part["marks"] is decimal marks) line += $"  [{Marks(marks)}]";
                    var paragraph = section.AddParagraph(line);
                    paragraph.Format.LeftIndent = Unit.FromCentimeter(part["parent_part_id"] == null ? 0.4 : 0.9);
                }
                await AddDiagrams(section, content["diagrams"], id, (int)part["question_part_id"]!, cancellation);
            }
        }
        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var pdf = renderer.PdfDocument;
        using var output = new MemoryStream();
        pdf.Save(output, false);
        return output.ToArray();
    }

    private static string Marks(object? value) => Convert.ToDecimal(value).ToString("0.##",CultureInfo.InvariantCulture);

    private async Task AddDiagrams(Section section, List<Row> files, int question, int? part, CancellationToken cancellation)
    {
        foreach (var file in files)
        {
            if (!Equals(file["question_version_id"],question) || !Equals(file["question_part_id"],part))
                continue;
            // Private file ka checksum approved attachment se milna zaroori hai.
            using var stream = diagrams.Open((string)file["storage_key"]!);
            if (stream == null) throw new InvalidDataException("Approved diagram is missing.");
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer,cancellation);
            byte[] bytes = buffer.ToArray();
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)),(string)file["checksum"]!,StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Approved diagram checksum changed.");
            buffer.Position = 0;
            using var source = XImage.FromStream(buffer);
            var image = section.AddImage("base64:" + Convert.ToBase64String(bytes));
            double scale = Math.Min(1, Math.Min(450.0/source.PointWidth, 380.0/source.PointHeight));
            image.Width = Unit.FromPoint(source.PointWidth * scale);
            image.Height = Unit.FromPoint(source.PointHeight * scale);
        }
    }
}
