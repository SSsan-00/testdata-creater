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
}
