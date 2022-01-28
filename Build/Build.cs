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
*/

using Nuke.Common.Execution;
using Nuke.WebDocu;

using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.IO.PathConstruction;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions(
	"BuildCI",
	GitHubActionsImage.WindowsLatest,
	GitHubActionsImage.UbuntuLatest,
	GitHubActionsImage.MacOsLatest, AutoGenerate = true,
	OnPushBranchesIgnore = new[] { MainBranch, ReleaseBranchPrefix + "/*" },
	OnPullRequestBranches = new[] { DevelopBranch },
	PublishArtifacts = false,
	InvokedTargets = new[] { nameof(Test), nameof(Pack) })]
public partial class Build : NukeBuild {
	private const string DevelopBranch = "develop";
	private const string FeatureBranchPrefix = "feature";
	private const string HotfixBranchPrefix = "hotfix";
	private const string MainBranch = "main";
	private const string ReleaseBranchPrefix = "release";

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[CI] private readonly GitHubActions GitHubActions;
	[Parameter] private readonly string GitHubToken;
	[GitRepository] private readonly GitRepository GitRepository;

	[Required, GitVersion(Framework = "net6.0")] private readonly GitVersion GitVersion;

	[Solution] private readonly Solution Solution;

	private string ChangelogFile => RootDirectory / "CHANGELOG.md";
	private IEnumerable<string> ChangelogSectionNotes => ExtractChangelogSectionNotes(ChangelogFile);

	//TODO - Condition for checking Original Repository
	private bool IsOriginalRepository => true;

	private AbsolutePath OutputDirectory => RootDirectory / "Output";

	private Target Pack => _ => _.DependsOn(Compile)
				.Produces(PackageDirectory / "*.nupkg")
				.Executes(() => DotNetPack(_ => _
					.SetProject(Solution)
					.SetNoBuild(InvokedTargets.Contains(Compile))
					.SetConfiguration(Configuration)
					.SetOutputDirectory(PackageDirectory)
					.SetVersion(GitVersion.NuGetVersionV2)
					.SetPackageReleaseNotes(GetNuGetReleaseNotes(ChangelogFile, GitRepository))));

	private AbsolutePath PackageDirectory => OutputDirectory / "packages";

	private AbsolutePath SourceDirectory => RootDirectory / "Source";

	public static int Main() => Execute<Build>(x => x.Compile);

	protected override void OnBuildInitialized() {
		Info($"\n\nBuilding version {GitVersion.NuGetVersion} of {Solution.Name} ({Configuration}) using version {typeof(NukeBuild).Assembly.GetName().Version} of Nuke.");
		Info($"IsLocalBuild: {Terminal.IsRunningTerminal}");
		Info($"IsRunningOn: {Environment.OSVersion.Platform switch { PlatformID.Unix => "Linux", PlatformID.MacOSX => "MacOS", _ => "Windows" }} - {Host}");
		Info($"Branch: {GitVersion.BranchName}\n\n");
	}

	/* GitRepository.Identifier == "nuke-build/nuke";*/

	private static void Info( string info ) => Logger.Info(info);

	private void ReleaseNotesCSV() {
		var gitTool = ToolResolver.GetLocalTool(ToolPathResolver.GetPathExecutable("git"));
		//var ignore = ToolResolver.GetLocalTool(gitPath)("log --pretty=format:\"- [%s](http://github.com/getkks/CSVParser/commit/%H) %cr @ %cd by %an for%d\"", RootDirectory);
		var _ = gitTool("log --pretty=format:\"%s,%H,%cr,%cd,%an,%d\"", RootDirectory);
	}
}
