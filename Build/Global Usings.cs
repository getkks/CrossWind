global using System;
global using System.Collections.Generic;
global using System.IO;

global using Nuke.Common;
global using Nuke.Common.CI;
global using Nuke.Common.CI.GitHubActions;
global using Nuke.Common.IO;
global using Nuke.Common.ProjectModel;
global using Nuke.Common.Tooling;
global using Nuke.Common.Tools.BenchmarkDotNet;
global using Nuke.Common.Tools.DocFX;
global using Nuke.Common.Tools.DotNet;
global using Nuke.Common.Tools.GitVersion;
global using Nuke.Common.Tools.ReportGenerator;
global using Nuke.Common.Tools.ReSharper;
global using Nuke.Components;

global using static Nuke.Common.Tools.DocFX.DocFXBuildSettingsExtensions;
