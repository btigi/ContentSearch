using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Drawing;

var builder = new ConfigurationBuilder()
                .AddJsonFile($"cs.json", true, true);

var config = builder.Build();
var fileForegroundColour = Color.FromName(config["FileForegroundColour"] ?? "Yellow");
var fileBackgroundColour = Color.FromName(config["FileBackgroundColour"] ?? "Black");

var fileForegroundConsoleColour = FromColour(fileForegroundColour);
var fileBackgroundConsoleColour = FromColour(fileBackgroundColour);

var lineNumberGoregroundColor = Color.FromName(config["LineNumberForegroundColor"] ?? "Cyan");
var lineNumberGoregroundConsoleColor = FromColour(lineNumberGoregroundColor);

var lineForegroundColour = Color.FromName(config["LineForegroundColour"] ?? "White");
var lineForegroundConsoleColour = FromColour(lineForegroundColour);

var lineBackgroundColour = Color.FromName(config["LineBackgroundColour"] ?? "Black");
var lineBackgroundConsoleColour = FromColour(lineBackgroundColour);

var defaultForegroundColour = Color.FromName(config["DefaultForegroundColour"] ?? "White");
var defaultForegroundConsoleColour = FromColour(defaultForegroundColour);

var defaultBackgroundColour = Color.FromName(config["DefaultBackgroundColour"] ?? "Black");
var defaultBackgroundConsoleColour = FromColour(defaultBackgroundColour);

if (args.Length < 3)
{
    Console.WriteLine("Usage: ");
    Console.WriteLine("  directory fileextension search text");
    Console.WriteLine("  e.g. cs C:\\source cs public void");
    return;
}

if (!Directory.Exists(args[0]))
{
    Console.WriteLine($"The specified directory '{args[0]}' does not exist");
    return;
}

var searchFolder = args[0];
var extension = args[1];

var searchText = String.Empty;
foreach (var arg in args.Skip(2))
{
    searchText += arg + " ";
}
searchText = searchText.Trim();


var hits = SearchContentListInFiles(searchFolder, extension, searchText);

foreach (var fileHit in hits.GroupBy(gb => gb.file))
{
    Console.ForegroundColor = fileForegroundConsoleColour;
    Console.BackgroundColor = fileBackgroundConsoleColour;
    // Swap \ for / to Windows Terminal makes a clickable link
    Console.WriteLine($"file:///{fileHit.Key.Replace('\\', '/')}");

    foreach (var (file, lineNumber, content) in fileHit)
    {
        Console.BackgroundColor = lineBackgroundConsoleColour;
        Console.ForegroundColor = lineNumberGoregroundConsoleColor;
        Console.Write($"  [{lineNumber}]  ");
        Console.ForegroundColor = lineForegroundConsoleColour;
        Console.Write($"{content.Trim()}");
        Console.WriteLine();
    }
    Console.WriteLine();
}

Console.ForegroundColor = defaultForegroundConsoleColour;
Console.BackgroundColor = defaultBackgroundConsoleColour;



static IEnumerable<(string file, int lineNumber, string content)> SearchContentListInFiles(string searchFolder, string extension, string searchText)
{
    var result = new BlockingCollection<(string file, int line, string content)>();

    var files = Directory.EnumerateFiles(searchFolder, $"*.{extension}", SearchOption.AllDirectories);
    Parallel.ForEach(files, (file) =>
    {
        var fileContent = File.ReadLines(file);

        var fileContentResult = fileContent.Select((line, i) => new { line, i })
              .Where(x => x.line.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
              .Select(s => new { s.i, s.line });

        foreach (var r in fileContentResult)
        {
            result.Add((file, r.i, r.line));
        }
    });

    return result;
}

// https://stackoverflow.com/a/29192463/9659
static ConsoleColor FromColour(Color c)
{
    int index = (c.R > 128 | c.G > 128 | c.B > 128) ? 8 : 0; // Bright bit
    index |= (c.R > 64) ? 4 : 0; // Red bit
    index |= (c.G > 64) ? 2 : 0; // Green bit
    index |= (c.B > 64) ? 1 : 0; // Blue bit
    return (System.ConsoleColor)index;
}