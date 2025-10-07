using DotNetEnv;

class Program
{
    static readonly string inputDir;
    static readonly string solutionRoot;

    static Program()
    {
        try
        {
            solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));

            Env.Load(Path.Combine(solutionRoot, ".env"));

            inputDir = Environment.GetEnvironmentVariable("FILEPARSER_INPUT_DIR");
            if (string.IsNullOrWhiteSpace(inputDir))
            {
                throw new InvalidOperationException("Variable FILEPARSER_INPUT_DIR not set in .env");
            }

            inputDir = Environment.GetEnvironmentVariable("FILEPARSER_INPUT_DIR");
            if (string.IsNullOrWhiteSpace(inputDir))
            {
                throw new InvalidOperationException("Переменная FILEPARSER_INPUT_DIR не задана в .env");
            }

            inputDir = Path.GetFullPath(Path.Combine(solutionRoot, inputDir));
            if (!IsValidPath(inputDir))
            {
                throw new InvalidOperationException($"Некорректный путь в FILEPARSER_INPUT_DIR: {inputDir}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initialization error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    static void Main()
    {
        try
        {
            Directory.CreateDirectory(inputDir);
            Console.WriteLine($"Input directory {inputDir} is ready.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Directory initialization error {inputDir}: {ex.Message}");
            Environment.Exit(1);
        }

        while (true)
        {
            Console.WriteLine("\n=== File Manager CLI ===");
            Console.WriteLine("1. List files");
            Console.WriteLine("2. Add file");
            Console.WriteLine("3. Delete file");
            Console.WriteLine("4. Exit");
            Console.Write("Choose option: ");

            var choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ListFiles();
                    break;
                case "2":
                    AddFile();
                    break;
                case "3":
                    DeleteFile();
                    break;
                case "4":
                    return;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }

    static void ListFiles()
    {
        var files = Directory.GetFiles(inputDir);
        if (files.Length == 0)
        {
            Console.WriteLine("No files in /app/input.");
            return;
        }

        Console.WriteLine("\nFiles in /app/input:");
        foreach (var file in files)
            Console.WriteLine($" - {Path.GetFileName(file)}");
    }

    static void AddFile()
    {
        Console.Write("Enter full path to the file (on host): ");
        var sourcePath = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            Console.WriteLine("File not found on host.");
            return;
        }

        if (Path.GetExtension(sourcePath).ToLower() != ".xml")
        {
            Console.WriteLine("File must have .xml extension.");
            return;
        }

        var fileName = Path.GetFileName(sourcePath);
        var destPath = Path.Combine(inputDir, fileName);

        try
        {
            File.Copy(sourcePath, destPath, overwrite: true);
            Console.WriteLine($"File '{fileName}' copied to {inputDir}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying file: {ex.Message}");
        }
    }

    static void DeleteFile()
    {
        ListFiles();

        Console.Write("Enter filename to delete: ");
        var fileName = Console.ReadLine();
        var filePath = Path.Combine(inputDir, fileName);

        if (!File.Exists(filePath))
        {
            Console.WriteLine("File not found.");
            return;
        }

        File.Delete(filePath);
        Console.WriteLine($"File '{fileName}' deleted from /app/input.");
    }

    static bool IsValidPath(string path)
    {
        try
        {
            Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
