namespace MapSearch;

public class PlaceResult
{
    public string Name { get; set; }
    public string Vicinity { get; set; }
    public string ZipCode { get; set; }
    public string PhoneNumber { get; set; }
    public string PlaceId { get; set; } // Added PlaceId property
    public string Website { get; set; } // Added Website property
    public string SearchZip { get; set; } // Added SearchZip property
}