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

File			: ReportIssues.cs @ d:\Development\FSharp\CSVParser\Build\ReportIssues.cs
File Created	: Friday, 5th March 2021 9:28:22 pm

Author			: Karthik K Selvan

Last Modified	: Friday, 5th March 2021 9:36:25 pm
Modified By		: Karthik K Selvan

Change History:

 */

public partial class Build : NukeBuild
{
	#region Private Properties

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
	private AbsolutePath InspectCodeReportFile => RootDirectory / "Issues/inspect-code.xml";

	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "<Pending>")]
	private Target ReportIssues => _ => _
			.DependsOn(Test)
			.Produces(InspectCodeReportFile)
			//.TriggeredBy(Test)
			.Executes(() =>
			{
				var _ = ReSharperInspectCode(_ => _
					  .SetTargetPath(Solution)
					  .SetOutput(InspectCodeReportFile));

				TeamCity.Instance?.ImportData(TeamCityImportType.ReSharperInspectCode, InspectCodeReportFile);
			});

	#endregion Private Properties
}
