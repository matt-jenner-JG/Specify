#tool "nuget:?package=GitReleaseNotes"
#tool "nuget:?package=GitVersion.CommandLine"
#tool "nuget:?package=gitlink"

var target = Argument("target", "Default");
var outputDir = "./artifacts/";
var solutionPath = "./src/Specify.sln";
var specifyProjectJson = "./src/app/Specify/project.json";
var specifyAutofacProjectJson = "./src/app/Specify.Autofac/project.json";

Task("Clean")
	.Does(() => {
		if (DirectoryExists(outputDir))
		{
			DeleteDirectory(outputDir, recursive:true);
		}
		CreateDirectory(outputDir);
	});

Task("Restore")
	.Does(() => {
		DotNetCoreRestore("src");
	});

GitVersion versionInfo = null;
Task("Version")
	.Does(() => {
		GitVersion(new GitVersionSettings{
			UpdateAssemblyInfo = true,
			OutputType = GitVersionOutput.BuildServer
		});
		versionInfo = GitVersion(new GitVersionSettings{ OutputType = GitVersionOutput.Json });

		// Update project.json
		VersionProject(specifyProjectJson, versionInfo);
		VersionProject(specifyAutofacProjectJson, versionInfo);
	});

Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("Version")
	.IsDependentOn("Restore")
	.Does(() => {
		MSBuild(solutionPath, new MSBuildSettings 
		{
			Verbosity = Verbosity.Minimal,
			ToolVersion = MSBuildToolVersion.VS2015,
			Configuration = "Release",
			PlatformTarget = PlatformTarget.MSIL
		});
	});

Task("Test")
	.IsDependentOn("Build")
	.Does(() => {
		DotNetCoreTest("./src/tests/Specify.Tests");
		DotNetCoreTest("./src/tests/Specify.IntegrationTests");
		DotNetCoreTest("./src/Samples/Specify.Samples");
	});

Task("Package")
	.IsDependentOn("Test")
	.Does(() => {
		//GitLink("./", new GitLinkSettings { ArgumentCustomization = args => args.Append("-include Specify,Specify.Autofac") });
        
        GenerateReleaseNotes();

		PackageProject("Specify");
		PackageProject("Specify.Autofac");

		if (AppVeyor.IsRunningOnAppVeyor)
		{
			foreach (var file in GetFiles(outputDir + "**/*"))
				AppVeyor.UploadArtifact(file.FullPath);
		}
	});

private void VersionProject(string projectJsonPath, GitVersion versionInfo)
{
	var updatedProjectJson = System.IO.File.ReadAllText(projectJsonPath)
		.Replace("1.0.0-*", versionInfo.NuGetVersion);
	System.IO.File.WriteAllText(projectJsonPath, updatedProjectJson);
}

private void PackageProject(string projectName)
{
	var settings = new NuGetPackSettings 
	{ 
		OutputDirectory = outputDir, 
		Version = versionInfo.NuGetVersion
	};
	var nuspec = "nuget/" + projectName + ".nuspec";

	NuGetPack(nuspec, settings);

	System.IO.File.WriteAllLines(outputDir + "artifacts", new[]{
		"nuget:" + projectName + "." + versionInfo.NuGetVersion + ".nupkg",
		"nugetSymbols:" + projectName + "." + versionInfo.NuGetVersion + ".symbols.nupkg",
		"releaseNotes:releasenotes.md"
	});
}    

private void GenerateReleaseNotes()
{
	var releaseNotesExitCode = StartProcess(
		@"tools\GitReleaseNotes\tools\gitreleasenotes.exe", 
		new ProcessSettings { Arguments = ". /o artifacts/releasenotes.md" });
	if (string.IsNullOrEmpty(System.IO.File.ReadAllText("./artifacts/releasenotes.md")))
		System.IO.File.WriteAllText("./artifacts/releasenotes.md", "No issues closed since last release");

	if (releaseNotesExitCode != 0) throw new Exception("Failed to generate release notes");
}

Task("Default")
	.IsDependentOn("Package");

RunTarget(target);