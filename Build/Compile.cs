/*

Copyright 2021 Karthik K Selvan, getkks@live.in

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

Project			: Build @ d:\Development\FSharp\CSVParser\Build

File			: Clean.cs @ d:\Development\FSharp\CSVParser\Build\Clean.cs
File Created	: Friday, 5th March 2021 6:24:48 pm

Author			: Karthik K Selvan (getkks@live.in)

Last Modified	: Saturday, 6th March 2021 9:27:40 am
Modified By		: Karthik K Selvan (getkks@live.in>)

Change History:

 */

using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotCover;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities.Collections;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;

public partial class Build : NukeBuild
{
	#region Private Properties

	private Target Clean => _ => _
		 .Before(Restore)
		 .Executes(() =>
		 {
			 SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
			 TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
			 EnsureCleanDirectory(OutputDirectory);
		 });

	private Target Compile => _ => _
		 .DependsOn(Restore)
		 .WhenSkipped(DependencyBehavior.Skip)
		 .Executes(() => DotNetBuild(_ => _
		 	.SetNoRestore(InvokedTargets.Contains(Restore))
			//.SetProjectFile(Solution)
			.CombineWith(SourceProjects, (_, v) => _
				.SetProjectFile(v)
				.SetConfiguration(Configuration)
				.SetRepositoryUrl(GitRepository.HttpsUrl)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion))));

	[Parameter("Ignore unreachable sources during " + nameof(Restore))]
	private bool IgnoreFailedSources => TryGetValue<bool?>(() => IgnoreFailedSources) ?? false;

	private Target Restore => _ => _.Executes(() => DotNetRestore(s => s.SetProjectFile(Solution).SetIgnoreFailedSources(IgnoreFailedSources)));
	private IEnumerable<Project> SourceProjects => Solution.GetProjects("*").Where(x => !(x.Name.Contains("build", StringComparison.InvariantCultureIgnoreCase) || x.Name.Contains("test", StringComparison.InvariantCultureIgnoreCase)));

	#endregion Private Properties
}
