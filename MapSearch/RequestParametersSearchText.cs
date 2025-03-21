public class Center
{
    public string latitude { get; set; }
    public string longitude { get; set; }
}

public class Circle
{
    public Center center { get; set; }
    public string radius { get; set; }
}

public class LocationBias
{
    public Circle circle { get; set; }
}

public class RequestParametersSearchText
{
    public LocationBias locationBias { get; set; }
    public int pageSize { get; set; } = 20;
    public string pageToken { get; set; }
    public string textQuery { get; set; }

}

public class RequestParametersSearchTypes
{
    public List<string> includedTypes { get; set; }
    public int maxResultCount { get; set; } = 20;
    public LocationBias locationRestriction { get; set; }
}
