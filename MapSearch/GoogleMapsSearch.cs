using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using PowerArgs;

namespace MapSearch;

public static class GoogleMapsSearch
{
    private static readonly HttpClient Client = new HttpClient();

    public static async Task<List<PlaceResult>> SearchCabinetShopsAsync(string apiKey, string zipCodes, string radius, string keyword, string type)
    {
        var places  = new List<PlaceResult>();
        
        var zips = zipCodes.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Where(IsValidZipCode)
            .ToArray();
        
        if (zips.Length == 0)
        {
            Console.WriteLine("No valid zip codes found.");
            return places;
        }
        
        
        
        foreach (var zip in zips)
        {
            // Get the coordinates for the zip code
            string geocodeUrl = $"https://maps.googleapis.com/maps/api/geocode/json?address={zip}&key={apiKey}";
            HttpResponseMessage geocodeResponse = await Client.GetAsync(geocodeUrl);
            if (!geocodeResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error: {geocodeResponse.StatusCode}");
                Console.WriteLine("Trying the next zip code...");
            }

            string geocodeResponseBody = await geocodeResponse.Content.ReadAsStringAsync();
            JObject geocodeJson = JObject.Parse(geocodeResponseBody);
            if (geocodeJson["status"].ToString() != "OK")
            {
                Console.WriteLine($"Geocode Error: {geocodeJson["status"]}");
                Console.WriteLine("Trying the next zip code...");
            }

            var location = geocodeJson["results"][0]["geometry"]["location"];
            string latitude = location["lat"].ToString();
            string longitude = location["lng"].ToString();

            string placesUrl;
            if (!type.IsNullOrEmpty())
            {
                placesUrl =
                    $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={latitude},{longitude}&radius={radius}&types={type}&key={apiKey}";
            } else {
                placesUrl =
                    $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={latitude},{longitude}&radius={radius}&keyword={keyword}&key={apiKey}";
            }

            places.AddRange(await FetchAllResults(placesUrl, apiKey, zip));            
        }
        return places;
    }
    
    public static double MilesToMeters(double miles)
    {
        const double metersPerMile = 1609.34;
        return miles * metersPerMile;
    }
    
    private static bool IsValidZipCode(string zipCode)
    {
        return Regex.IsMatch(zipCode, @"^\d{5}(-\d{4})?$");
    }
    private static async Task<List<PlaceResult>> FetchAllResults(string url, string apiKey, string searchZip)
    {
        var results = new List<PlaceResult>();
        string nextPageToken = null;
        var fetchCount = 0;
        var baseUrl = url;
        Console.WriteLine($"Searching for results near {searchZip}...");
        do
        {
            if (nextPageToken != null)
            {
                await Task.Delay(2000); // Wait for a short period to ensure the next page token is valid
                url = $"{baseUrl}&pagetoken={nextPageToken}";
            }
            Console.WriteLine($"Fetching Page... {++fetchCount}");
            HttpResponseMessage response = await Client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject json = JObject.Parse(responseBody);
                // Console.WriteLine(responseBody);

                foreach (var result in json["results"])
                {
                    // await Task.Delay(500);
                    string placeId = result["place_id"].ToString();
                    // Console.WriteLine($"Place ID: {placeId}");
                    string detailsUrl =
                        $"https://maps.googleapis.com/maps/api/place/details/json?place_id={placeId}&fields=name,vicinity,formatted_phone_number,address_component,website,url&key={apiKey}";
                    HttpResponseMessage detailsResponse = await Client.GetAsync(detailsUrl);
                    if (detailsResponse.IsSuccessStatusCode)
                    {
                        string detailsResponseBody = await detailsResponse.Content.ReadAsStringAsync();
                        JObject detailsJson = JObject.Parse(detailsResponseBody);

                        string zipCode = null;
                        foreach (var component in detailsJson["result"]["address_components"])
                        {
                            if (component["types"].ToString().Contains("postal_code"))
                            {
                                zipCode = component["long_name"].ToString();
                                break;
                            }
                        }

                        results.Add(new PlaceResult
                        {
                            Name = detailsJson["result"]["name"].ToString(),
                            Vicinity = detailsJson["result"]["vicinity"].ToString(),
                            PhoneNumber = detailsJson["result"]["formatted_phone_number"]?.ToString(),
                            ZipCode = zipCode,
                            PlaceId = placeId, // Set PlaceId property
                            Website = detailsJson["result"]["website"]?.ToString(), // Set Website property
                            SearchZip = searchZip // Set SearchZip property
                        });
                    }
                }

                nextPageToken = json["next_page_token"]?.ToString();
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                break;
            }
        } while (!string.IsNullOrEmpty(nextPageToken));

        return results;
    }
}