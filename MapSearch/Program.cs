using System.Globalization;
using CsvHelper;
using PowerArgs;

namespace MapSearch;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await Args.InvokeMainAsync<ProgramArgs>(args);      
    }
}