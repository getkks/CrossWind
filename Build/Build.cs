using Nuke.Common.Execution;

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
	InvokedTargets = new[] { nameof(ITest.Test) })]
public sealed partial class Build : NukeBuild, IReportCoverage, IHazArtifacts, IHazChangelog, IHazGitVersion, IHazSolution, IPublish {
	private const string DevelopBranch = "develop";
	private const string FeatureBranchPrefix = "feature";
	private const string HotfixBranchPrefix = "hotfix";
	private const string MainBranch = "main";
	private const string ReleaseBranchPrefix = "release";

	[CI] private readonly GitHubActions GitHubActions;
	[Parameter] private readonly string GitHubToken;

	//[Solution(GenerateProjects = true)] private readonly Solution Solution;
	public bool CreateCoverageHtmlReport => true;

	public Configure<ReportGeneratorSettings> ReportGeneratorSettings =>
		_ => _
		.SetFramework("net6.0")
		.SetReportTypes(ReportTypes.HtmlInline, ReportTypes.Badges)
		.SetHistoryDirectory(Path.Combine(From<IReportCoverage>().CoverageReportDirectory, "history"));

	public bool ReportToCodecov => false;
	//Nuke.Common.ProjectModel.Solution IHazSolution.Solution => Solution;

	Target ITest.Test =>
		 _ => _
			.Inherit<ITest>()
			.Partition(2);

	IEnumerable<Project> ITest.TestProjects => Partition.GetCurrent(From<IHazSolution>().Solution.GetProjects("*.Tests"));

	public static int Main() => Execute<Build>(x => ( (ICompile) x ).Compile);

	protected override void OnBuildInitialized() {
		Info(Build.IsLocalBuild ? "LocalBuild" : "CI Build");
		Info($"RunningOn {Environment.OSVersion.Platform switch { PlatformID.Unix => "Linux", PlatformID.MacOSX => "MacOS", _ => "Windows" }} - {Host}");
		Info($"Branch: {From<IHazGitVersion>().Versioning.BranchName}\n\n");
	}

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
