public partial class Build : NukeBuild {
	private AbsolutePath DocFxFile => DocumentationDirectory / "docfx.json";

	private Target Documentation => _ => _
		 //.DependsOn(Clean)
		 .Executes(() => {
			// Using README.md as index.md
			if( File.Exists(DocumentationDirectory / "index.md") ) {
				 File.Delete(DocumentationDirectory / "index.md");
			 }
			 File.Copy(RootDirectory / "README.md", DocumentationDirectory / "index.md");
			 var _ = DocFXTasks.DocFXBuild(x => x.SetConfigFile(DocFxFile));
		 });

	private AbsolutePath DocumentationDirectory => RootDirectory / "Documentation";
}
