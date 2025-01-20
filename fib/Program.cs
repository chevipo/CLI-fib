
using System.CommandLine;


var rootCommand = new RootCommand("A tool to bundle the contents of a few files into one file");

var bundleCommand = new Command("bundle", "Combines the contents of a few files into one file");
bundleCommand.AddAlias("b");

var languageOption = new Option<string[]>("--language",
                description: "Programming languages to include (e.g., csharp, python, java). Use 'all' to include all code files.",
                parseArgument: result =>
                {
                    var value = result.Tokens.Select(t => t.Value).ToArray();
                    if (value.Length == 0)
                    {
                        result.ErrorMessage = "At least one language must be specified.";
                    }
                    return value;
                })
{
    IsRequired = true
};
languageOption.AddAlias("-l");
bundleCommand.AddOption(languageOption);

var outputOption = new Option<FileInfo>("--output", "File path and name");
outputOption.AddAlias("-o");
bundleCommand.AddOption(outputOption);

var noteOption = new Option<bool>("--note", "Include a comment with the source file name and relative path before each file content");
noteOption.AddAlias("-n");
bundleCommand.AddOption(noteOption);

var sortOption = new Option<string>("--sort",
    description: "Sort order for files: 'name' (default) or 'type'.",
    getDefaultValue: () => "name");
sortOption.AddAlias("-s");
bundleCommand.AddOption(sortOption);

var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from code files before bundling");
removeEmptyLinesOption.AddAlias("-rel");
bundleCommand.AddOption(removeEmptyLinesOption);

var authorOption = new Option<string>("--author", "Specify the author name to include in the bundle file");
authorOption.AddAlias("-a");
bundleCommand.AddOption(authorOption);


bundleCommand.SetHandler((string[] languages, FileInfo output, bool note, string sort, bool removeEmptyLines, string author) =>
{
    try
    {
        var languageExtensions = new Dictionary<string, string[]>
        {
            { "csharp", new[] { ".cs" } },
            { "python", new[] { ".py" } },
            { "java", new[] { ".java" } },
            { "javascript", new[] { ".js" } },
            { "cpp", new[] { ".cpp", ".h" } },
            { "html", new[] { ".html" } },
            { "css", new[] { ".css" } },
            { "typescript", new[] { ".ts" } }
        };
        var selectedExtensions = languages.Contains("all")
        ? languageExtensions.Values.SelectMany(ext => ext).ToHashSet()
        : languages.SelectMany(lang => languageExtensions.GetValueOrDefault(lang, Array.Empty<string>())).ToHashSet();

        if (!selectedExtensions.Any())
        {
           throw new Exception("No valid languages selected");
        }

        var files = Directory.GetFiles(Directory.GetCurrentDirectory());
        var filteredFiles = files
            .Where(file => selectedExtensions.Contains(Path.GetExtension(file).ToLower()))
            .ToList();

        filteredFiles = sort.ToLower() switch
        {
            "type" => filteredFiles.OrderBy(file => Path.GetExtension(file).ToLower()).ThenBy(file => Path.GetFileName(file)).ToList(),
            _ => filteredFiles.OrderBy(file => Path.GetFileName(file)).ToList(),
        };
        
        using var writer = new StreamWriter(output.FullName);
        {

            if (!string.IsNullOrEmpty(author))
            {
                writer.WriteLine($"# Author: {author}");
            }

            foreach (var file in filteredFiles)
            {
                if (note)
                {
                    writer.WriteLine($"# Source: {Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}");
                }

                var content = File.ReadAllLines(file);

                if (removeEmptyLines)
                {
                    content = content.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                }

                foreach (var line in content)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();

            }
        }
        Console.WriteLine($"Files have been successfully bundled into {output.FullName}");
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("ERROR: File path is not valid");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }

}, languageOption, outputOption, noteOption, sortOption, removeEmptyLinesOption, authorOption);

rootCommand.AddCommand(bundleCommand);

var createRspCommand = new Command("create-rsp", "Create a response file for bundling files");
createRspCommand.AddAlias("c-rsp");
createRspCommand.SetHandler(() =>
{
    Console.Write("Enter output file name (e.g., bundle.txt): ");
    var output = Console.ReadLine();

    Console.Write("Enter programming languages (from: (csharp, python, java, javascript, cpp, html, css, typescript ), or 'all'): ");
    var languagesInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(languagesInput))
    {
        Console.WriteLine("You must specify at least one language or 'all'.");
        return;
    }
    var languages = languagesInput.Split(',').Select(l => l.Trim().ToLower()).ToArray();

    Console.Write("Remove empty lines? (y/n): ");
    var removeEmptyLinesInput = Console.ReadLine();
    var removeEmptyLines = removeEmptyLinesInput?.Trim().ToLower() == "y";

    Console.Write("Enter author name (optional): ");
    var author = Console.ReadLine();

    Console.Write("Sort files by (name/type) [default: name]: ");
    var sortOption = Console.ReadLine()?.Trim().ToLower();
    if (string.IsNullOrWhiteSpace(sortOption)) sortOption = "name";

    Console.Write("Include source notes? (y/n): ");
    var includeSourceNotesInput = Console.ReadLine();
    var includeSourceNotes = includeSourceNotesInput?.Trim().ToLower() == "y";

    var rspContent = new List<string>
    {
        "bundle ",
        $"--output {output}",
        $"--language {string.Join(',', languages)}",
        $"--remove-empty-lines {removeEmptyLines}",
        $"--sort {sortOption}",
        $"--author {author}",
        $"--note {includeSourceNotes}"
    };

    var rspFileName = "bundle.rsp";
    File.WriteAllLines(rspFileName, rspContent.Where(line => !string.IsNullOrWhiteSpace(line)));

    Console.WriteLine($"Response file '{rspFileName}' has been created successfully!");
    Console.WriteLine($"You can now run the bundling command with: dotnet @bundle.rsp");
});

rootCommand.AddCommand(createRspCommand);

await rootCommand.InvokeAsync(args);