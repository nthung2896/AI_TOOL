using System.Text.Json;
using System.Text.RegularExpressions;

namespace Hinet.Api;

public static class RentalPostLinks
{
    private static readonly Regex Id = new(@"\A(?:[0-9]+|pfbid[A-Za-z0-9]+)\z");
    public static string? Normalize(string? value, string? groupUrl = null)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Port != 443 ||
            !string.IsNullOrEmpty(uri.UserInfo) || uri.Host is not ("www.facebook.com" or "facebook.com")) return null;
        if (Regex.IsMatch(uri.AbsolutePath, @"^/groups/[a-zA-Z0-9.]+/(posts|permalink)/(?:[0-9]+|pfbid[A-Za-z0-9]+)/?$"))
            return "https://www.facebook.com" + uri.AbsolutePath.TrimEnd('/');
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
        if (Regex.IsMatch(uri.AbsolutePath, @"^/groups/[a-zA-Z0-9.]+/?$") && query.TryGetValue("multi_permalinks", out var ids) && Id.IsMatch(ids.ToString()))
            return "https://www.facebook.com" + uri.AbsolutePath.TrimEnd('/') + "/posts/" + ids;
        if (uri.AbsolutePath is "/permalink.php" or "/story.php" && groupUrl is not null && RentalFacebook.ValidUrl(groupUrl, true) &&
            query.TryGetValue("story_fbid", out var story) && Id.IsMatch(story.ToString()))
            return RentalFacebook.CanonicalUrl(groupUrl) + "/posts/" + story;
        return null;
    }
    public static bool IsCreationRequest(string? postData)
    {
        if (string.IsNullOrEmpty(postData)) return false;
        var fields = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery("?" + postData);
        var name = fields.TryGetValue("fb_api_req_friendly_name", out var value) ? value.ToString() : "";
        return name.Contains("StoryCreate", StringComparison.OrdinalIgnoreCase) || name.Contains("CreateStory", StringComparison.OrdinalIgnoreCase) ||
            (name.Contains("Composer", StringComparison.OrdinalIgnoreCase) && name.Contains("PostCreation", StringComparison.OrdinalIgnoreCase));
    }
    public static string? FromCreationResponse(string text, string groupUrl)
    {
        if (text.Length > 4_000_000) return null;
        text = text.Trim();
        if (text.StartsWith("for (;;);")) text = text[9..].Trim();
        var matches = new HashSet<string>();
        var fallback = new HashSet<string>();
        void Visit(JsonElement node, bool inCreatedStory = false)
        {
            if (node.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in node.EnumerateObject())
                {
                    var created = inCreatedStory || prop.Name is "story_create" or "story_create_result" or "created_story";
                    if (created && prop.Value.ValueKind == JsonValueKind.String)
                    {
                        if (prop.Name is "url" or "permalink" or "permalink_url" or "story_url")
                        { var url = Normalize(prop.Value.GetString(), groupUrl); if (url is not null) matches.Add(url); }
                        else if (prop.Name is "legacy_fbid" or "post_id")
                        { var id = prop.Value.GetString(); if (id is not null && Id.IsMatch(id)) fallback.Add(RentalFacebook.CanonicalUrl(groupUrl) + "/posts/" + id); }
                    }
                    Visit(prop.Value, created);
                }
            }
            else if (node.ValueKind == JsonValueKind.Array) foreach (var child in node.EnumerateArray()) Visit(child, inCreatedStory);
        }
        // Facebook can stream one JSON object per line.
        try { using var json = JsonDocument.Parse(text); Visit(json.RootElement); }
        catch (JsonException)
        {
            foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                try { using var json = JsonDocument.Parse(line); Visit(json.RootElement); }
                catch (JsonException) { }
            }
        }
        if (matches.Count == 0) matches.UnionWith(fallback);
        // Prefer the actual URL over an ID-derived alias for the same newly created post.
        var distinctPosts = matches.GroupBy(url => new Uri(url).AbsolutePath.Split('/').Last()).ToList();
        return distinctPosts.Count == 1 ? distinctPosts[0].First() : null;
    }
}
