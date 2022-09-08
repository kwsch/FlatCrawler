FlatCrawler
=====

![License](https://img.shields.io/badge/License-GPLv3-blue.svg)

Data crawling utility tool & library to reverse engineer [FlatBuffer](https://google.github.io/flatbuffers/) binaries with undocumented schemas, programmed in [C#](https://en.wikipedia.org/wiki/C_Sharp_%28programming_language%29).

Loading a FlatBuffer binary into the console application will allow manual traversal through the serialized objects, and displays a terminal interface for program input.

An example manual parse of a FlatBuffer used by Pokémon Sword/Shield is provided in the sandbox project.

## Screenshots

![Console Window](https://i.imgur.com/23fFpkj.png)

## Instructions

Basic commands:
| Command | Alias(es) | Description | Usage |
| :-----: | :-------: | ----------- | ----- |
| `load` | - | Load a file | - |
| `tree` | - | Print the node tree | - |
| `clear` | - | Clear the console | - |
| `path` | - | ??? | - |
| `quit` | - | Quit | - |
| `print` | - | ??? | `info`, `p` |
| `hex` | - | ??? | `h` |
| `dump` | - | Save the state of this session to a file | - |


Crawler commands
| Command | Alias(es) | Description | Usage |
| :----: | :----: | ------------- | ------------- |
| `root` | - | Navigate to the root of the tree | - |
| `up` | - | Navigate one node up the tree | - |
| `ro` | - | Get reference offset | - |
| `fo` | - | Get field offset | - |
| `eo` | - | Get entry offset | - |
| `rf` | - | Set field type | `rf` + offset + type |
| `rf` | - | Read field | `rf` + offset |
| `fowf` | - | ??? | - |
| `fewf` | - | ??? | - |
| `fewfs` | - | ??? | - |
| `of` | - | ??? | - |
| `hex` | - | ??? | - |
| `analyze` | `union`, `au` | ??? | - |
| `oof` | - | ??? | - |
| `oofd` | - | ??? | - |
| `mfc` | - | ??? | - |


| Types | Alias(es) | Description |
| :----: | :----: | ------------- |
| `bool` | - | Boolean |
| `sbyte` | `s8` | Signed Byte/Int8 |
| `short` | `s16` | Int16 |
| `int` | `s32` | Int32 |
| `long` | `s64` | Int64 |
| `byte` | `i8` | Byte/UInt8 |
| `ushort` | `i16` | UInt16 |
| `uint` | `i32` | UInt32 |
| `ulong` | `i64` | UInt64 |
| `float` | - | Float |
| `double` | - | Double |
| `string` | `str` | String |
| `object` | - | Object |
| `table` | `object[]` | Object array |
| `string[]` | - | String Array |

All primitive types can be used in array form (`type + []`).

## Building

FlatCrawler is a [.NET 5.0](https://dotnet.microsoft.com/download/dotnet/5.0) application.

The executable can be built with any compiler that supports C# 9.

The `.sln` can be opened with IDEs such as [Visual Studio](https://visualstudio.microsoft.com/downloads/).
