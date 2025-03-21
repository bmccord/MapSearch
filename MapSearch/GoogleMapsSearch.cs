using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PowerArgs;

namespace MapSearch;

public static class GoogleMapsSearch
{
    private static readonly HttpClient Client = new HttpClient();

    public static async Task<List<PlaceResult>> SearchCabinetShopsAsync(string apiKey, string zipCodes, string radius,
        string keyword, string type)
    {
        var places = new List<PlaceResult>();

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
                continue;
            }

            string geocodeResponseBody = await geocodeResponse.Content.ReadAsStringAsync();
            JObject geocodeJson = JObject.Parse(geocodeResponseBody);
            if (geocodeJson["status"].ToString() != "OK")
            {
                Console.WriteLine($"Geocode Error: {geocodeJson["status"]}");
                Console.WriteLine("Trying the next zip code...");
                continue;
            }

            var location = geocodeJson["results"][0]["geometry"]["location"];
            string latitude = location["lat"].ToString();
            string longitude = location["lng"].ToString();
            
            var requestParametersSearchText = new RequestParametersSearchText()
            {
                locationBias = new LocationBias()
                {
                    circle = new Circle()
                    {
                        center = new Center()
                        {
                            latitude = latitude,
                            longitude = longitude
                        },
                        radius = radius
                    }
                }
            };
            
            var requestParametersSearchTypes = new RequestParametersSearchTypes()
            {
                locationRestriction = new LocationBias()
                {
                    circle = new Circle()
                    {
                        center = new Center()
                        {
                            latitude = latitude,
                            longitude = longitude
                        },
                        radius = radius
                    }
                }
            };            

            places.AddRange(await FetchAllResults(apiKey, keyword, type, requestParametersSearchText, requestParametersSearchTypes, zip));
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

    private static async Task<List<PlaceResult>> FetchAllResults(string apiKey, string keyword, string type,
        RequestParametersSearchText requestParametersSearchText, RequestParametersSearchTypes requestParametersSearchTypes, string searchZip)
    {
        var results = new List<PlaceResult>();
        string nextPageToken = null;
        var fetchCount = 0;
        // var baseUrl = url;
        Console.WriteLine($"Searching for results near {searchZip}...");

        do
        {
            string fields = "places.id,places.displayName,places.formattedAddress,places.nationalPhoneNumber,places.websiteUri";
            if (type.IsNullOrEmpty())
            {
                fields += ",nextPageToken";
            }
            
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers =
                {
                    { "x-goog-api-key", apiKey },
                    {
                        "x-goog-fieldmask",
                        fields
                    },
                }
            };

            if (!type.IsNullOrEmpty())
            {
                request.RequestUri = new Uri("https://places.googleapis.com/v1/places:searchNearby");
                requestParametersSearchTypes.includedTypes = type.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                request.RequestUri = new Uri("https://places.googleapis.com/v1/places:searchText");
                requestParametersSearchText.textQuery = keyword;
            }

            if (nextPageToken != null)
            {
                await Task.Delay(2000); // Wait for a short period to ensure the next page token is valid
                requestParametersSearchText.pageToken = nextPageToken;
            }

            var requestBody = !type.IsNullOrEmpty() ? JsonConvert.SerializeObject(requestParametersSearchTypes) : JsonConvert.SerializeObject(requestParametersSearchText);
            
            request.Content = new StringContent(requestBody)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            };

            Console.WriteLine($"Fetching Page... {++fetchCount}");
            HttpResponseMessage response = await Client.SendAsync(request);

            string responseBody = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(responseBody);
            
            if (response.IsSuccessStatusCode)
            {
                foreach (var place in json["places"])
                {
                    results.Add(new PlaceResult
                    {
                        Name = place["displayName"]["text"].ToString(),
                        Address = place["formattedAddress"].ToString(),
                        PhoneNumber = place["nationalPhoneNumber"]?.ToString(),
                        PlaceId = place["id"].ToString(),
                        Website = place["websiteUri"]?.ToString(),
                        SearchZip = searchZip
                    });
                }

                nextPageToken = json["nextPageToken"]?.ToString();
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine(json);
                break;
            }
        } while (!string.IsNullOrEmpty(nextPageToken));

        return results;
    }
}