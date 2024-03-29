global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;

global using Nuke.Common;
global using Nuke.Common.CI;
global using Nuke.Common.CI.AzurePipelines;
global using Nuke.Common.CI.GitHubActions;
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
global using Nuke.Common.Tools.ReportGenerator;
global using Nuke.Common.Tools.ReSharper;
global using Nuke.Common.Utilities;
global using Nuke.Common.Utilities.Collections;
global using Nuke.Components;

global using static Nuke.Common.IO.CompressionTasks;
global using static Nuke.Common.IO.FileSystemTasks;
global using static Nuke.Common.Tools.DocFX.DocFXBuildSettingsExtensions;
global using static Nuke.Common.Tools.DotNet.DotNetTasks;
global using static Nuke.Common.Tools.Git.GitTasks;
global using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
