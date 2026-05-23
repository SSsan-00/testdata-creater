using System.Xml.Linq;

namespace TestDataCreater.Tests;

[TestClass]
public sealed class ProjectRequirementsTests
{
    [TestMethod]
    public void WinFormsProjectTargetsDotNet9Windows()
    {
        DirectoryInfo repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot.FullName, "src", "TestDataCreater", "TestDataCreater.csproj");
        XDocument project = XDocument.Load(projectPath);

        string? targetFramework = project.Root?
            .Element("PropertyGroup")?
            .Element("TargetFramework")?
            .Value;

        Assert.AreEqual("net9.0-windows", targetFramework);
    }

    [TestMethod]
    public void CoreProjectDoesNotEmitReleasePdbForSingleFileDistribution()
    {
        DirectoryInfo repositoryRoot = FindRepositoryRoot();
        string projectPath = Path.Combine(repositoryRoot.FullName, "src", "TestDataCreater.Core", "TestDataCreater.Core.csproj");
        XDocument project = XDocument.Load(projectPath);
        XElement? releasePropertyGroup = project.Root?
            .Elements("PropertyGroup")
            .FirstOrDefault(element => ((string?)element.Attribute("Condition"))?.Contains("Release", StringComparison.Ordinal) == true);

        Assert.IsNotNull(releasePropertyGroup);
        Assert.AreEqual("none", releasePropertyGroup.Element("DebugType")?.Value);
        Assert.AreEqual("false", releasePropertyGroup.Element("DebugSymbols")?.Value);
    }

    [TestMethod]
    public void BootstrapSourceDoesNotIncludeMSTestArtifacts()
    {
        DirectoryInfo repositoryRoot = FindRepositoryRoot();
        string bootstrapPath = Path.Combine(repositoryRoot.FullName, "TestDataCreater.Bootstrap.csproj");
        string bootstrapSource = File.ReadAllText(bootstrapPath);

        Assert.IsFalse(bootstrapSource.Contains("TestDataCreater.Tests", StringComparison.Ordinal));
        Assert.IsFalse(bootstrapSource.Contains("MSTest", StringComparison.Ordinal));
        Assert.IsFalse(bootstrapSource.Contains("tests/", StringComparison.Ordinal));
        Assert.IsFalse(bootstrapSource.Contains("tests\\", StringComparison.Ordinal));
    }

    [TestMethod]
    public void WinFormsProjectHasSingleFileReleasePublishProfile()
    {
        DirectoryInfo repositoryRoot = FindRepositoryRoot();
        string publishProfilePath = Path.Combine(
            repositoryRoot.FullName,
            "src",
            "TestDataCreater",
            "Properties",
            "PublishProfiles",
            "win-x64-single-file.pubxml");
        XDocument publishProfile = XDocument.Load(publishProfilePath);

        Assert.AreEqual("Release", ReadProperty(publishProfile, "Configuration"));
        Assert.AreEqual("net9.0-windows", ReadProperty(publishProfile, "TargetFramework"));
        Assert.AreEqual("win-x64", ReadProperty(publishProfile, "RuntimeIdentifier"));
        Assert.AreEqual("true", ReadProperty(publishProfile, "SelfContained"));
        Assert.AreEqual("true", ReadProperty(publishProfile, "PublishSingleFile"));
        Assert.AreEqual("true", ReadProperty(publishProfile, "IncludeNativeLibrariesForSelfExtract"));
        Assert.AreEqual("false", ReadProperty(publishProfile, "DebugSymbols"));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "TestDataCreater.sln")))
            {
                return current;
            }

            current = current.Parent;
        }

        Assert.Fail("Could not locate the repository root.");
        throw new InvalidOperationException("Could not locate the repository root.");
    }

    private static string? ReadProperty(XDocument project, string propertyName)
    {
        return project.Root?
            .Elements("PropertyGroup")
            .Elements(propertyName)
            .FirstOrDefault()?
            .Value;
    }
}
