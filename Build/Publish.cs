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

Author			: Karthik K Selvan

Last Modified	: Friday, 5th March 2021 10:45:32 pm
Modified By		: Karthik K Selvan

Change History:

 */

public partial class Build : NukeBuild {

	[Parameter]
	private readonly string NUGET_API_KEY;

	private static string NuGetPackageSource => "https://api.nuget.org/v3/index.json";
	private string GitHubPackageSource => $"https://nuget.pkg.github.com/{GitHubActions.GitHubRepositoryOwner}/index.json";
	private IReadOnlyCollection<AbsolutePath> PackageFiles => PackageDirectory.GlobFiles("*.nupkg");

	private Target Publish => _ => _
		 .ProceedAfterFailure()
		 .DependsOn(Clean, Test, Pack)
		 .Consumes(Pack)
		 .Requires(() => !NUGET_API_KEY.IsNullOrEmpty() || IsOriginalRepository)
		 .Requires(() => GitHasCleanWorkingCopy())
		 .Requires(() => Configuration.Equals(Configuration.Release))
		 .Requires(() => IsOriginalRepository && GitRepository.IsOnMasterBranch() ||
					 IsOriginalRepository && GitRepository.IsOnReleaseBranch() ||
						 !IsOriginalRepository && GitRepository.IsOnDevelopBranch())
		 .Executes(() => DotNetNuGetPush(_ => _
							 .SetSource(Source)
							 .SetApiKey(NUGET_API_KEY)
							 .CombineWith(PackageFiles, ( _, v ) => _.SetTargetPath(v)), degreeOfParallelism: 5, completeOnFailure: true));

	private string Source => IsOriginalRepository ? NuGetPackageSource : GitHubPackageSource;
}
