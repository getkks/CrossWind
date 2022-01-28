/*

Copyright 2021 Karthik K.Selvan, getkks@live.in

Unlicense

This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or distribute this
software, either in source code form or as a compiled binary, for any purpose,
commercial or non-commercial, and by any means.

In jurisdictions that recognize copyright laws, the author or authors of this
software dedicate any and all copyright interest in the software to the public
domain. We make this dedication for the benefit of the public at large and to
the detriment of our heirs and
successors. We intend this dedication to be an overt act of relinquishment in
perpetuity of all present and future rights to this software under copyright
law.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

For more information, please refer to <http://unlicense.org/>

Project			: Build @ z:\FSharp\CrossWind\Build

File			: Global Usings.cs @ z:\FSharp\CrossWind\Build\Global Usings.cs
File Created	: Friday, 27th August 2021 4:47:26 pm

Author			: Karthik K.Selvan

Modified By		: Karthik K.Selvan

Change History:

 */

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;

global using Nuke.Common;
global using Nuke.Common.CI;
global using Nuke.Common.CI.AzurePipelines;
global using Nuke.Common.CI.GitHubActions;
global using Nuke.Common.CI.TeamCity;
global using Nuke.Common.Git;
global using Nuke.Common.IO;
global using Nuke.Common.ProjectModel;
global using Nuke.Common.Tooling;
global using Nuke.Common.Tools.BenchmarkDotNet;
global using Nuke.Common.Tools.Coverlet;
global using Nuke.Common.Tools.DocFX;
global using Nuke.Common.Tools.DotCover;
global using Nuke.Common.Tools.DotNet;
global using Nuke.Common.Tools.GitVersion;
global using Nuke.Common.Tools.MSBuild;
global using Nuke.Common.Tools.ReSharper;
global using Nuke.Common.Tools.ReportGenerator;
global using Nuke.Common.Utilities;
global using Nuke.Common.Utilities.Collections;

global using static Nuke.Common.IO.CompressionTasks;
global using static Nuke.Common.IO.FileSystemTasks;
global using static Nuke.Common.Tools.DocFX.DocFXBuildSettingsExtensions;
global using static Nuke.Common.Tools.DotNet.DotNetTasks;
global using static Nuke.Common.Tools.Git.GitTasks;
global using static Nuke.Common.Tools.ReSharper.ReSharperTasks;
global using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
global using static Nuke.Common.ValueInjection.ValueInjectionUtility;
