###Work in Progress some features are partly built out, most Combinations are yet untested, if you just start it you will get asked while file executedfolder you want to convert and you can choose an index.###

# OpenMorph.NET

A cute project of me and my bf ChatGPT 💖

OpenMorph.NET is an open-source tool that helps bridge the gap between **raw 3D geometry** (STL files) and **editable parametric models** (OpenSCAD code). With this tool, you can convert static STL files into flexible OpenSCAD scripts, making it easier to tweak, modify, and remix 3D models with just a few simple variables.

This project aims to provide an easy way to take existing 3D models, understand their structure, and give users the ability to modify them parametrically.

## 💡 Why OpenMorph.NET?

Most 3D modeling workflows today are divided between designers who use tools like Blender (focused on artistic design) and engineers who prefer OpenSCAD (focused on parametric, code-driven design). But what if you could seamlessly convert an existing 3D model (like an STL file) into OpenSCAD code that is **modular, customizable, and easy to work with**?

That’s exactly what OpenMorph.NET aims to do!

🛠 Features
STL → Parametric OpenSCAD Conversion: Convert an STL file into an editable OpenSCAD file with parameters you can tweak.

High-Precision Polyhedron Export: Outputs detailed .scad code with minimal loss of original mesh precision.

Memory-Efficient: Handles large STL files efficiently using parallel processing and streaming techniques.

Flexible Command-Line Interface: Use flags to control output length, timeouts, and format detection.

Cross-Platform: Works on Windows, macOS, and Linux via self-contained binaries.

⚠️ Note on Shape Recognition:
While the app is structured to support shape recognition (spheres, cylinders, cones, etc.), this feature is not yet implemented in the current release.
At the moment, OpenMorph.NET acts as a very fast STL → OpenSCAD polyhedron converter, with exact facet preservation and high fidelity.

Shape recognition will be gradually introduced in future updates — intelligently identifying common primitives without any loss of detail.

## 🚀 Getting Started

### Prerequisites

To run this project from source, you need:

- **.NET 8 SDK** installed on your computer. You can download it from [here](https://dotnet.microsoft.com/download).
- A **compatible STL file** to test the conversion process.

> ✅ *Note: The tool has been tested on Windows during development. Self-contained binaries are available for other platforms in the releases.*

### Running From Source

1. Clone the repository:
    ```bash
    git clone https://github.com/Michi0403/OpenMorph.NET.git
    cd OpenMorph.NET
    ```

2. Build the application:
    ```bash
    dotnet build
    ```

3. Run with command-line options: (If you run without command-line options it asks you, if stl-files exist in the same directory, to choose one of them.)
    ```bash
    dotnet run -- \
      --filepath "your_model.stl" \
      --format ascii \
      --max-length 1000 \
      --max-rendertimeInMinutes 3 \
      -f
    ```

### Command-Line Options

| Option                     | Description                                                                 |
|----------------------------|-----------------------------------------------------------------------------|
| `--filepath`               | Path to the STL file to be processed (optional if using drag-and-drop)     |
| `-f`                       | Force processing even if warnings are encountered                          |
| `--format ascii|binary`    | Force the STL file format; auto-detected if not specified                   |
| `--max-length`             | Limit the number of characters printed from the point list (default: ∞)    |
| `--max-rendertimeInMinutes`| Maximum render time allowed (default: 0 = no timeout)                      |

You can also **drag & drop** STL files onto the compiled executable.

## 📦 Releases

Self-contained binaries are provided for the following platforms:

- ✅ Windows x64
- ✅ Windows ARM64
- ✅ Linux x64
- ✅ macOS x64
- ✅ macOS ARM (M1/M2)

> No need to install .NET to run these builds!

Download them from the [Releases](https://github.com/Michi0403/OpenMorph.NET/releases) section.

## ✨ Output Example

When successful, the tool generates an `.scad` file with:

- A `module` containing your 3D model as a polyhedron
- `Min()` and `Max()` functions for bounding box dimensions
- High-precision decimal coordinates (up to 14 digits)

## 🤖 How You Can Help

We welcome contributions from anyone interested in making 3D modeling more accessible and code-driven. Here are some areas where you can help:

- **Shape Detection**: Improve logic for recognizing complex geometries in STL files.
- **Code Optimization**: Improve performance for very large models.
- **Multi-format Support**: Add support for other file formats (e.g., OBJ).
- **Testing**: Help validate across platforms and STL sources.

### To Contribute

1. Fork this repository.
2. Create a new branch:  
   ```bash
   git checkout -b feature-name
Commit your changes:

git commit -am "Add new feature"
Push and create a PR:

git push origin feature-name
📄 License
OpenMorph.NET is licensed under the Apache License 2.0.
See the LICENSE file for details.

🌐 Where to Find Us
GitHub: https://github.com/Michi0403/OpenMorph.NET

Issues: Submit Feedback

🙌 Credits
This project was built with love and collaboration by Micha and ChatGPT 💖
Big hugs to the open-source community for making projects like this possible!