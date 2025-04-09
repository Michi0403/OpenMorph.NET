using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;

namespace OpenMorph.NET
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                // Define the filepath argument
                new Option<string>(
                    "--filepath",
                    description: "Path to the STL file to be processed."),

                // Optional force flag if needed later
                new Option<bool>(
                    "-f",
                    "Force process file even if warnings are present")
            };

            rootCommand.Description = "OpenMorph.NET - A tool to convert STL to OpenSCAD";

            rootCommand.Handler = CommandHandler.Create<string, bool>(async (filepath, force) =>
            {
                // If no file path is provided, offer files in the current directory
                if (string.IsNullOrWhiteSpace(filepath))
                {
                    filepath = await OfferStlFilesInDirectory();
                }

                // Validate file path argument
                if (string.IsNullOrWhiteSpace(filepath))
                {
                    Console.WriteLine("Error: No valid file path provided, and no STL files were found.");
                    return;
                }

                if (!File.Exists(filepath))
                {
                    Console.WriteLine($"Error: The specified file does not exist: {filepath}");
                    return;
                }

                // Process the STL file
                try
                {
                    string stlContent = ReadStlFile(filepath);
                    Console.WriteLine("STL File Content (First 500 characters):");
                    Console.WriteLine(stlContent.Substring(0, Math.Min(500, stlContent.Length)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file: {ex.Message}");
                }
            });

            await rootCommand.InvokeAsync(args);
        }

        // Method to read STL file content
        static string ReadStlFile(string filePath)
        {
            StringBuilder fileContent = new StringBuilder();

            const int bufferSize = 8192; // 8 KB chunks
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                char[] buffer = new char[bufferSize];
                int bytesRead;

                while ((bytesRead = reader.Read(buffer, 0, bufferSize)) > 0)
                {
                    fileContent.Append(buffer, 0, bytesRead);
                }
            }

            return fileContent.ToString();
        }

        // Method to offer STL files in the current directory if no filepath is provided
        static async Task<string> OfferStlFilesInDirectory()
        {
            string[] stlFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.stl");

            if (stlFiles.Length == 0)
            {
                Console.WriteLine("No STL files found in the current directory.");
                return null;
            }

            // List the files and ask the user to select one
            Console.WriteLine("No file path provided. Here are the available STL files:");

            for (int i = 0; i < stlFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(stlFiles[i])}");
            }

            Console.Write("Please select a file by number (or press Enter to cancel): ");
            string userInput = Console.ReadLine();

            if (int.TryParse(userInput, out int selectedFileIndex) && selectedFileIndex >= 1 && selectedFileIndex <= stlFiles.Length)
            {
                return stlFiles[selectedFileIndex - 1]; // Return the selected file path
            }

            Console.WriteLine("No valid selection made.");
            return null;
        }
    }
}
