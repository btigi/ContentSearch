using System.Collections.Concurrent;

if (args.Length != 3)
{
    Console.WriteLine("Usage: ");
    Console.WriteLine("  directory fileextension searchtext");
    Console.WriteLine("  e.g. cs C:\\source cs enumerable");
    return;
}

if (!Directory.Exists(args[0]))
{
    Console.WriteLine("The specified directory '{0}' does not exist");
    return;
}

var searchFolder = args[0];
var extension = args[1];
var searchText = args[2];

var hits = SearchContentListInFiles(searchFolder, extension, searchText);

foreach (var fileHit in hits.GroupBy(gb => gb.file))
{
    // Swap \ for / to Windows Terminal makes a clickable link
    Console.WriteLine($"file:///{fileHit.Key.Replace('\\', '/')}");

    foreach (var (file, lineNumber, content) in fileHit)
    {
        Console.WriteLine($"  [{lineNumber}]  {content.Trim()}");
    }
    Console.WriteLine();
}


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