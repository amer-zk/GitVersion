using GitVersion.Configuration.Init.Wizard;
using GitVersion.Logging;

namespace GitVersion.Configuration.Init.BuildServer;

internal class SetupBuildScripts(IConsole console, IFileSystem fileSystem, ILog log, IConfigInitStepFactory stepFactory)
    : ConfigInitWizardStep(console, fileSystem, log, stepFactory)
{
    protected override StepResult HandleResult(string? result, Queue<ConfigInitWizardStep> steps, ConfigurationBuilder configurationBuilder, string workingDirectory)
    {
        switch (result)
        {
            case "0":
                steps.Enqueue(this.StepFactory.CreateStep<EditConfigStep>());
                return StepResult.Ok();
            case "1":
                steps.Enqueue(this.StepFactory.CreateStep<AppveyorPublicPrivate>());
                return StepResult.Ok();
        }
        return StepResult.Ok();
    }

    protected override string GetPrompt(ConfigurationBuilder configurationBuilder, string workingDirectory) => @"What build server are you using?

Want to see more? Contribute a pull request!

0) Go Back
1) AppVeyor";

    protected override string DefaultResult => "0";
}
