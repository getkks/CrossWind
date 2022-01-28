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

Project			: Build @ d:\Development\FSharp\CSVParser\Build

File			: Test.cs @ d:\Development\FSharp\CSVParser\Build\Test.cs
File Created	: Friday, 5th March 2021 6:50:19 pm

Author			: Karthik K Selvan

Last Modified	: Saturday, 6th March 2021 9:27:13 am
Modified By		: Karthik K Selvan

Change History:

 */

public partial class Build : NukeBuild
{
	#region Private Properties

	[Partition(2)] private readonly Partition TestPartition;

	private static AbsolutePath TestsDirectory => RootDirectory / "Tests";

	private Target Test => _ => _
		.DependsOn(Compile)
		.WhenSkipped(DependencyBehavior.Skip)
		.Produces(CoverageDirectory / "*.xml")
		.Produces(CoverageDirectory / "*.trx")
		.Partition(2)
		.Executes(() =>
		{
			try
			{
				var _ = DotNetTest(_ => _
					  .SetConfiguration(Configuration)
					  .SetNoBuild(InvokedTargets.Contains(Compile))
					  .ResetVerbosity()
					  .SetResultsDirectory(CoverageDirectory)
					  .EnableCollectCoverage()
					  .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
					  .SetExcludeByFile("*.Generated.*")
					  .EnableUseSourceLink()
					  .CombineWith(TestProjects, (_, v) => _
						  .SetProjectFile(v)
						  .EnableCollectCoverage()
						  //.SetLogger($"trx;LogFileName={v.Name}.trx")
						  .SetCoverletOutput(CoverageDirectory / $"{v.Name}/cov.xml")));
			}
			finally
			{
				CoverageDirectory.GlobFiles("*.trx").ForEach(x =>
					AzurePipelines.Instance?.PublishTestResults(type: AzurePipelinesTestResultsType.VSTest,
												 title: $"{Path.GetFileNameWithoutExtension(x)} ({AzurePipelines.Instance.StageDisplayName})",
												 files: new string[] { x }));
			}
		});

	private IEnumerable<Project> TestProjects => from project in TestPartition.GetCurrent(Solution.GetProjects("*")) where project.Name.ContainsOrdinalIgnoreCase("test") select project;

	#endregion Private Properties
}
