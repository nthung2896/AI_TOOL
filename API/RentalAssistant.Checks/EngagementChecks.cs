using Hinet.Api;
public static class EngagementChecks
{
    public static void Run(Action<bool, string> check)
    {
        check(RentalFacebook.ValidUrl("https://www.facebook.com/messages/e2ee/t/123456", false), "encrypted Messenger conversation URLs are supported");
        var selected = new RentalRoom { Name = "Ngõ 1 Bùi Xương Trạch", Available = true };
        var other = new RentalRoom { Name = "Phòng Thanh Xuân", Available = true };
        check(RentalTelegram.SelectRoom(new[] { selected, other }, "Đăng bài cho thuê phòng Ngõ 1 Bùi Xương Trạch, nhấn mạnh nội thất") == selected, "Telegram selects the named room among multiple available rooms");
        check(RentalTelegram.SelectRoom(new[] { selected, other }, "/dang ngo 1 bui xuong trach") == selected, "Telegram room lookup accepts text without Vietnamese accents");
        check(RentalTelegram.SelectRoom(new[] { selected, new RentalRoom { Name = selected.Name } }, "Đăng " + selected.Name) is null, "duplicate room names require clarification instead of posting the wrong room");
        check(RentalTelegram.InteractionNotice("Phòng", "Nhóm", "https://example.test", new(null,null,null), new(5,2,1)) is null, "first metrics read establishes a baseline without reporting old interactions as new");
        check(RentalTelegram.InteractionNotice("Phòng", "Nhóm", "https://example.test", new(5,2,1), new(5,2,1)) is null, "unchanged interactions do not generate Telegram notices");
        var notice = RentalTelegram.InteractionNotice("Phòng", "Nhóm", "https://example.test", new(0,2,1), new(1,3,1));
        check(notice is not null && notice.Contains("Cảm xúc: +1") && notice.Contains("Bình luận: +1") && notice.Contains("https://example.test"), "interaction increases produce one notice with deltas and the post link");
        check(notice!.StartsWith("CÓ BÌNH LUẬN MỚI"), "new comment notifications include a clear comment heading");
        var incoming = new List<InboxMessage> { new("m1", "Xin giá", ""), new("m2", "Còn phòng không?", ""), new("m3", "Muốn xem phòng", "") };
        check(RentalTelegram.NewMessages(incoming, "", false, false).Count == 0, "first inbox scan baselines existing history without spamming old messages");
        var fresh = RentalTelegram.NewMessages(incoming, "m1", true, false);
        check(fresh.Count == 2 && fresh[0].Id == "m2" && fresh[1].Id == "m3", "all visible incoming messages after the saved cursor are notified");
        check(RentalTelegram.NewMessages(incoming, "m3", true, false).Count == 0, "repeated inbox polls do not generate duplicate message notifications");
        check(RentalTelegram.NewMessages(incoming, "", false, true).Single().Id == "m3", "a newly discovered conversation notifies its latest incoming message");
        var messageNotice = RentalTelegram.IncomingNotice("Khách thử", "https://www.facebook.com/messages/t/123", fresh);
        check(messageNotice.Contains("Còn phòng không?") && messageNotice.Contains("Muốn xem phòng") && messageNotice.Contains("https://www.facebook.com/messages/t/123"), "Messenger notifications include the incoming text and conversation link");
        check(RentalEngagement.ValidPostUrl("https://www.facebook.com/groups/nhatro/posts/12345?ref=share"), "accept direct Facebook group post links");
        check(RentalEngagement.ValidPostUrl("https://facebook.com/groups/123/permalink/456/"), "accept group permalink links");
        foreach (var url in new[] { "https://facebook.com.evil.test/groups/a/posts/1", "https://user@facebook.com/groups/a/posts/1", "http://facebook.com/groups/a/posts/1", "https://facebook.com/groups/a", "https://facebook.com/messages/t/1" })
            check(!RentalEngagement.ValidPostUrl(url), "reject non-post or untrusted tracking link");
        var english = RentalEngagement.Parse(new[] { "All reactions: 1.2K", "12 comments", "3 shares" });
        check(english == new RentalMetrics(1200, 12, 3), "read English reaction, comment and share counters");
        var vietnamese = RentalEngagement.Parse(new[] { "1.234 cảm xúc", "0 bình luận", "2 lượt chia sẻ" });
        check(vietnamese == new RentalMetrics(1234, 0, 2), "read Vietnamese counts and preserve explicit zero");
        check(RentalEngagement.Parse(new[] { "Like", "Comment", "Share" }) == new RentalMetrics(null, null, null), "hidden counters remain unknown instead of zero");
        check(RentalEngagement.Canonical("https://facebook.com/groups/a/posts/1/?ref=share") == "https://www.facebook.com/groups/a/posts/1", "tracking URLs strip query parameters");
        check(RentalPostLinks.Normalize("https://www.facebook.com/groups/nhatro/?multi_permalinks=123&ref=share") == "https://www.facebook.com/groups/nhatro/posts/123", "timestamp multi_permalinks normalize to direct post links");
        check(RentalPostLinks.Normalize("https://www.facebook.com/groups/nhatro/posts/pfbidAbC123") is not null, "pfbid post identifiers are supported");
        check(RentalPostLinks.IsCreationRequest("fb_api_req_friendly_name=ComposerStoryCreateMutation") && !RentalPostLinks.IsCreationRequest("fb_api_req_friendly_name=CometUFICreateCommentMutation"), "only story creation responses can supply the newly submitted post link");
        var response = "for (;;);{\"data\":{\"story_create\":{\"story\":{\"url\":\"https://www.facebook.com/groups/123/posts/pfbidXYZ\",\"legacy_fbid\":\"456\"}}}}";
        check(RentalPostLinks.FromCreationResponse(response, "https://www.facebook.com/groups/nhatro") == "https://www.facebook.com/groups/123/posts/pfbidXYZ", "creation response captures the actual permalink rather than an ID-derived alias");
        check(RentalPostLinks.FromCreationResponse("{\"data\":{\"feed\":{\"url\":\"https://www.facebook.com/groups/123/posts/99\"}}}", "https://www.facebook.com/groups/nhatro") is null, "unrelated feed URLs cannot be mistaken for a newly created story");
    }
}
