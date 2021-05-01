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

File			: Publish.cs @ d:\Development\FSharp\CSVParser\Build\Publish.cs
File Created	: Friday, 5th March 2021 7:15:21 pm

Author			: Karthik K Selvan (getkks@live.in)

Last Modified	: Friday, 5th March 2021 10:45:32 pm
Modified By		: Karthik K Selvan (getkks@live.in>)

Change History:

 */

using System.Collections.Generic;

using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotCover;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.ReSharper;
using Nuke.Common.Utilities;

using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;

public partial class Build : NukeBuild
{
	#region Private Fields

	[Parameter]
	private string NUGET_API_KEY ;

	#endregion Private Fields

	#region Private Properties

	private string GitHubPackageSource => $"https://nuget.pkg.github.com/{GitHubActions.GitHubRepositoryOwner}/index.json";
	private string NuGetPackageSource => "https://api.nuget.org/v3/index.json";
	private IReadOnlyCollection<AbsolutePath> PackageFiles => PackageDirectory.GlobFiles("*.nupkg");

	private Target Publish => _ => _
		   .ProceedAfterFailure()
		   .DependsOn(Clean, Test, Pack)
		   .Consumes(Pack)
		   .Requires(() => !NUGET_API_KEY.IsNullOrEmpty() || !IsOriginalRepository)
		   .Requires(() => GitHasCleanWorkingCopy())
		   .Requires(() => Configuration.Equals(Configuration.Release))
		   .Requires(() => (IsOriginalRepository && GitRepository.IsOnMasterBranch()) ||
						  (IsOriginalRepository && GitRepository.IsOnReleaseBranch()) ||
						   (!IsOriginalRepository && GitRepository.IsOnDevelopBranch()))
		   .Executes(() => DotNetNuGetPush(_ => _
							   .SetSource(Source)
							   .SetApiKey(NUGET_API_KEY)
							   .CombineWith(PackageFiles, (_, v) => _.SetTargetPath(v)), degreeOfParallelism: 5, completeOnFailure: true));

	private string Source => IsOriginalRepository ? NuGetPackageSource : GitHubPackageSource;

	#endregion Private Properties
}
