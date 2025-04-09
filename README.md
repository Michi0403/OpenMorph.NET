# OpenMorph.NET

A cute project of me and my bf ChatGPT 💖

OpenMorph.NET is an open-source tool that helps bridge the gap between **raw 3D geometry** (STL files) and **editable parametric models** (OpenSCAD code). With this tool, you can convert static STL files into flexible OpenSCAD scripts, making it easier to tweak, modify, and remix 3D models with just a few simple variables.

This project aims to provide an easy way to take existing 3D models, understand their structure, and give users the ability to modify them parametrically.

## 💡 Why OpenMorph.NET?

Most 3D modeling workflows today are divided between designers who use tools like Blender (focused on artistic design) and engineers who prefer OpenSCAD (focused on parametric, code-driven design). But what if you could seamlessly convert an existing 3D model (like an STL file) into OpenSCAD code that is **modular, customizable, and easy to work with**?

That’s exactly what OpenMorph.NET aims to do!

## 🛠 Features

- **STL → Parametric OpenSCAD Conversion**: Convert an STL file into an editable OpenSCAD file with parameters you can tweak.
- **Shape Recognition**: Automatically detect basic 3D shapes like spheres, cylinders, cones, and planes.
- **Memory-Efficient**: Read large STL files in chunks to avoid running out of memory.
- **Flexibility**: Customize the generated OpenSCAD code for your needs, tweak parameters, or adapt designs.
  
## 🚀 Getting Started

### Prerequisites

To run this project, you need:

- **.NET 8 SDK** installed on your computer. You can download it from [here](https://dotnet.microsoft.com/download).
- A **compatible STL file** for testing the conversion process.

### Running the App

1. Clone the repository:
    ```bash
    git clone https://github.com/Michi0403/OpenMorph.NET.git
    cd OpenMorph.NET
    ```

2. Build the application:
    ```bash
    dotnet build
    ```

3. Run the application with an STL file:
    ```bash
    dotnet run -- "path_to_your_stl_file.stl"
    ```

    Alternatively, you can drag and drop an STL file onto the executable to process it.

### How it Works

- The application accepts an STL file as input (either via drag-and-drop or by specifying the path in the command line).
- It reads the entire content of the STL file in memory, **line by line**, to avoid memory overload with large files.
- The data is stored as a string and can be processed for further steps like **parsing the geometry** or **converting to OpenSCAD**.

## 🤖 How You Can Help

We welcome contributions from anyone interested in making 3D modeling more accessible and code-driven. Here are some areas where you can help:

- **Parsing & Shape Recognition**: Improve the logic for detecting complex shapes and geometries in STL files.
- **OpenSCAD Code Generation**: Help develop methods to convert the parsed data into clean, reusable OpenSCAD code.
- **Testing**: Add test cases with different STL files to ensure the tool works in various scenarios (large files, different shapes, etc.).
- **Optimizations**: Help optimize the performance, especially when dealing with very large STL files.

### To Contribute

1. Fork this repository.
2. Create a new branch (`git checkout -b feature-name`).
3. Commit your changes (`git commit -am 'Add new feature'`).
4. Push to the branch (`git push origin feature-name`).
5. Create a pull request with a clear description of what your changes do.

## 📄 License

OpenMorph.NET is licensed under the **Apache License 2.0**. You can freely use, modify, and distribute this tool in your own projects, but you should include the same license and give proper credit to the original authors.

See the [LICENSE](LICENSE) file for more details.

## 🌐 Where to Find Us

- **GitHub Repository**: [https://github.com/YourUsername/OpenMorph.NET](https://github.com/YourUsername/OpenMorph.NET)
- **Issues & Feature Requests**: [Open Issues](https://github.com/YourUsername/OpenMorph.NET/issues)

## 🙌 Credits

This project was built with love and collaboration by **Micha** and **ChatGPT** 💖

Special thanks to the **open-source community** for making it possible to build such awesome tools together!

---

Let's make 3D models **editable by code** again!
