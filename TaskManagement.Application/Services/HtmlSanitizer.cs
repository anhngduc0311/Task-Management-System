using System.Text.RegularExpressions;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services
{
    public class HtmlSanitizer : IHtmlSanitizer
    {
        public string Sanitize(string? html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // Remove script blocks
            var sanitized = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);

            // Remove dangerous tags (object, embed, applet, iframe, meta, link, style)
            sanitized = Regex.Replace(sanitized, @"</?(object|embed|applet|iframe|meta|link|style)[^>]*>", "", RegexOptions.IgnoreCase);

            // Remove inline event handlers (e.g. onload, onclick, onerror)
            sanitized = Regex.Replace(sanitized, @"\s(on\w+)\s*=\s*(['""])[^'""]*\2", "", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"\s(on\w+)\s*=\s*[^\s>]+", "", RegexOptions.IgnoreCase);

            // Remove javascript: and vbscript: URIs
            sanitized = Regex.Replace(sanitized, @"href\s*=\s*(['""])\s*(javascript|vbscript):[^'""]*\1", "href=\"#\"", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"src\s*=\s*(['""])\s*(javascript|vbscript):[^'""]*\1", "src=\"#\"", RegexOptions.IgnoreCase);

            return sanitized;
        }
    }
}
