using System.Text;

namespace OpenMorph.NET
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Check if a file path is provided
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide an STL file path.");
                return;
            }

            string filePath = args[0];

            // Check if file exists
            if (!File.Exists(filePath))
            {
                Console.WriteLine("The specified file does not exist.");
                return;
            }

            // Read the STL file content
            string stlContent = ReadStlFile(filePath);

            // Output the first 500 characters as a sample (for large files)
            Console.WriteLine("STL File Content (First 500 characters):");
            Console.WriteLine(stlContent.Substring(0, Math.Min(500, stlContent.Length)));
        }

        // Method to read STL file content
        static string ReadStlFile(string filePath)
        {
            // Initialize a StringBuilder to store the STL content
            StringBuilder fileContent = new StringBuilder();

            // Read the file in chunks to avoid memory issues with large files
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
    }
}
