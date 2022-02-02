public partial class Build : NukeBuild {

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
	private AbsolutePath InspectCodeReportFile => RootDirectory / "Issues/inspect-code.xml";

	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
	private Target ReportIssues => _ => _
				 .DependsOn(Test)
				 .Produces(InspectCodeReportFile)
				 //.TriggeredBy(Test)
				 .Executes(() => {
					 var _ = ReSharperInspectCode(_ => _
						   .SetTargetPath(Solution)
						   .SetOutput(InspectCodeReportFile));

					 TeamCity.Instance?.ImportData(TeamCityImportType.ReSharperInspectCode, InspectCodeReportFile);
				 });
}
