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

Project			: Build @ z:\FSharp\CSVParser\Build

File			: Documentation.cs @ z:\FSharp\CSVParser\Build\Documentation.cs
File Created	: Friday, 19th March 2021 12:47:56 pm

Author			: Karthik K Selvan

Last Modified	: Sun May 02 2021
Modified By		: Karthik K Selvan

Change History:

 */
public partial class Build : NukeBuild
{
	private AbsolutePath DocFxFile => DocumentationDirectory / "docfx.json";
	private AbsolutePath DocumentationDirectory => RootDirectory / "Documentation";

	Target Documentation => _ => _
		//.DependsOn(Clean)
		.Executes(() =>
		{
			// Using README.md as index.md
			if (File.Exists(DocumentationDirectory / "index.md"))
			{
				File.Delete(DocumentationDirectory / "index.md");
			}
			File.Copy(RootDirectory / "README.md", DocumentationDirectory / "index.md");
			var _ = DocFXTasks.DocFXBuild(x => x.SetConfigFile(DocFxFile));
		});

}
