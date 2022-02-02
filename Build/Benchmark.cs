public partial class Build : NukeBuild {

	private Target Benchmark =>
		_ => _
			.DependsOn(From<ICompile>().Compile)
			.WhenSkipped(DependencyBehavior.Skip)
			.Executes(() => BenchmarkDotNetTasks.BenchmarkDotNet(_ => _
				.SetAffinity(1)
				.SetDisassembly(true)
				.SetDisassemblyDiff(true)
				.SetExporters(BenchmarkDotNetExporter.GitHub, BenchmarkDotNetExporter.CSV)));
}
