using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;

var docsDir = args.Length > 0 ? args[0] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs"));
var outputDir = args.Length > 1 ? args[1] : Path.Combine(docsDir, "..", "_site");

Console.WriteLine($"Docs: {docsDir}");
Console.WriteLine($"Output: {outputDir}");

var pipeline = new MarkdownPipelineBuilder()
    .UseYamlFrontMatter().UseAutoLinks().UseTaskLists().UsePipeTables().Build();

var docs = new List<DocEntry>();
var order = new[] { "01-quick-refs", "02-tutorials/basics", "02-tutorials/intermediate", "02-tutorials/advanced", "03-patterns", "04-migration" };
var labels = new Dictionary<string, string>
{
    ["01-quick-refs"] = "Quick References", ["02-tutorials/basics"] = "Basics",
    ["02-tutorials/intermediate"] = "Intermediate", ["02-tutorials/advanced"] = "Advanced",
    ["03-patterns"] = "Patterns", ["04-migration"] = "Migration",
};
var exclude = new HashSet<string> { "_archive", "_assets", "_skills", "link-audit.ps1", "serve-docs.ps1", "viewer.html", "potential_todo.md" };

var files = Directory.EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories)
    .Where(f => !exclude.Any(e => f.Contains(e))).OrderBy(f => f).ToList();
Console.WriteLine($"Found {files.Count} files");

foreach (var f in files)
{
    var rel = Path.GetRelativePath(docsDir, f).Replace('\\', '/');
    var text = File.ReadAllText(f);
    var (fm, body) = ExtractFrontMatter(text);
    var title = ExtractTitle(fm, body, rel);
    var tier = order.FirstOrDefault(t => rel.StartsWith(t)) ?? "other";
    var html = Markdown.ToHtml(body, pipeline);
    html = Regex.Replace(html, @"href=""([^"":#]+)\.md(#?[^""]*)""", m => $"href=\"{m.Groups[1]}.html{m.Groups[2]}\"");
    var plain = Regex.Replace(html, "<[^>]+>", " ");
    if (plain.Length > 8000) plain = plain[..8000];
    var heads = Regex.Matches(body, @"^#{2,3}\s+(.+)", RegexOptions.Multiline).Select(m => m.Groups[1].Value.Trim()).ToArray();
    docs.Add(new DocEntry(rel, title, labels.GetValueOrDefault(tier, "Other"), html, plain, heads));
}

var nav = BuildNav(docs, order, labels);
var search = docs.Select(d => new SearchEntry(
    Path.ChangeExtension(d.Path, ".html"), d.Title, d.Tier, d.PlainText, d.Headings)).ToList();

