using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotCover;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.InspectCode;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.ChangeLog.ChangelogTasks;
using static Nuke.Common.ControlFlow;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Git.GitTasks;
using static Nuke.Common.Tools.InspectCode.InspectCodeTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
[GitHubActions(
	"continuous",
	GitHubActionsImage.MacOs1014,
	GitHubActionsImage.Ubuntu1604,
	GitHubActionsImage.Ubuntu1804,
	GitHubActionsImage.WindowsServer2016R2,
	GitHubActionsImage.WindowsServer2019,
	On = new[] { GitHubActionsTrigger.Push },
	InvokedTargets = new[] { nameof(Compile) },
	ImportGitHubTokenAs = nameof(GitHubToken))]
class Build : NukeBuild
{

	public static int Main() => Execute<Build>(x => x.Compile);

	[Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
	readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

	[Required] [GitRepository] readonly GitRepository GitRepository;
	[Required] [Solution] readonly Solution Solution;
	[Required] [GitVersion(Framework = "netcoreapp3.1", NoFetch = true)] readonly GitVersion GitVersion;
	[Parameter] readonly string GitHubToken;

	AbsolutePath SourceDirectory => RootDirectory / "Source";
	AbsolutePath TestsDirectory => RootDirectory / "Tests";
	AbsolutePath OutputDirectory => RootDirectory / "Output";

	[Parameter] readonly bool IgnoreFailedSources;

	const string MasterBranch = "master";
	const string DevelopBranch = "development";
	const string ReleaseBranchPrefix = "release";
	const string HotfixBranchPrefix = "hotfix";

	Target Clean => _ => _
		.Executes(() =>
		{
			//DotNetClean();
			SourceDirectory.GlobDirectories("**/bin", "**/obj")
				  .ForEach(DeleteDirectory);
			TestsDirectory.GlobDirectories("**/bin", "**/obj")
				 .ForEach(DeleteDirectory);
			EnsureCleanDirectory(OutputDirectory);
		});

	Target Restore => _ => _
		.DependsOn(Clean)
		.Executes(() =>
		{
			DotNetRestore(s => s
				.SetProjectFile(Solution)
				.SetIgnoreFailedSources(IgnoreFailedSources));
		});

	Target Compile => _ => _
		.DependsOn(Restore)
		.Executes(() =>
		{
			DotNetBuild(s => s
				.SetProjectFile(Solution)
				.SetConfiguration(Configuration)
				.SetAssemblyVersion(GitVersion.AssemblySemVer)
				.SetFileVersion(GitVersion.AssemblySemFileVer)
				.SetInformationalVersion(GitVersion.InformationalVersion)
				.EnableNoRestore());
		});

}
