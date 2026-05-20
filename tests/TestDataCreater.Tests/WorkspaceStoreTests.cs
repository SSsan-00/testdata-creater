using TestDataCreater.Core;

namespace TestDataCreater.Tests;

[TestClass]
public sealed class WorkspaceStoreTests
{
    [TestMethod]
    public void SaveAndLoadPreservesMultipleResultSetsAndValueTypes()
    {
        string path = Path.Combine(Path.GetTempPath(), $"testdatacreater-{Guid.NewGuid():N}", "workspace.json");
        WorkspaceDocument document = new()
        {
            ActiveResultSetId = "users",
            ResultSets =
            [
                CreateUsers(),
                new ResultSet("Orders") { Id = "orders" }
            ]
        };

        WorkspaceStore store = new();
        store.Save(path, document);

        WorkspaceDocument loaded = store.Load(path);

        Assert.AreEqual("users", loaded.ActiveResultSetId);
        Assert.AreEqual(2, loaded.ResultSets.Count);
        Assert.AreEqual("Users", loaded.ResultSets[0].Name);
        Assert.AreEqual(CellValueKind.CustomExpression, loaded.ResultSets[0].Rows[0].Cells["status"].Kind);
        Assert.AreEqual("UserStatus.Active", loaded.ResultSets[0].Rows[0].Cells["status"].Text);
    }

    [TestMethod]
    public void ResetToDefaultClearsExistingInputAndRestoresInitialWorkspace()
    {
        WorkspaceDocument document = new()
        {
            ActiveResultSetId = "users",
            ResultSets =
            [
                CreateUsers(),
                new ResultSet("Orders") { Id = "orders" }
            ]
        };

        document.ResetToDefault();

        Assert.AreEqual(1, document.ResultSets.Count);
        Assert.AreEqual(document.ResultSets[0].Id, document.ActiveResultSetId);
        Assert.AreEqual("Default", document.ResultSets[0].Name);
        Assert.AreEqual(1, document.ResultSets[0].Columns.Count);
        Assert.AreEqual("COLUMN1", document.ResultSets[0].Columns[0].Name);
        Assert.AreEqual(1, document.ResultSets[0].Rows.Count);
        Assert.AreEqual("", document.ResultSets[0].Rows[0].GetCell(document.ResultSets[0].Columns[0].Id).Text);
    }

    private static ResultSet CreateUsers()
    {
        ResultSet resultSet = new("Users") { Id = "users" };
        resultSet.AddColumn("STATUS", CellValueKind.CustomExpression, "status");
        ResultRow row = resultSet.AddRow("row-1");
        row.SetCell("status", new CellValue(CellValueKind.CustomExpression, "UserStatus.Active"));
        return resultSet;
    }
}
