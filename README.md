# Vertex Animation Tool for Animated Characters



[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)


The Vertex Animation Tool is designed to simplify the creation and management of vertex animations for animated characters. This tool is ideal for developers and artists looking to efficiently animate crowds and individual characters in various environments such as games, simulations, and visualizations. The tool leverages vertex animations to deliver smooth and realistic character movements with minimal performance overhead.

## Watch the Demo Video

[![Demo Video](https://img.youtube.com/vi/NrVlNjb3XuQ/0.jpg)](https://youtu.be/UWQL-R4SXvY)


## Features
- **Bake Legacy/Generic/Humanoid Animations**: Bake Animator animations into vertex animation textures for optimized playback and support humanoid and generic animations.
- **LOD Support**: Support for Level of Detail (LOD) to maintain performance with high-quality visuals.
- **Material Management**: Automatically generate and manage materials for each animated character.
- **Flexible Animation Settings**: Customize frame rates, shadow settings, and texture baking options.
- **Integration with Unity**: Seamlessly integrates with Unity Editor for easy use.

## Installation

1. **Clone the Repository**:
    ```sh
    git clone https://github.com/SouheilHous/VertexAnimationInstancing.git
    ```

2. **Open in Unity**:
    - Open Unity and select the cloned repository as your project folder.

## Usage

### Setting Up the Tool

1. **Open the Vertex Animation Tool**:
    - Navigate to `Window` > `VertexAnimation` > `Animation Map Baker` in the Unity Editor.

2. **Select Target Object**:
    - Use the `Target GameObject` field to select the GameObject you want to animate.

3. **Configure Settings**:
    - Adjust the animation settings such as frame rate, output path, and texture baking options.
    - If the target object has LOD components, enable `Support LOD`.

4. **Bake Animations**:
    - Click the `Bake` button to generate vertex animation textures and materials.

### Example Scenes

The repository includes example scenes demonstrating various use cases:
- **Stadium Scene**: A small stadium filled with animated characters.
- **Spider-Man Meme Scene**: Multiple Spider-Man characters mimicking the iconic meme pose.
- **War Crowd**: A war Simulation large, animated running crowd.

## Contributing

Contributions are welcome! Please follow these steps to contribute:
1. **Fork the Repository**: Click on the fork button at the top right of this page.
2. **Clone Your Fork**:
    ```sh
    git clone https://github.com/yourusername/VertexAnimationInstancing.git
    ```
3. **Create a Branch**:
    ```sh
    git checkout -b feature/YourFeatureName
    ```
4. **Commit Your Changes**:
    ```sh
    git commit -m 'Add some feature'
    ```
5. **Push to the Branch**:
    ```sh
    git push origin feature/YourFeatureName
    ```
6. **Open a Pull Request**: Navigate to the original repository and open a pull request.


## Current Limitations

This package is currently not ready to bake characters with more than 16k triangles. An update is coming soon to support this feature! 


## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

This tool was inspired by various projects and contributions in the field of computer graphics and animation. Special thanks to the contributors of the [Render Crowd Of Animated Characters](https://github.com/chenjd/Render-Crowd-Of-Animated-Characters) project.

## Contact

For questions, suggestions, or issues, please open an issue on GitHub or contact the project owner:
- **Owner**: Souheil Elhoucine
- **Email**: [souhailhous@gmail.com](mailto:souhailhous@gmail.com)
