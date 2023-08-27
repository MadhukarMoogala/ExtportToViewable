# ExtportToViewable

![Platforms](https://img.shields.io/badge/platform-Windows|MacOS-lightgray.svg)
![.NET](https://img.shields.io/badge/.NET6.0-blue.svg)
[![License](http://img.shields.io/:license-MIT-blue.svg)](http://opensource.org/licenses/MIT)

[![oAuth2](https://img.shields.io/badge/oAuth2-v2-green.svg)](http://developer.autodesk.com/)
[![Data-Management](https://img.shields.io/badge/Data%20Management-v1-green.svg)](http://developer.autodesk.com/)
[![Design-Automation](https://img.shields.io/badge/Design%20Automation-v3-green.svg)](http://developer.autodesk.com/)

![Advanced](https://img.shields.io/badge/Level-Basic-blue.svg)

# Description

This project uses APS Design Automation to export a drawing custom properties to SVF viewable

# Demo

![](https://github.com/MadhukarMoogala/ExtportToViewable/blob/main/Media/demo.gif)

# Setup

## Prerequisites

1. **APS Account**: Learn how to create a APS Account, activate subscription and create an app at [this tutorial](https://tutorials.autodesk.io/#create-an-account). 
2. **Visual Studio**: Either Community (Windows) or Code (Windows, MacOS).
3. **.NET Core** basic knowledge with C#
4. AutoCAD API knowledge with C#
5. **Electron.js** basic knowledge with Node.js

## Project Stucture

There  3 projects, you need to setup them seperately as each project using a different technology.

```bash
git clone https://github.com/MadhukarMoogala/ExtportToViewable.git
cd ExtportToViewable
devenv ExtportToViewable.sln
```

Now, the solution file is opened in Visual Studio, let's understand each project and their role.

### Plugin

    This is a .NET 4.8 AutoCAD plugin to export the XData to SVF bubble package and other properties also are hooked to  Dwg extraction complex.

- Find `LMVExtractor` in the solution explorer, right click it and set it as the Startup Project.

- Set project configuration to Debug and x64.

- Rebuild, restores all nuget packages.

### DAClient

    This is a .NET 6 console application to orchestrate Design Automation pipeline.

- Find `DAClient` in the solution explorer, right click it and set it as the Startup Project.

- Right-click on the project, then `Add` ---> `New Item`, select `JSON` file and name it `appsettings.user.json` , this is a user settings files required by Design Automation sdk to make authentication and run various API requests.
  
  ```json
  {
    "Forge": {
      "ClientId": "your APS_CLIENT_ID",
      "ClientSecret": "your APS_CLIENT_SECRET"
    }
  }
  ```

- Set project configuration to Debug and x64.

- Rebuild, restores all nuget packages.

 To build both projects together,

```bash
cd ExtportToViewable
dotnet build
```

#### Build Output

```
MSBuild version 17.7.1+971bf70db for .NET
  Determining projects to restore...
  All projects are up-to-date for restore.
  LMVExtractor -> D:\OneDrive - Autodesk\APITeam\DevCon2023\Samples\ExtportToViewable\Plugin\bin\x64\Debug\TestLMVExtractor.dll
  DAClient -> D:\OneDrive - Autodesk\APITeam\DevCon2023\Samples\ExtportToViewable\DAClient\bin\x64\Debug\net6.0-windows\DAClient.dll

  7-Zip 19.00 (x64) : Copyright (c) 1999-2018 Igor Pavlov : 2019-02-21

  Open archive: D:\OneDrive - Autodesk\APITeam\DevCon2023\Samples\ExtportToViewable\Bundles\LMVExporter.bundle.zip
  --
  Path = D:\OneDrive - Autodesk\APITeam\DevCon2023\Samples\ExtportToViewable\Bundles\LMVExporter.bundle.zip
  Type = zip
  Physical Size = 4622

  Scanning the drive:
  2 folders, 2 files, 8301 bytes (9 KiB)

  Updating archive: D:\OneDrive - Autodesk\APITeam\DevCon2023\Samples\ExtportToViewable\Bundles\LMVExporter.bundle.zip

  Add new data to archive: 2 folders, 2 files, 8301 bytes (9 KiB)


  Files read from disk: 2
  Archive size: 4622 bytes (5 KiB)
  Everything is Ok

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.13
```

### SvfViewer

    This is an electron application project to view the SVF package generated from the Design Automation pipeline.

```
cd SvfViewer
npm install
npm start
```

# Further Reading

Documentation:

- [Design Automation v3](https://aps.autodesk.com/en/docs/design-automation/v3/developers_guide/overview/)
- [Data Management](https://aps.autodesk.com/en/docs/data/v2/reference/http/) used to store input and output files.

Other APIs:

- [[**Electron**](https://www.electronjs.org/)](https://www.electronjs.org/)

### Tips & Tricks

This sample uses .NET 6 and works fine on both Windows and MacOS, see [this tutorial for MacOS](https://github.com/augustogoncalves/dotnetcoreheroku). You still need Windows debug the AppBundle plugins.

### Troubleshooting

1. **error setting certificate verify locations** error: may happen on Windows, use the following: `git config --global http.sslverify "false"`

## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT), refer [License](https://github.com/MadhukarMoogala/ExtportToViewable/blob/main/LICENSE) page for further details.

## Written by

Madhukar Moogala  [@galakar](https://twitter.com/galakar), [APS Partner Development](http://aps.autodesk.com)
