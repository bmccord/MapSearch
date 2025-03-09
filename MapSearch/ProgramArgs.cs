using System.Globalization;
using CsvHelper;
using PowerArgs;

namespace MapSearch;

[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public class ProgramArgs
{
        // This argument is required and if not specified the user will
        // be prompted.
        [ArgRequired(), ArgDescription("Google Maps API Key"), ArgShortcut("-k")]
        public string GoogleApiKey { get; set; }
        
        [ArgRequired(), ArgDescription("Zip Codes to search"), ArgShortcut("-z")]
        public string Zips { get; set; }

        [ArgDescription("Radius in miles"), ArgShortcut("-r")]
        public int Radius { get; set; } = 50;
        
        [ArgRequired(), ArgDescription("Search term"), ArgShortcut("-s")]
        public string SearchTerm { get; set; }
        
        [ArgDescription("Output file"), ArgShortcut("-o")]
        public string OutputFile { get; set; } = "output.csv";
        
        [HelpHook, ArgShortcut("-?"), ArgDescription("Shows this help")]
        public bool Help { get; set; }
        
        // This non-static Main method will be called and it will be able to access the parsed and populated instance level properties.
        public async Task Main()
        {
                try
                {
                        var results = await GoogleMapsSearch.SearchCabinetShopsAsync(GoogleApiKey, Zips, GoogleMapsSearch.MilesToMeters(Radius).ToString(CultureInfo.InvariantCulture));
                        results = RemoveDuplicates(results);
                        results = results.OrderBy(r=>r.SearchZip).ThenBy(r => r.Name).ToList(); // Sort by name
                        foreach (var result in results)
                        {
                                Console.WriteLine($"Name: {result.Name}, Vicinity: {result.Vicinity}, Zip: {result.ZipCode}, Phone: {result.PhoneNumber}, Website: {result.Website}");
                        }

                        string directory = Path.GetDirectoryName(OutputFile);
                        if (!directory.IsNullOrEmpty() && !Directory.Exists(directory))
                        {
                                Directory.CreateDirectory(directory);
                        }
                        await using (var writer = new StreamWriter(OutputFile))
                        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                        {
                                await csv.WriteRecordsAsync(results.Select(s=> new {s.Name, s.Vicinity, s.ZipCode, s.PhoneNumber, s.Website, s.SearchZip}));
                        }
            
                        Console.WriteLine($"Total Results: {results.Count}");
                        Console.WriteLine($"Data written to {OutputFile}");
                        Console.WriteLine("Done!");
                }
                catch (Exception e)
                {
                        Console.WriteLine(e);
                        throw;
                }
        }
        private static List<PlaceResult> RemoveDuplicates(List<PlaceResult> list)
        {
                var uniqueResults = new HashSet<string>();
                var filteredList = new List<PlaceResult>();

                foreach (var item in list)
                {
                        if (uniqueResults.Add(item.PlaceId))
                        {
                                filteredList.Add(item);
                        }
                }

                return filteredList;
        }

}