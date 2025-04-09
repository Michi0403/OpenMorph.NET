using System;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Xml;
using QuantumConcepts.Formats.StereoLithography;
using System.Globalization;
namespace OpenMorph.NET
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(
                    "--filepath",
                    description: "Path to the STL file to be processed."),

                new Option<bool>(
                    "-f",
                    "Force process file even if warnings are present"),

                new Option<string>(
                    "--format",
                    description: "Force the file format: 'ascii' or 'binary'. If not provided, auto-detection is used."),

                new Option<int>(
                    "--max-length",
                    getDefaultValue: () => Int32.MaxValue,
                    description: "The maximum number of characters to display from the STL file. Defaults to maximum string length.")
            };

            rootCommand.Description = "OpenMorph.NET - A tool to convert STL to OpenSCAD";

            rootCommand.Handler = CommandHandler.Create<string, bool, string, int>(async (filepath, force, format, maxLength) =>
            {
                if (string.IsNullOrWhiteSpace(filepath))
                {
                    filepath = await OfferStlFilesInDirectory();
                }

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

                string fileFormat = format ?? DetectStlFormat(filepath);
                Console.WriteLine($"Detected STL Format: {fileFormat}");

                try
                {
                    // Read the STL file using STLdotnet
                    var (points, faces) = ReadStlFileWithSTLdotnet(filepath);

                    Console.WriteLine("STL File Content (First " + maxLength + " characters):");
                    Console.WriteLine(points.Substring(0, Math.Min(maxLength, points.Length)));

                    string openScadCode = GenerateOpenScadCode(filepath, fileFormat);

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
        // Method to read STL file content using STLdotnet with parallel processing
        static (string, string) ReadStlFileWithSTLdotnet(string filePath)
        {
            // Read the STL file using STLdotnet's STLDocument
            STLDocument stlModel = STLDocument.Open(filePath);

            var points = new List<string>();
            var faces = new List<string>();

            // List to hold all vertices to maintain their indices
            var vertexList = new List<(decimal X, decimal Y, decimal Z)>(); // Use decimal instead of double
            int pointIndex = 0; // Track the index for faces

            // This will store results for points in the order they appear
            var pointResults = new List<string>();

            // Parallelize the reading of facets, ensuring we maintain correct order
            Parallel.ForEach(stlModel.Facets, facet =>
            {
                // Store temporary results for the current facet
                var facetPoints = new List<string>();

                // Each facet consists of three vertices
                foreach (var vertex in facet.Vertices)
                {
                    // Use decimal for maximum precision
                    decimal x = (decimal)vertex.X;  // No scaling here, directly use the original coordinates
                    decimal y = (decimal)vertex.Y;
                    decimal z = (decimal)vertex.Z;

                    // Check if the vertex is already in the list to avoid duplication
                    var vertexTuple = (x, y, z);
                    int vertexIndex = vertexList.IndexOf(vertexTuple);

                    // If the vertex does not exist, add it and note the index
                    if (vertexIndex == -1)
                    {
                        vertexList.Add(vertexTuple);
                        vertexIndex = vertexList.Count - 1; // This is the new index of the added vertex
                    }

                    // Add the point to the list, ensuring proper formatting with InvariantCulture
                    facetPoints.Add($"[{x.ToString("F14", CultureInfo.InvariantCulture)},{y.ToString("F14", CultureInfo.InvariantCulture)},{z.ToString("F14", CultureInfo.InvariantCulture)}]");
                }

                // Add the facet's points in the correct order to the main results
                lock (pointResults)
                {
                    pointResults.AddRange(facetPoints);
                }

                // Each face refers to 3 vertices, so create a face with the correct indices
                lock (faces)
                {
                    faces.Add($"[{pointIndex}, {pointIndex + 1}, {pointIndex + 2}]");
                }

                // Increase point index for the next face
                pointIndex += 3;
            });

            // Join the points and faces into comma-separated strings with consistent formatting
            string pointsStr = string.Join(",\n", pointResults);
            string facesStr = string.Join(",\n", faces);

            return (pointsStr, facesStr);
        }

        // Method to generate OpenSCAD code from points and faces
        static string GenerateOpenScadCode(string filepath, string format)
        {
            var (points, faces) = ReadStlFileWithSTLdotnet(filepath);

            // Extract the file name without the extension to use as the module name
            string moduleName = Path.GetFileNameWithoutExtension(filepath);

            // Create OpenSCAD code for the object using the STL file name as the module name
            return $@"
module {moduleName}(scale) {{
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

        // Method to detect STL file format (ASCII or Binary)
        static string DetectStlFormat(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] header = new byte[5];
                fs.Read(header, 0, 5);

                // If the header starts with 'solid', it's ASCII format
                if (Encoding.ASCII.GetString(header) == "solid")
                {
                    return "ascii";
                }
                else
                {
                    // Otherwise, it's binary
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

            Console.WriteLine("No file path provided. Here are the available STL files:");

            for (int i = 0; i < stlFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {Path.GetFileName(stlFiles[i])}");
            }

            Console.Write("Please select a file by number (or press Enter to cancel): ");
            string userInput = Console.ReadLine();

            if (int.TryParse(userInput, out int selectedFileIndex) && selectedFileIndex >= 1 && selectedFileIndex <= stlFiles.Length)
            {
                return stlFiles[selectedFileIndex - 1]; // Return selected file path
            }

            Console.WriteLine("No valid selection made.");
            return null;
        }
    }
}