var jOpts = new JsonSerializerOptions { WriteIndented = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
var navJson = JsonSerializer.Serialize(nav, jOpts);
var searchJson = JsonSerializer.Serialize(search, jOpts);

if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
Directory.CreateDirectory(outputDir);
CopyAssets(docsDir, outputDir);

foreach (var doc in docs)
{
    var outPath = Path.Combine(outputDir, Path.ChangeExtension(doc.Path, ".html"));
    Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
    File.WriteAllText(outPath, RenderPage(doc, navJson, searchJson));
}

var idx = docs.FirstOrDefault(d => d.Path == "00-index.md");
if (idx != null) File.WriteAllText(Path.Combine(outputDir, "index.html"), RenderPage(idx, navJson, searchJson));

Console.WriteLine($"Done ({Directory.GetFiles(outputDir, "*.html", SearchOption.AllDirectories).Length} pages)");

static (string, string) ExtractFrontMatter(string c)
{
    if (c.StartsWith("---")) { var e = c.IndexOf("---", 3, StringComparison.Ordinal); if (e > 0) return (c[3..e].Trim(), c[(e+3)..].TrimStart()); }
    return ("", c);
}
static string ExtractTitle(string fm, string body, string path)
{
    var m = Regex.Match(fm, @"topic:\s*(.+)", RegexOptions.Multiline);
    if (m.Success) return m.Groups[1].Value.Trim();
    m = Regex.Match(body, @"^#\s+(.+)", RegexOptions.Multiline);
    if (m.Success) return m.Groups[1].Value.Trim();
    var n = Path.GetFileNameWithoutExtension(path);
    n = Regex.Replace(n, @"^0*\d+-", ""); n = Regex.Replace(n, "[-_]", " ");
    return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(n);
}
static List<NavEntry> BuildNav(List<DocEntry> d, string[] order, Dictionary<string,string> labels)
{
    var r = new List<NavEntry>();
    foreach (var t in order)
    {
        var td = d.Where(x => x.Tier == labels.GetValueOrDefault(t, t)).OrderBy(x => x.Path).ToList();
        if (td.Count == 0) continue;
        r.Add(new NavEntry(labels.GetValueOrDefault(t, t), null, true));
        foreach (var x in td) r.Add(new NavEntry(x.Title, Path.ChangeExtension(x.Path, ".html"), false));
    }
    return r;
}
static string RenderPage(DocEntry doc, string navJson, string searchJson)
{
    var depth = doc.Path.Count(c => c == '/');
    var root = depth == 0 ? "." : string.Join("/", Enumerable.Repeat("..", depth));
    var current = Path.ChangeExtension(doc.Path, ".html");
    var nav = JsonSerializer.Deserialize<List<NavEntry>>(navJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

    var sb = new StringBuilder();
    sb.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1.0\">");
    sb.Append("<title>").Append(Escape(doc.Title)).Append(" — Avalonia Docs</title>");
    sb.Append("<link rel=\"stylesheet\" href=\"").Append(root).Append("/_assets/css/doc-theme.css\">");
    sb.Append("<link rel=\"stylesheet\" href=\"").Append(root).Append("/_assets/css/ssg.css\">");
    sb.Append("<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/catppuccin-mocha.min.css\">");
    sb.Append("</head><body><div class=\"layout\"><nav class=\"sidebar\" id=\"sidebar\">");
    sb.Append("<div class=\"sidebar-header\"><h2><a href=\"").Append(root).Append("/index.html\" style=\"color:inherit;text-decoration:none\">Avalonia Docs</a></h2></div>");
    sb.Append("<div class=\"search-box\"><input type=\"text\" id=\"searchInput\" placeholder=\"Search docs...\" autocomplete=\"off\"></div>");
    sb.Append("<div id=\"searchResults\" class=\"search-results\" style=\"display:none\"></div><ul class=\"nav-tree\" id=\"navTree\">");

    foreach (var e in nav)
    {
        if (e.IsGroup) sb.Append("<li class=\"nav-group\"><span class=\"nav-group-label\">").Append(Escape(e.Label)).Append("</span><ul class=\"nav-items\">");
        else {
            var active = e.Path == current;
            sb.Append("<li class=\"").Append(active ? "nav-item active" : "nav-item").Append("\"><a href=\"").Append(root).Append("/").Append(Escape(e.Path!)).Append("\">").Append(Escape(e.Label)).Append("</a></li>");
        }
    }
    for (int i = nav.Count - 1; i >= 0; i--) if (nav[i].IsGroup) sb.Append("</ul></li>");
    sb.Append("</ul></nav><main class=\"doc-content\" id=\"docContent\">");
    sb.Append(doc.HtmlContent);
    sb.Append("</main><nav class=\"doc-toc\" id=\"toc\"></nav></div>");
    sb.Append("<script src=\"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/highlight.min.js\"></script>");
    sb.Append("<script>const SEARCH_INDEX=").Append(searchJson).Append(";const NAV_INDEX=").Append(navJson).Append(";const CURRENT_PAGE=\"").Append(root).Append("/").Append(EscapeJs(current)).Append("\";</script>");
    sb.Append("<script src=\"").Append(root).Append("/_assets/js/ssg.js\"></script></body></html>");
    return sb.ToString();
}
static void CopyAssets(string srcDir, string dstDir)
{
    foreach (var dir in new[] { "_assets/css", "_assets/js", "_assets/screenshots", "_assets/img" })
    {
        var src = Path.Combine(srcDir, dir);
        if (!Directory.Exists(src)) continue;
        var dst = Path.Combine(dstDir, dir); Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src)) File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
    }
}
static string Escape(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
static string EscapeJs(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");

record DocEntry(string Path, string Title, string Tier, string HtmlContent, string PlainText, string[] Headings);
record SearchEntry(string Path, string Title, string Tier, string Text, string[] Headings);
record NavEntry(string Label, string? Path, bool IsGroup);