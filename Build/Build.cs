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
	OnPullRequestBranches = new[] { DevelopBranch, FeatureBranchPrefix, HotfixBranchPrefix },
	PublishArtifacts = false,
	InvokedTargets = new[] { nameof(ITest.Test), nameof(IPack.Pack) })]
public partial class Build : NukeBuild, IReportCoverage, IHazArtifacts, IHazChangelog, IHazGitVersion, IHazSolution, IPack {
	private const string DevelopBranch = "develop";
	private const string FeatureBranchPrefix = "feature";
	private const string HotfixBranchPrefix = "hotfix";
	private const string MainBranch = "main";
	private const string ReleaseBranchPrefix = "release";

	[CI] private readonly GitHubActions GitHubActions;
	[Parameter] private readonly string GitHubToken;

	[Solution(GenerateProjects = true)] private readonly Solution Solution;
	public bool CreateCoverageHtmlReport => true;

	public Configure<ReportGeneratorSettings> ReportGeneratorSettings =>
		_ => _
		.SetFramework("net6.0")
		.SetReportTypes(ReportTypes.HtmlInline, ReportTypes.Badges)
		.SetHistoryDirectory(Path.Combine(From<IReportCoverage>().CoverageReportDirectory, "history"));

	public bool ReportToCodecov => false;
	Nuke.Common.ProjectModel.Solution IHazSolution.Solution => Solution;

	Target ITest.Test =>
		 _ => _
			.Inherit<ITest>()
			.Partition(2);

	IEnumerable<Project> ITest.TestProjects => Partition.GetCurrent(Solution.GetProjects("*.Tests"));

	//TODO - Condition for checking Original Repository
	private bool IsOriginalRepository => true;

	private AbsolutePath OutputDirectory => RootDirectory / "Output";

	private Target Pack1 =>
		_ => _
		.DependsOn(From<ICompile>().Compile).Produces(PackageDirectory / "*.nupkg")
		.Executes(() =>
			DotNetPack(_ => _
				.SetProject(From<IHazSolution>().Solution)
				.SetNoBuild(InvokedTargets.Contains(From<ICompile>().Compile))
				.SetConfiguration(From<IHazConfiguration>().Configuration)
				.SetOutputDirectory(PackageDirectory)
				.SetVersion(From<IHazGitVersion>().Versioning.NuGetVersionV2)
				.SetPackageReleaseNotes(GetNuGetReleaseNotes(ChangelogFile, From<IHazGitRepository>().GitRepository))));

	private AbsolutePath PackageDirectory => OutputDirectory / "packages";
	private AbsolutePath SourceDirectory => RootDirectory / "Source";

	public static int Main() => Execute<Build>(x => ( (ICompile) x ).Compile);

	protected override void OnBuildInitialized() {
		//Info($"\n\nBuilding version {From<IHazGitVersion>().Versioning.NuGetVersion} of {From<IHazSolution>().Solution.Name} ({From<IHazConfiguration>().Configuration}) using version {typeof(NukeBuild).Assembly.GetName().Version} of Nuke.");
		Info(Terminal.IsRunningTerminal ? "LocalBuild" : "CI Build");
		Info($"RunningOn {Environment.OSVersion.Platform switch { PlatformID.Unix => "Linux", PlatformID.MacOSX => "MacOS", _ => "Windows" }} - {Host}");
		Info($"Branch: {From<IHazGitVersion>().Versioning.BranchName}\n\n");
	}

	/* GitRepository.Identifier == "nuke-build/nuke";*/

	private static void Info( string info ) => Serilog.Log.Information(info);

	private T From<T>()
	   where T : INukeBuild
	   => (T) (object) this;

	private void ReleaseNotesCSV() {
		var gitTool = ToolResolver.GetLocalTool(ToolPathResolver.GetPathExecutable("git"));
		//var ignore = ToolResolver.GetLocalTool(gitPath)("log --pretty=format:\"- [%s](http://github.com/getkks/CSVParser/commit/%H) %cr @ %cd by %an for%d\"", RootDirectory);
		var _ = gitTool("log --pretty=format:\"%s,%H,%cr,%cd,%an,%d\"", RootDirectory);
	}
}
