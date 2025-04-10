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
using System.Threading;
using System.Collections.Concurrent;
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
                    description: "The maximum number of characters to display from the STL file. Defaults to maximum string length."),

                new Option<int>(
                    "--max-rendertimeInMinutes",
                    getDefaultValue: () => 0,
                    description: "The maximum number of minutes to render the STL file. Default is 0. uint")
            };

            rootCommand.Description = "OpenMorph.NET - A tool to convert STL to OpenSCAD";

            rootCommand.Handler = CommandHandler.Create<string, bool, string, int, int>(async (filepath, force, format, maxLength, maxRenderTimeInMinutes) =>
            {
                try
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
                        string openScadCode = GenerateOpenScadCode(filepath, maxLength, maxRenderTimeInMinutes);

                        string scadFilePath = Path.ChangeExtension(filepath, ".scad");
                        File.WriteAllText(scadFilePath, openScadCode);
                        Console.WriteLine($"OpenSCAD code has been written to {scadFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing the file: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
             
            });

            await rootCommand.InvokeAsync(args);
        }
        // Method to read STL file content using STLdotnet with parallel processing
        // Define a Point class to store the X, Y, Z coordinates
        public class Point
        {
            public decimal X { get; }
            public decimal Y { get; }
            public decimal Z { get; }

            public Point(decimal x, decimal y, decimal z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public override string ToString()
            {
                return $"[{X.ToString("F14", CultureInfo.InvariantCulture)}, {Y.ToString("F14", CultureInfo.InvariantCulture)}, {Z.ToString("F14", CultureInfo.InvariantCulture)}]";
            }
        }

        // Method to read STL file content using STLdotnet with parallel processing
        static(List<Point> points, List<string> faces) ReadStlFileWithSTLdotnet(string filePath, int maxRenderTimeInMinutes)
        {
            try
            {
                STLDocument stlModel = STLDocument.Open(filePath);

                var allPoints = new List<Point>(stlModel.Facets.Count * 3);
                var allFaces = new List<string>(stlModel.Facets.Count);
                int pointIndex = 0;

                var localResults = new ConcurrentBag<(List<Point> points, List<string> faces)>();

                var options = new ParallelOptions();
                if (maxRenderTimeInMinutes > 0 && maxRenderTimeInMinutes < int.MaxValue)
                {
                    var tokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(maxRenderTimeInMinutes));
                    options.CancellationToken = tokenSource.Token;
                }

                Parallel.ForEach(stlModel.Facets, options, facet =>
                {
                    var localPoints = new List<Point>();
                    foreach (var vertex in facet.Vertices)
                    {
                        localPoints.Add(new Point((decimal)vertex.X, (decimal)vertex.Y, (decimal)vertex.Z));
                    }

                    int startIdx = Interlocked.Add(ref pointIndex, 3) - 3;
                    var face = $"[{startIdx}, {startIdx + 1}, {startIdx + 2}]";

                    localResults.Add((localPoints, new List<string> { face }));
                });

                // Merge all local results
                foreach (var (points, faces) in localResults.OrderBy(r => r.faces[0])) // ensures order
                {
                    allPoints.AddRange(points);
                    allFaces.AddRange(faces);
                }

                return (allPoints, allFaces);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return (new(), new());
            }
        }

        // Method to generate OpenSCAD code from points and faces, including min/max bounding box and precision adjustment
        static string GenerateOpenScadCode(string filepath,int maxLength , int maxRenderTimeInMinutes)
        {
            try
            {
                var (points, faces) = ReadStlFileWithSTLdotnet(filepath, maxRenderTimeInMinutes);

                // Initialize min and max values with the first point's coordinates
                if (points.Count == 0)
                {
                    throw new InvalidOperationException("No points found in the STL file.");
                }
                Console.WriteLine("STL File Content (First " + maxLength + " characters):");
                // Join the points into a single string
                // Check if points is null or empty
                if (points == null || points.Count == 0)
                {
                    Console.WriteLine("Error: No points were read from the STL file.");
                }

                // Now join the points into a string
                string pointsString = string.Join(", ", points.Select(p => p?.ToString() ?? "[null]"));

                // Take a substring of the points string based on maxLength
                Console.WriteLine(pointsString.Substring(0, Math.Min(maxLength, pointsString.Length)));
                // Use the first point's coordinates to initialize the min/max values
                decimal minX = points[0].X;
                decimal minY = points[0].Y;
                decimal minZ = points[0].Z;

                decimal maxX = minX;
                decimal maxY = minY;
                decimal maxZ = minZ;

                // Loop through the points and update min/max values
                foreach (var point in points)
                {
                    minX = Math.Min(minX, point.X);
                    minY = Math.Min(minY, point.Y);
                    minZ = Math.Min(minZ, point.Z);
                    maxX = Math.Max(maxX, point.X);
                    maxY = Math.Max(maxY, point.Y);
                    maxZ = Math.Max(maxZ, point.Z);
                }

                // Get the module name from the file name
                string moduleName = Path.GetFileNameWithoutExtension(filepath);

                // Create OpenSCAD code for the object with min/max functions and improved precision
                return $@"
function {moduleName}Min() = [{minX:F14}, {minY:F14}, {minZ:F14}];
function {moduleName}Max() = [{maxX:F14}, {maxY:F14}, {maxZ:F14}];

{moduleName}(1);

module {moduleName}(scale) {{
    polyhedron(
        points = [
            {string.Join(",\n", points.Select(p => p.ToString()))}
        ],
        faces = [
            {string.Join(",\n", faces)}
        ]
    );
}}";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return string.Empty;
            }
         
        }
        // Method to detect STL file format (ASCII or Binary)
        static string DetectStlFormat(string filePath)
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return string.Empty;
            }
         
        }

        // Method to offer STL files in the current directory if no filepath is provided
        static async Task<string> OfferStlFilesInDirectory()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return string.Empty;
            }
            
        }
    }
}
