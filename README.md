<br />
<div align="center">
  <h3 align="center">UnityPeekPlugin</h3>

  <p align="center">
    The <a href="https://github.com/BepInEx/BepInEx">BepInEx</a> plugin part of <a href="https://github.com/CookieLynx/UnityPeekPlugin">UnityPeek</a>
    <br />
    <a href="https://github.com/CookieLynx/UnityPeek/releases">Releases</a>
    ·
    <a href="https://github.com/CookieLynx/UnityPeek/issues">Report Bug</a>
  </p>
</div>


## About

Please see <a href="https://github.com/CookieLynx/UnityPeekPlugin">UnityPeek's</a> page for details, This repository is just for the plugin that does nothing on its own



# Installing

## Both this dll and the UnityPeek.exe are required
- Install <a href=https://github.com/BepInEx/BepInEx/releases/tag/v6.0.0-pre.2>Releases 6.0.0</a> onto the Unity Game 
- Download the .dll from <a href="https://github.com/CookieLynx/UnityPeek/releases">Releases</a> and put it into the Unity game's BepInEx/Plugins folder
- Follow steps for downloading and installing <a href="https://github.com/CookieLynx/UnityPeek/releases">UnityPeek</a> on its github page



# Development

## Prerequisites

- **Visual Studio 2022** or later with the following workloads installed:
  - **.NET Desktop Development**
  - **Desktop development with C++** (required for certain dependencies)
- **.NET 8.0 SDK**: Ensure the .NET 8.0 SDK is installed. You can download it from the official [.NET download page](https://dotnet.microsoft.com/download/dotnet/8.0).

## Getting Started

### 1. Clone the Repository

Open a terminal or command prompt and run:

```bash
git clone https://github.com/CookieLynx/UnityPeekPlugin.git
```

### 2. Open the Solution in Visual Studio

- Navigate to the cloned repository directory.

- Open UnityPeekPlugin.sln in Visual Studio by double-clicking the file.

### 4. Build the Solution

- Set the build configuration to Release:
  - In the toolbar, locate the build configuration dropdown (usually set to Debug by default) and select Release.
- Build the solution:
  - Go to the menu bar and select Build > Build Solution (or press Ctrl+Shift+B).

### 5. Locate the Executable

- After a successful build, the executable (UnityPeekPlugin.dll) will be located in:

`
<RepositoryRoot>\bin\Release\net8.0-windows
`
