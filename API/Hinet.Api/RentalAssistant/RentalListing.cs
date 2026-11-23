using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Hinet.Api;

public class RentalListing
{
    [Range(0, 10000)] public decimal Area { get; set; }
    [Range(0, 100)] public int MaxOccupants { get; set; }
    [MaxLength(200)] public string RoomType { get; set; } = "";
    [MaxLength(1000)] public string Furniture { get; set; } = "";
    [MaxLength(1000)] public string Amenities { get; set; } = "";
    [MaxLength(1000)] public string NearbyPlaces { get; set; } = "";
    [MaxLength(200)] public string Electricity { get; set; } = "";
    [MaxLength(200)] public string Water { get; set; } = "";
    [MaxLength(500)] public string OtherFees { get; set; } = "";
    [MaxLength(200)] public string Deposit { get; set; } = "";
    [MaxLength(300)] public string AccessHours { get; set; } = "";
    [MaxLength(200)] public string AvailableFrom { get; set; } = "";
    [MaxLength(300)] public string ViewingHours { get; set; } = "";
    [MaxLength(150)] public string ContactName { get; set; } = "";
    [MaxLength(200)] public string ContactPhones { get; set; } = "";
    [MaxLength(500)] public string Hashtags { get; set; } = "";
    public bool OwnerDirect { get; set; }
    public List<string> Images { get; set; } = new();

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static string Serialize(RentalListing value) => JsonSerializer.Serialize(value, Json);
    public static RentalListing Deserialize(string value) => JsonSerializer.Deserialize<RentalListing>(value, Json) ?? new();
    public static string SerializeImages(List<string> value) => JsonSerializer.Serialize(value, Json);
    public static List<string> DeserializeImages(string value) => JsonSerializer.Deserialize<List<string>>(value, Json) ?? new();
}
