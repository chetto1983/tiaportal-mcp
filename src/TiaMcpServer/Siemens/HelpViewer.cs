using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TiaMcpServer.Siemens
{
    // Client of the TIA Help Viewer service, the local F1 help of TIA Portal.
    //
    // Callers: the help tools in McpServer.Help.cs. Affected API: the service's 'HelpViewer'
    // controller. Siemens does not document it; the contract below is the one the viewer's own web
    // client uses (main-*.js in 'C:\Program Files\Siemens\Automation\TIA Help Viewer Service'),
    // measured on V21 on 2026-10-26:
    // - the service speaks HTTP/2 only; the .NET Framework stack fails at the TLS handshake, so the
    //   requests go through WinHttpHandler, which supports HTTP/2 on .NET Framework;
    // - 'SearchV2' needs a non-empty device ('' makes its Lucene query fail); the viewer's "no
    //   filter" value is the text 'Display all' (assets/i18n/en-us.json). Its documentation scope
    //   filter returned no results for every value tried, so only 'Display all' is sent;
    // - 'Html' takes the topic as a JSON 'HelpReference', whose package is the topic id up to the
    //   first '/'; 'Toc' is left out on purpose (348 MB).
    // Reads no project data and writes nothing.
    public static class HelpViewer
    {
        private const string AllFilter = "Display all";

        // The help is installed in en-US on this kind of system; the viewer passes the culture as is.
        private const string Culture = "en-US";

        /// <summary>Base URL from '--help-url'; null uses the local service of the TIA major version.</summary>
        public static string? BaseUrl { get; set; }

        public static string EffectiveBaseUrl =>
            (BaseUrl ?? $"https://localhost:5112/PortalV{Engineering.TiaMajorVersion}").TrimEnd('/');

        private static readonly Lazy<HttpClient> Client = new Lazy<HttpClient>(() => new HttpClient(new WinHttpHandler
        {
            // The local service uses a self-signed certificate; a remote '--help-url' must present a valid one.
            ServerCertificateValidationCallback = (request, certificate, chain, errors) =>
                errors == SslPolicyErrors.None || request.RequestUri.IsLoopback
        })
        {
            Timeout = TimeSpan.FromSeconds(60)
        });

        public sealed class SearchResult
        {
            public string Title { get; set; } = string.Empty;
            public string TopicId { get; set; } = string.Empty;

            /// <summary>The manual the topic belongs to, e.g. "STEP7: High-speed counters".</summary>
            public string Source { get; set; } = string.Empty;
        }

        public sealed class Link
        {
            public string Title { get; set; } = string.Empty;
            public string TopicId { get; set; } = string.Empty;
        }

        public sealed class Topic
        {
            public string TopicId { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Manual { get; set; } = string.Empty;
            public string Device { get; set; } = string.Empty;
            public string ParentTopicId { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public List<Link> Links { get; } = new List<Link>();
        }

        public sealed class DeviceFilter
        {
            public string Product { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Device { get; set; } = string.Empty;
        }

        public static List<SearchResult> Search(string searchText, string device)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new PortalException(PortalErrorCode.InvalidParams, "searchText must not be empty.");
            }

            var filter = string.IsNullOrWhiteSpace(device) ? AllFilter : device.Trim();
            using var document = GetJson($"SearchV2?culture={Culture}&&searchText={Uri.EscapeDataString(searchText)}" +
                                         $"&&device='{Uri.EscapeDataString(filter)}'&&scope='{Uri.EscapeDataString(AllFilter)}'");

            return document.RootElement.GetProperty("SearchResults").EnumerateArray()
                .Select(r => new SearchResult
                {
                    Title = Text(r, "Title"),
                    TopicId = Text(r, "TopicId"),
                    Source = Text(r, "HintText")
                })
                .ToList();
        }

        public static Topic GetTopic(string topicId)
        {
            var slash = string.IsNullOrWhiteSpace(topicId) ? -1 : topicId.IndexOf('/');
            if (slash <= 0 || slash == topicId.Length - 1)
            {
                throw new PortalException(PortalErrorCode.InvalidParams,
                    $"'{topicId}' is not a topic id. Use a 'topicId' from 'SearchHelp', e.g. 'ProgCPU12002MenUS/11627607179/12970745995.htm'.");
            }

            var reference = JsonSerializer.Serialize(new Dictionary<string, object?>
            {
                ["ItemId"] = topicId,
                ["PackageName"] = topicId.Substring(0, slash),
                ["InternalFileName"] = topicId.Substring(slash + 1),
                ["IsPackagedFileReference"] = true,
                ["IsTextFileReference"] = true,
                ["IsHtmlFileReference"] = true
            });

            using var document = GetJson($"Html?webApiParamsString={Uri.EscapeDataString(WebApiParams())}" +
                                         $"&&helpReferenceString={Uri.EscapeDataString(reference)}");
            var html = document.RootElement.GetString() ?? string.Empty;

            var topic = new Topic
            {
                TopicId = topicId,
                Title = WebUtility.HtmlDecode(Capture(html, @"<title>(.*?)</title>")),
                Manual = Meta(html, "HelpPackageTitleWithClient"),
                Device = Meta(html, "HelpScopeText"),
                ParentTopicId = Meta(html, "TocParentId")
            };

            // The page content is in 'nstext'; the banner above it repeats the title.
            var start = html.IndexOf("id=\"nstext\"", StringComparison.Ordinal);
            var body = start >= 0 ? html.Substring(start) : html;

            foreach (Match link in Regex.Matches(body,
                         @"<a\b[^>]*href=""([^""#]+\.htm)(?:#[^""]*)?""[^>]*>(.*?)</a>", RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                if (!link.Groups[1].Value.Contains("://"))
                {
                    topic.Links.Add(new Link
                    {
                        Title = ToText(link.Groups[2].Value),
                        TopicId = new Uri(new Uri("tia:/" + topicId), link.Groups[1].Value).AbsolutePath.TrimStart('/')
                    });
                }
            }

            topic.Text = ToText(body.Substring(body.IndexOf('>') + 1));
            return topic;
        }

        public static List<DeviceFilter> GetDeviceFilters()
        {
            using var document = GetJson($"DeviceFilters?webApiParamsString={Uri.EscapeDataString(WebApiParams())}");

            return document.RootElement.EnumerateArray()
                .SelectMany(product => product.GetProperty("Devices").EnumerateArray()
                    .Select(d => new DeviceFilter
                    {
                        Product = Text(product, "ClientName"),
                        Name = Text(d, "Name"),
                        Device = Text(d, "ScopeName")
                    }))
                .ToList();
        }

        private static string WebApiParams() =>
            JsonSerializer.Serialize(new Dictionary<string, string> { ["Culture"] = Culture, ["RequestUrl"] = string.Empty });

        private static JsonDocument GetJson(string relativeUrl)
        {
            var url = $"{EffectiveBaseUrl}/HelpViewer/{relativeUrl}";
            HttpResponseMessage response;
            string body;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url) { Version = new Version(2, 0) };
                response = Client.Value.SendAsync(request).GetAwaiter().GetResult();
                body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException)
            {
                throw new PortalException(PortalErrorCode.InvalidState,
                    $"The TIA Help Viewer at '{EffectiveBaseUrl}' did not answer: {Innermost(ex).Message} " +
                    "Check that the 'TIA Help Viewer Service' is running, or pass '--help-url'.", null, ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                // The service answers errors as JSON with 'ExceptionMessage', e.g. "Trying to create an
                // second application WorkingContext within the same thread." after rapid calls.
                var reason = TryRead(body, "ExceptionMessage") ?? TryRead(body, "Message") ?? body;
                throw new PortalException(PortalErrorCode.InvalidState,
                    $"The TIA Help Viewer answered {(int)response.StatusCode}: {reason}");
            }

            return JsonDocument.Parse(body);
        }

        private static string? TryRead(string json, string property)
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return document.RootElement.ValueKind == JsonValueKind.Object &&
                       document.RootElement.TryGetProperty(property, out var value)
                    ? value.GetString()
                    : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static Exception Innermost(Exception ex) => ex.InnerException == null ? ex : Innermost(ex.InnerException);

        private static string Text(JsonElement element, string property) =>
            element.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;

        private static string Capture(string html, string pattern) =>
            Regex.Match(html, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups[1].Value.Trim();

        private static string Meta(string html, string name) =>
            WebUtility.HtmlDecode(Capture(html, $@"<meta name=""Siemens\.TIAHelp\.{name}"" content=""([^""]*)"""));

        /// <summary>Plain text of an XHTML fragment: blocks and list items on their own lines, table cells separated by '|'.</summary>
        private static string ToText(string html)
        {
            var text = Regex.Replace(html, @"<(script|style)\b.*?</\1>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            // The source is indented XHTML: its line breaks carry no meaning, the tags do.
            text = Regex.Replace(text, @"\s+", " ");
            // One line per table row; line breaks inside a cell become ' / ' so they do not split the row.
            text = Regex.Replace(text, @"<tr\b[^>]*>(.*?)</tr>", row =>
                "\n| " + string.Join(" | ", Regex.Matches(row.Groups[1].Value, @"<t[dh]\b[^>]*>(.*?)</t[dh]>", RegexOptions.Singleline | RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Select(cell => ToText(cell.Groups[1].Value).Replace("\n- ", " / ").Replace("\n", " / ").TrimStart('-', ' ', '/'))) + " |",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<li\b[^>]*>", "\n- ", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<(br|/p|/div|/h[1-6]|/table|/ul|/ol|/dt|/dd)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"<[^>]+>", string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"[ \t]*\n[ \t]*", "\n");
            text = Regex.Replace(text, @"[ \t]{2,}", " ");
            text = Regex.Replace(text, @"\n{3,}", "\n\n");
            return text.Trim();
        }
    }
}
