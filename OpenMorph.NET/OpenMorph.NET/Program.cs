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
                    "Force process file even if warnings are present"),

                // Option to force the format
                new Option<string>(
                    "--format",
                    description: "Force the file format: 'ascii' or 'binary'. If not provided, auto-detection is used."),

                // Option to set the maximum string length (default to maximum string length in .NET)
                new Option<int>(
                    "--max-length",
                    getDefaultValue: () => Int32.MaxValue,
                    description: "The maximum number of characters to display from the STL file. Defaults to maximum string length.")
            };

            rootCommand.Description = "OpenMorph.NET - A tool to convert STL to OpenSCAD";

            rootCommand.Handler = CommandHandler.Create<string, bool, string, int>(async (filepath, force, format, maxLength) =>
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

                // Format detection and optional overwrite
                string fileFormat = format ?? DetectStlFormat(filepath);
                Console.WriteLine($"Detected STL Format: {fileFormat}");

                // Process the STL file
                try
                {
                    string stlContent = ReadStlFile(filepath, fileFormat);
                    Console.WriteLine("STL File Content (First " + maxLength + " characters):");
                    Console.WriteLine(stlContent.Substring(0, Math.Min(maxLength, stlContent.Length)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading file: {ex.Message}");
                }
            });

            await rootCommand.InvokeAsync(args);
        }

        // Method to read STL file content based on format (ASCII or Binary)
        static string ReadStlFile(string filePath, string format)
        {
            if (format.ToLower() == "ascii")
            {
                return ReadAsciiStlFile(filePath);
            }
            else if (format.ToLower() == "binary")
            {
                return ReadBinaryStlFile(filePath);
            }
            else
            {
                throw new InvalidOperationException("Invalid STL format specified.");
            }
        }

        // Method to read ASCII STL file content
        static string ReadAsciiStlFile(string filePath)
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

        // Method to read Binary STL file content
        static string ReadBinaryStlFile(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            StringBuilder fileContent = new StringBuilder();

            // Read binary STL header and number of triangles
            int numTriangles = BitConverter.ToInt32(fileBytes, 80);  // Number of triangles starts at byte 80
            fileContent.AppendLine($"Number of Triangles: {numTriangles}");

            // Read the triangles (each triangle is 50 bytes)
            for (int i = 0; i < numTriangles; i++)
            {
                int offset = 84 + i * 50;  // The first 84 bytes are header and number of triangles
                byte[] triangleData = new byte[50];
                Array.Copy(fileBytes, offset, triangleData, 0, 50);
                fileContent.AppendLine($"Triangle {i + 1}: {BitConverter.ToString(triangleData)}");
            }

            return fileContent.ToString();
        }

        // Method to detect STL file format (ASCII or Binary)
        static string DetectStlFormat(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] header = new byte[5];
                fs.Read(header, 0, 5);

                // Check if it starts with 'solid' (ASCII STL)
                if (Encoding.ASCII.GetString(header) == "solid")
                {
                    return "ascii";
                }
                else
                {
                    // Binary STL typically doesn't start with "solid"
                    return "binary";
                }
            }
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
