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
    public void ResetResultSetGridClearsOnlySelectedWorkspaceGrid()
    {
        ResultSet users = CreateUsers();
        ResultSet orders = new("Orders") { Id = "orders" };
        orders.AddColumn("ORDER_ID", CellValueKind.Int32, "order-id");
        ResultRow orderRow = orders.AddRow("order-1");
        orderRow.SetCell("order-id", new CellValue(CellValueKind.Int32, "100"));
        WorkspaceDocument document = new()
        {
            ActiveResultSetId = "users",
            ResultSets = [users, orders]
        };

        bool cleared = document.ResetResultSetGrid("users");

        Assert.IsTrue(cleared);
        Assert.AreEqual("users", document.ActiveResultSetId);
        Assert.AreEqual(2, document.ResultSets.Count);
        Assert.AreEqual("Users", users.Name);
        Assert.AreEqual(1, users.Columns.Count);
        Assert.AreEqual("COLUMN1", users.Columns[0].Name);
        Assert.AreEqual(1, users.Rows.Count);
        Assert.AreEqual("", users.Rows[0].GetCell(users.Columns[0].Id).Text);
        Assert.AreEqual("Orders", orders.Name);
        Assert.AreEqual(1, orders.Columns.Count);
        Assert.AreEqual("ORDER_ID", orders.Columns[0].Name);
        Assert.AreEqual("100", orders.Rows[0].GetCell("order-id").Text);
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
