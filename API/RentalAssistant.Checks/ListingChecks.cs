using Hinet.Api;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

public static class ListingChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var directory = Path.Combine(Path.GetTempPath(), "AI_TOOL_listing_checks", Guid.NewGuid().ToString("N"));
        var media = new RentalMedia(directory);
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Zl1sAAAAASUVORK5CYII=");
        using var imageStream = new MemoryStream(png);
        var images = await media.Upload(new FormFileCollection { new FormFile(imageStream, 0, png.Length, "files", "room.png") }, default);
        check(images.Count == 1 && File.ReadAllBytes(media.Resolve(images[0])).SequenceEqual(png), "uploaded room image survives storage round trip");
        check(!RentalMedia.ValidId("../settings.json") && !RentalMedia.ValidId("https://example.com/room.jpg"), "image identifiers reject paths and remote URLs");
        bool rejected = false;
        using var fake = new MemoryStream("not an image"u8.ToArray());
        try { await media.Upload(new FormFileCollection { new FormFile(fake, 0, fake.Length, "files", "fake.jpg") }, default); }
        catch (ArgumentException) { rejected = true; }
        check(rejected, "upload checks file content instead of extension");
        rejected = false;
        try { media.ValidateImages(new[] { images[0], images[0] }); } catch (ArgumentException) { rejected = true; }
        check(rejected, "room images cannot contain duplicates");
        var room = new RentalRoom { Name = "Phòng A", Address = "Bùi Xương Trạch", MonthlyRent = 3000000,
            Listing = new() { Area = 30, ContactPhones = "0912345678", Furniture = "Điều hòa, giường", Electricity = "3.500 đ/kWh", OwnerDirect = true, Hashtags = "#phongtro", Images = images } };
        var text = RentalTemplates.Draft(room);
        check(text.Contains("0912345678") && text.Contains("Điều hòa") && text.Contains("3.500") && text.Contains("#phongtro") && !text.Contains(images[0]), "draft uses listing facts without image IDs");
        room.Listing.Area = -1;
        var errors = new List<ValidationResult>();
        check(!Validator.TryValidateObject(room, new ValidationContext(room), errors, true), "negative room area is rejected");
        // Remove only files created by this check; keep unrelated temp directories untouched.
        foreach (var id in images) File.Delete(media.Resolve(id));
        Directory.Delete(directory);
    }
}
