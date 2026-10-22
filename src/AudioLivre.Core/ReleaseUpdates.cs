using System.Net.Http;
using System.Text.Json;

namespace AudioLivre;
public record ReleaseUpdate(Version Version, Uri Page);
public static class ReleaseUpdates {
    public static ReleaseUpdate Parse(string json, Version current) {
        using var document=JsonDocument.Parse(json);
        var root=document.RootElement;
        if(root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean()) return null;
        var tag=root.GetProperty("tag_name").GetString()?.TrimStart('v','V');
        if(!Version.TryParse(tag,out var version) || version<=current) return null;
        if(!Uri.TryCreate(root.GetProperty("html_url").GetString(),UriKind.Absolute,out var page) || page.Scheme!="https" || page.Host!="github.com" || !page.AbsolutePath.StartsWith("/arilsonbc/AudioLivre/releases/tag/",StringComparison.Ordinal)) return null;
        return new(version,page);
    }
    public static async Task<ReleaseUpdate> Check(Version current) {
        using var client=new HttpClient { Timeout=TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("AudioLivre/"+current);
        string json=await client.GetStringAsync("https://api.github.com/repos/arilsonbc/AudioLivre/releases/latest");
        return Parse(json,current);
    }
}
