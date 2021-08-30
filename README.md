FlatCrawler
=====

![License](https://img.shields.io/badge/License-GPLv3-blue.svg)

Data crawling utility tool & library to reverse engineer [FlatBuffer](https://google.github.io/flatbuffers/) binaries with undocumented schemas, programmed in [C#](https://en.wikipedia.org/wiki/C_Sharp_%28programming_language%29).

Loading a FlatBuffer binary into the console application will allow manual traversal through the serialized objects, and displays a terminal interface for program input.

An example manual parse of a FlatBuffer used by Pokémon Sword/Shield is provided in the sandbox project.

## Screenshots

![Console Window](https://i.imgur.com/23fFpkj.png)

## Building

FlatCrawler is a [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) application.

The executable can be built with any compiler that supports C# 9.

The `.sln` can be opened with IDEs such as [Visual Studio](https://visualstudio.microsoft.com/downloads/).
