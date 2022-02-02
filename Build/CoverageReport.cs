public partial class Build : NukeBuild {
	private AbsolutePath CoverageDirectory => RootDirectory / "Coverage";

	private Target CoverageReport => _ => _
		  .DependsOn(Test)
		  .TriggeredBy(Test)
		  .Consumes(Test)
		  .Produces(CoverageReportArchive)
		  .Executes(() => {
			  var _ = ReportGenerator(_ => _.CombineWith(TestProjects, ( _, v ) => _.SetReports(CoverageDirectory / $"{v.Name}/*.xml")
				  .SetReportTypes(ReportTypes.HtmlInline, ReportTypes.Badges)
				  .SetTargetDirectory(CoverageReportDirectory)
				  .SetHistoryDirectory(CoverageReportDirectory / "history")
				  .SetFramework("net6.0")));

			  CompressZip(
				  directory: CoverageReportDirectory,
				  archiveFile: CoverageReportArchive,
				  fileMode: FileMode.Create);

			  CoverageDirectory.GlobFiles("*.xml").ForEach(x =>
				 AzurePipelines.Instance?.PublishCodeCoverage(
					 AzurePipelinesCodeCoverageToolType.Cobertura,
					 x,
					 CoverageReportDirectory));
		  });

	private AbsolutePath CoverageReportArchive => RootDirectory / "CoverageReport.zip";

	private AbsolutePath CoverageReportDirectory => RootDirectory / "CoverageReport";
}
