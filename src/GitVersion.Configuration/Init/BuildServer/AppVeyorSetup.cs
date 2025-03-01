using GitVersion.Configuration.Init.Wizard;
using GitVersion.Helpers;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.BuildServer;

internal class AppVeyorSetup(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    : ConfigInitWizardStep(console, fileSystem, log, stepFactory)
{
    private ProjectVisibility projectVisibility;

    public AppVeyorSetup WithData(ProjectVisibility visibility)
    {
        this.projectVisibility = visibility;
        return this;
    }

    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        var editConfigStep = this.StepFactory.CreateStep<EditConfigStep>();
        switch (result)
        {
            case "0":
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "1":
                GenerateBasicConfig(workingDirectory);
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
            case "2":
                GenerateNuGetConfig(workingDirectory);
                steps.Enqueue(editConfigStep);
                return StepResult.Ok();
        }
        return StepResult.InvalidResponseSelected();
    }

    private static string GetGvCommand(ProjectVisibility visibility) => visibility switch
    {
        ProjectVisibility.Public => "  - ps: gitversion /l console /output buildserver /updateAssemblyInfo",
        ProjectVisibility.Private => "  - ps: gitversion $env:APPVEYOR_BUILD_FOLDER /l console /output buildserver /updateAssemblyInfo /nofetch /b $env:APPVEYOR_REPO_BRANCH",
        _ => ""
    };

    private void GenerateBasicConfig(string workingDirectory) => WriteConfig(workingDirectory, this.FileSystem, $@"install:
  - choco install gitversion.portable -pre -y

before_build:
  - nuget restore
{GetGvCommand(this.projectVisibility)}

build:
  project: <your sln file>");

    private void GenerateNuGetConfig(string workingDirectory) => WriteConfig(workingDirectory, this.FileSystem, $@"install:
  - choco install gitversion.portable -pre -y

assembly_info:
  patch: false

before_build:
  - nuget restore
{GetGvCommand(this.projectVisibility)}

build:
  project: <your sln file>

after_build:
  - cmd: ECHO nuget pack <Project>\<NuSpec>.nuspec -version ""%GitVersion_NuGetVersion%"" -prop ""target=%CONFIGURATION%""
  - cmd: nuget pack <Project>\<NuSpec>.nuspec -version ""%GitVersion_NuGetVersion%"" -prop ""target=%CONFIGURATION%""
  - cmd: appveyor PushArtifact ""<NuSpec>.%GitVersion_NuGetVersion%.nupkg""");

    private void WriteConfig(string workingDirectory, IFileSystem fileSystem, string configContents)
    {
        var outputFilename = GetOutputFilename(workingDirectory, fileSystem);
        fileSystem.WriteAllText(outputFilename, configContents);
        this.Log.Info($"AppVeyor sample configuration file written to {outputFilename}");
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        var prompt = new StringBuilder();
        if (AppVeyorConfigExists(workingDirectory, this.FileSystem))
        {
            prompt.AppendLine("GitVersion doesn't support modifying existing appveyor configuration files. We will generate appveyor.gitversion.yml instead");
            prompt.AppendLine();
        }

        prompt.Append(@"What sort of configuration template would you like generated?

0) Go Back
1) Generate basic (gitversion + msbuild) configuration
2) Generate with NuGet package publish");

        return prompt.ToString();
    }

    private static string GetOutputFilename(string workingDirectory, IFileSystem fileSystem)
    {
        if (AppVeyorConfigExists(workingDirectory, fileSystem))
        {
            var count = 0;
            do
            {
                var path = PathHelper.Combine(workingDirectory, $"appveyor.gitversion{(count == 0 ? string.Empty : "." + count)}.yml");

                if (!fileSystem.Exists(path))
                {
                    return path;
                }

                count++;
            } while (count < 10);
            throw new Exception("appveyor.gitversion.yml -> appveyor.gitversion.9.yml all exist. Pretty sure you have enough templates");
        }

        return PathHelper.Combine(workingDirectory, "appveyor.yml");
    }

    private static bool AppVeyorConfigExists(string workingDirectory, IFileSystem fileSystem) => fileSystem.Exists(PathHelper.Combine(workingDirectory, "appveyor.yml"));

    protected override string DefaultResult => "0";
}
