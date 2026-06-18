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
var groupLabels = new Dictionary<string, string>
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
    var navLabel = BuildNavLabel(rel);
    var tier = order.FirstOrDefault(t => rel.StartsWith(t)) ?? "other";
    var html = Markdown.ToHtml(body, pipeline);
    html = Regex.Replace(html, @"href=""([^"":#]+)\.md(#?[^""]*)""", m => $"href=\"{m.Groups[1]}.html{m.Groups[2]}\"");
    html = ConvertQuizBlocks(html);
    var plain = Regex.Replace(html, "<[^>]+>", " ");
    if (plain.Length > 8000) plain = plain[..8000];
    var heads = Regex.Matches(body, @"^#{2,3}\s+(.+)", RegexOptions.Multiline).Select(m => m.Groups[1].Value.Trim()).ToArray();
    docs.Add(new DocEntry(rel, title, navLabel, groupLabels.GetValueOrDefault(tier, "Other"), html, plain, heads));
}

var nav = BuildNav(docs, order, groupLabels);
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

static string BuildNavLabel(string rel)
{
    var name = Path.GetFileNameWithoutExtension(rel);
    name = Regex.Replace(name, @"^0*\d+-", "");
    string variant = "";
    foreach (var kv in new Dictionary<string, string> { ["verbose"] = " (in depth)", ["examples"] = " (examples)", ["quiz"] = " (quiz)" })
    {
        if (name.EndsWith("-" + kv.Key)) { variant = kv.Value; name = name[..^(kv.Key.Length + 1)]; break; }
    }
    name = Regex.Replace(name, "[-_]", " ");
    name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
    return name + variant;
}

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

static string ConvertQuizBlocks(string html)
{
    return Regex.Replace(html,
        @"<pre><code\s+class=""language-quiz"">(.*?)</code></pre>",
        m =>
        {
            var raw = m.Groups[1].Value;
            raw = raw.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&#39;", "'");
            var lines = raw.Split('\n');
            if (lines.Length == 0) return m.Value;
            var sb = new StringBuilder();
            sb.Append("<div class=\"quiz-container\">");
            var qDone = false;
            var idx = 0;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                if (!qDone)
                {
                    var q = trimmed.StartsWith("Q:") ? trimmed[2..].Trim() : trimmed;
                    sb.Append("<div class=\"quiz-question\">").Append(Escape(q)).Append("</div>");
                    qDone = true;
                    continue;
                }
                if (trimmed.StartsWith("Explanation:", StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append("<div class=\"quiz-option\" style=\"cursor:default;border-color:var(--border);background:none\">");
                    sb.Append("<span class=\"option-text\" style=\"color:var(--text-dim);font-size:0.8125rem\">");
                    sb.Append("💡 ").Append(Escape(trimmed["Explanation:".Length..].Trim()));
                    sb.Append("</span></div>");
                    continue;
                }
                var optMatch = Regex.Match(trimmed, @"^([A-Z])\.\s*(.+)$");
                if (!optMatch.Success) continue;
                var label = optMatch.Groups[1].Value;
                var rest = optMatch.Groups[2].Value;
                var isCorrect = rest.Contains("(correct)");
                rest = rest.Replace(" (correct)", "").Replace("(correct)", "");
                var parts = rest.Split(" || ", 2);
                var text = parts[0].Trim();
                var explanation = parts.Length > 1 ? parts[1].Trim() : "";
                sb.Append("<div class=\"quiz-option").Append(isCorrect ? " correct" : "").Append("\">");
                sb.Append("<span class=\"option-label\">").Append(label).Append("</span>");
                sb.Append("<span class=\"option-text\">").Append(Escape(text)).Append("</span>");
                if (!string.IsNullOrEmpty(explanation))
                    sb.Append("<div class=\"option-explanation\">").Append(Escape(explanation)).Append("</div>");
                sb.Append("</div>");
                idx++;
            }
            sb.Append("<div class=\"quiz-feedback\"></div>");
            sb.Append("</div>");
            return sb.ToString();
        }, RegexOptions.Singleline);
}

static List<NavEntry> BuildNav(List<DocEntry> d, string[] order, Dictionary<string, string> groupLabels)
{
    var r = new List<NavEntry>();
    foreach (var t in order)
    {
        var td = d.Where(x => x.Tier == groupLabels.GetValueOrDefault(t, t)).ToList();
        if (td.Count == 0) continue;
        td.Sort((a, b) =>
        {
            var baseA = Path.GetFileNameWithoutExtension(a.Path);
            var baseB = Path.GetFileNameWithoutExtension(b.Path);
            baseA = Regex.Replace(baseA, @"-(verbose|examples|quiz)$", "");
            baseB = Regex.Replace(baseB, @"-(verbose|examples|quiz)$", "");
            var cmp = string.Compare(baseA, baseB, StringComparison.Ordinal);
            if (cmp != 0) return cmp;
            var va = GetVariantOrder(a.Path);
            var vb = GetVariantOrder(b.Path);
            return va.CompareTo(vb);
        });
        r.Add(new NavEntry(groupLabels.GetValueOrDefault(t, t), null, true));
        foreach (var x in td) r.Add(new NavEntry(x.NavLabel, Path.ChangeExtension(x.Path, ".html"), false));
    }
    return r;
}

static int GetVariantOrder(string path)
{
    var name = Path.GetFileNameWithoutExtension(path);
    if (name.EndsWith("-verbose")) return 1;
    if (name.EndsWith("-examples")) return 2;
    if (name.EndsWith("-quiz")) return 3;
    return 0;
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
    sb.Append("<link rel=\"stylesheet\" href=\"").Append(root).Append("/_assets/css/ssg.css\">");
    sb.Append("<link rel=\"stylesheet\" href=\"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/styles/github-dark.min.css\">");
    sb.Append("<link rel=\"icon\" href=\"data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'><text y='12' font-size='12'>A</text></svg>\">");
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
    sb.Append("<script src=\"https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.11.1/languages/powershell.min.js\"></script>");
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

record DocEntry(string Path, string Title, string NavLabel, string Tier, string HtmlContent, string PlainText, string[] Headings);
record SearchEntry(string Path, string Title, string Tier, string Text, string[] Headings);
record NavEntry(string Label, string? Path, bool IsGroup);
