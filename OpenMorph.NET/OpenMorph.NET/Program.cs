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
                    // Destructure the tuple returned by ReadStlFile into points and faces
                    var (points, faces) = ReadStlFile(filepath, fileFormat);

                    Console.WriteLine("STL File Content (First " + maxLength + " characters):");
                    Console.WriteLine(points.Substring(0, Math.Min(maxLength, points.Length)));

                    // Generate OpenSCAD code
                    string openScadCode = GenerateOpenScadCode(filepath, fileFormat);

                    // Write the OpenSCAD code to a file with the same name as the STL file
                    string scadFilePath = Path.ChangeExtension(filepath, ".scad");
                    File.WriteAllText(scadFilePath, openScadCode);
                    Console.WriteLine($"OpenSCAD code has been written to {scadFilePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing the file: {ex.Message}");
                }
            });

            await rootCommand.InvokeAsync(args);
        }

        // Method to read STL file content based on format (ASCII or Binary)
        static (string, string) ReadStlFile(string filePath, string format)
        {
            if (format.ToLower() == "ascii")
            {
                // If the format is ASCII, return the result from ReadAsciiStlFile
                return ReadAsciiStlFile(filePath); // This must return a tuple like (points, faces)
            }
            else if (format.ToLower() == "binary")
            {
                // If the format is binary, return the result from ReadBinaryStlFile
                return ReadBinaryStlFile(filePath); // This returns a tuple (points, faces)
            }
            else
            {
                throw new InvalidOperationException("Invalid STL format specified.");
            }
        }

        // Method to read ASCII STL file content and extract points/faces
        static (string, string) ReadAsciiStlFile(string filePath)
        {
            var points = new List<string>(); // To store point coordinates
            var faces = new List<string>();  // To store faces as index-based references

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                string line;
                string vertexLine;
                int pointIndex = 0; // This will help us track point indices for faces

                while ((line = reader.ReadLine()) != null)
                {
                    // Look for vertices in the file
                    if (line.Trim().StartsWith("vertex"))
                    {
                        vertexLine = line.Trim();
                        var parts = vertexLine.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (parts.Length == 4)
                        {
                            // Add the vertex (x, y, z) to points
                            points.Add($"[{parts[1]}, {parts[2]}, {parts[3]}]");
                        }
                    }

                    // Look for a new facet (this means the beginning of a new triangle)
                    if (line.Trim().StartsWith("facet"))
                    {
                        // When we encounter a new facet, we know the next 3 vertices make up the face
                        if (pointIndex >= 3)
                        {
                            int baseIndex = pointIndex - 3; // We use the last three points
                            faces.Add($"[{baseIndex}, {baseIndex + 1}, {baseIndex + 2}]");
                        }
                    }
                }
            }

            // Join the lists into comma-separated strings
            string pointsStr = string.Join(",\n", points);
            string facesStr = string.Join(",\n", faces);

            return (pointsStr, facesStr); // Return both points and faces as a tuple
        }

        // Method to read Binary STL file content and extract points/faces
        // Method to read Binary STL file content and extract points/faces
        static (string, string) ReadBinaryStlFile(string filePath)
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            StringBuilder fileContent = new StringBuilder();

            // Read binary STL header and number of triangles
            int numTriangles = BitConverter.ToInt32(fileBytes, 80);  // Number of triangles starts at byte 80
            fileContent.AppendLine($"Number of Triangles: {numTriangles}");

            // Lists to hold points and faces for OpenSCAD
            var points = new List<string>();
            var faces = new List<string>();

            // Parse the triangles
            for (int i = 0; i < numTriangles; i++)
            {
                int offset = 84 + i * 50;  // The first 84 bytes are header and number of triangles
                byte[] triangleData = new byte[50];
                Array.Copy(fileBytes, offset, triangleData, 0, 50);

                // Extract the three vertices (each vertex is 12 bytes)
                float[] vertices = new float[9]; // 3 vertices * 3 coordinates
                for (int j = 0; j < 9; j++)
                {
                    vertices[j] = BitConverter.ToSingle(triangleData, 12 + j * 4);
                }

                // Append the vertices to the points list (OpenSCAD format)
                points.Add($"[{vertices[0]}, {vertices[1]}, {vertices[2]}]");
                points.Add($"[{vertices[3]}, {vertices[4]}, {vertices[5]}]");
                points.Add($"[{vertices[6]}, {vertices[7]}, {vertices[8]}]");

                // Append the face (triangular face with indices to the points array)
                int baseIndex = i * 3; // Each triangle uses 3 points
                faces.Add($"[{baseIndex}, {baseIndex + 1}, {baseIndex + 2}]");
            }

            // Prepare the output strings
            string pointsStr = string.Join(",\n", points);
            string facesStr = string.Join(",\n", faces);

            return (pointsStr, facesStr);
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

        // Method to generate OpenSCAD code from points and faces
        static string GenerateOpenScadCode(string filepath, string format)
        {
            // Get the points and faces (do not call ReadBinaryStlFile here)
            var (points, faces) = ReadStlFile(filepath, format);

            // Return the OpenSCAD code
            return $@"
module object1(scale) {{
    polyhedron(
        points = [
            {points}
        ],
        faces = [
            {faces}
        ]
    );
}}";
        }
    }
}
