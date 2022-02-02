public partial class Build : NukeBuild {
	[Partition(2)] private readonly Partition TestPartition;

	private static AbsolutePath TestsDirectory => RootDirectory / "Tests";

	private Target Test =>
		_ => _
		.DependsOn(From<ICompile>().Compile)
		.WhenSkipped(DependencyBehavior.Skip)
		.Produces(CoverageDirectory / "*.xml")
		.Produces(CoverageDirectory / "*.trx")
		.Partition(2)
		.Executes(() => {
			try {
				var _ = DotNetTest(_ => _
						.SetConfiguration(From<IHazConfiguration>().Configuration)
						.SetNoBuild(InvokedTargets.Contains(From<ICompile>().Compile))
						.ResetVerbosity()
						.SetResultsDirectory(CoverageDirectory)
						.EnableCollectCoverage()
						.SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
						.SetExcludeByFile("*.Generated.*")
						.EnableUseSourceLink()
						.CombineWith(TestProjects, ( _, v ) => _
							.SetProjectFile(v)
							.EnableCollectCoverage()
							//.SetLogger($"trx;LogFileName={v.Name}.trx")
							.SetCoverletOutput(CoverageDirectory / $"{v.Name}/cov.xml")));
			} finally {
				CoverageDirectory.GlobFiles("*.trx").ForEach(x =>
					AzurePipelines.Instance?.PublishTestResults(type: AzurePipelinesTestResultsType.VSTest,
													title: $"{Path.GetFileNameWithoutExtension(x)} ({AzurePipelines.Instance.StageDisplayName})",
													files: new string[] { x }));
			}
		});

	private IEnumerable<Project> TestProjects => from project in TestPartition.GetCurrent(From<IHazSolution>().Solution.GetProjects("*")) where project.Name.ContainsOrdinalIgnoreCase("test") select project;
}
