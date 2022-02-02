public partial class Build : NukeBuild {

	[Parameter]
	private readonly string NUGET_API_KEY;

	private static string NuGetPackageSource => "https://api.nuget.org/v3/index.json";
	private string GitHubPackageSource => $"https://nuget.pkg.github.com/{GitHubActions.RepositoryOwner}/index.json";
	private IReadOnlyCollection<AbsolutePath> PackageFiles => PackageDirectory.GlobFiles("*.nupkg");

	private Target Publish =>
		_ => _
			.ProceedAfterFailure()
			.DependsOn(Clean, From<ITest>().Test, From<IPack>().Pack)
			.Consumes(From<IPack>().Pack)
			.Requires(() => !NUGET_API_KEY.IsNullOrEmpty() || IsOriginalRepository)
			.Requires(() => GitHasCleanWorkingCopy())
			.Requires(() => From<IHazConfiguration>().Configuration.Equals(Configuration.Release))
			.Requires(() => IsOriginalRepository && From<IHazGitRepository>().GitRepository.IsOnMasterBranch() ||
						IsOriginalRepository && From<IHazGitRepository>().GitRepository.IsOnReleaseBranch() ||
							!IsOriginalRepository && From<IHazGitRepository>().GitRepository.IsOnDevelopBranch())
			.Executes(() => DotNetNuGetPush(_ => _
								.SetSource(Source)
								.SetApiKey(NUGET_API_KEY)
								.CombineWith(PackageFiles, ( _, v ) => _.SetTargetPath(v)), degreeOfParallelism: 5, completeOnFailure: true));

	private string Source => IsOriginalRepository ? NuGetPackageSource : GitHubPackageSource;
}
