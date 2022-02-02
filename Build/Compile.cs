public partial class Build : NukeBuild {
	//[Parameter("Ignore unreachable sources during " + nameof(Restore))]
	//private bool IgnoreFailedSources; //=> ValueInjectionUtility.TryGetValue<bool?>(() => IgnoreFailedSources) ?? false;

	private Target Clean =>
		_ => _
			.Before(From<IRestore>().Restore)
				.Executes(() => {
					SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
					//TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
					EnsureCleanDirectory(OutputDirectory);
				});

	private Target Compile1 =>
		_ => _
			.DependsOn(From<IRestore>().Restore)
			.WhenSkipped(DependencyBehavior.Skip)
			.Executes(() =>
				DotNetBuild(_ => _
					.SetNoRestore(InvokedTargets.Contains(From<IRestore>().Restore))
					//.SetProjectFile(Solution)
					.CombineWith(SourceProjects, ( _, v ) => _
						.SetProjectFile(v)
						.SetConfiguration(From<IHazConfiguration>().Configuration)
						.SetRepositoryUrl(From<IHazGitRepository>().GitRepository.HttpsUrl)
						.SetAssemblyVersion(From<IHazGitVersion>().Versioning.AssemblySemVer)
						.SetFileVersion(From<IHazGitVersion>().Versioning.AssemblySemFileVer)
						.SetInformationalVersion(From<IHazGitVersion>().Versioning.InformationalVersion))));

	//private Target Restore => _ => _.Executes(() => DotNetRestore(s => s.SetProjectFile(From<IHazSolution>().Solution).SetIgnoreFailedSources(IgnoreFailedSources)));
	private IEnumerable<Project> SourceProjects => from project in From<IHazSolution>().Solution.GetProjects("*") where !project.Name.ContainsAnyOrdinalIgnoreCase("build", "test") select project;
}
