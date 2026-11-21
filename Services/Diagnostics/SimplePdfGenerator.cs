using System.Globalization;
using System.Text;

namespace RAQEEB.Services.Diagnostics
{
    // Builds small, valid single-page PDF documents (text only, Helvetica).
    // Used to create demonstration reports for development seed data,
    // so no third-party PDF library is needed.
    public static class SimplePdfGenerator
    {
        private const int MaxLines = 48;

        public static byte[] Create(string title, IEnumerable<string> lines)
        {
            var content = new StringBuilder();

            content.Append("BT /F2 16 Tf 50 790 Td (")
                .Append(Escape(title))
                .Append(") Tj ET\n");

            content.Append("BT /F1 11 Tf 15 TL 50 760 Td\n");

            foreach (var line in lines.Take(MaxLines))
            {
                content.Append('(').Append(Escape(line)).Append(") Tj T*\n");
            }

            content.Append("ET\n");

            var contentBytes = Encoding.ASCII.GetBytes(content.ToString());

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                    "/Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>",
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>",
                $"<< /Length {contentBytes.Length} >>\nstream\n" +
                    Encoding.ASCII.GetString(contentBytes) + "endstream"
            };

            using var stream = new MemoryStream();
            var offsets = new List<long>();

            Write(stream, "%PDF-1.4\n");

            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(stream.Position);
                Write(stream, $"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xrefPosition = stream.Position;

            var xref = new StringBuilder();
            xref.Append("xref\n");
            xref.Append(CultureInfo.InvariantCulture, $"0 {objects.Count + 1}\n");
            xref.Append("0000000000 65535 f \n");

            foreach (var offset in offsets)
            {
                xref.Append(offset.ToString("D10", CultureInfo.InvariantCulture))
                    .Append(" 00000 n \n");
            }

            xref.Append(CultureInfo.InvariantCulture,
                $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");

            Write(stream, xref.ToString());

            return stream.ToArray();
        }

        private static void Write(Stream stream, string text)
        {
            var bytes = Encoding.ASCII.GetBytes(text);
            stream.Write(bytes, 0, bytes.Length);
        }

        // Keep printable ASCII only and escape PDF string delimiters.
        private static string Escape(string text)
        {
            var builder = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                if (c == '\\' || c == '(' || c == ')')
                {
                    builder.Append('\\').Append(c);
                }
                else if (c >= 32 && c <= 126)
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('?');
                }
            }

            return builder.ToString();
        }
    }
}
