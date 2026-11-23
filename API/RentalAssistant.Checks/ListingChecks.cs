using Hinet.Api;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

public static class ListingChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        using (var db = new RentalDb(new DbContextOptionsBuilder<RentalDb>().UseSqlServer("Server=localhost;Database=AI_TOOL;Integrated Security=true").Options))
        {
            var comparer = db.Model.FindEntityType(typeof(RentalPost))!.FindProperty(nameof(RentalPost.Images))!.GetValueComparer()!;
            var photos = new List<string> { "cover.jpg", "interior.jpg" };
            check(!comparer.Equals(photos, null) && !comparer.Equals(null, photos) && comparer.Equals(null, null), "post image comparer handles EF null defaults in both directions");
            check(!comparer.Equals(photos, new List<string> { "interior.jpg", "cover.jpg" }), "post image comparer preserves cover order");
            var snapshot = (List<string>)comparer.Snapshot(photos)!;
            photos.Add("extra.jpg");
            check(snapshot.Count == 2 && !comparer.Equals(photos, snapshot), "image snapshot detects changes without mutating original snapshot");
            var post = new RentalPost { RoomId = Guid.NewGuid(), Content = "Bản nháp có ảnh", Images = photos };
            db.Posts.Add(post);
            db.ChangeTracker.DetectChanges();
            check(db.Entry(post).State == EntityState.Added, "SQL Server change tracker accepts a new draft with images and store defaults");
            var trackedRoom = new RentalRoom { Name = "Phòng kiểm tra", Address = "Hà Nội", MonthlyRent = 3000000 };
            db.Rooms.Attach(trackedRoom);
            trackedRoom.Listing.Furniture = "Điều hòa";
            db.ChangeTracker.DetectChanges();
            check(db.Entry(trackedRoom).Property(r => r.Listing).IsModified, "listing JSON comparer detects nested updates");
            var builder = db.GetService<IRawSqlCommandBuilder>();
            foreach (var sql in new[] { RentalSchemaUpgrade.Sql, db.Database.GenerateCreateScript() })
            {
                var command = builder.Build(RentalSchemaUpgrade.EscapeLiteralSql(sql), Array.Empty<object>());
                check(command.RelationalCommand.CommandText == sql, "EF formats schema SQL while preserving literal JSON braces");
            }
        }
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
