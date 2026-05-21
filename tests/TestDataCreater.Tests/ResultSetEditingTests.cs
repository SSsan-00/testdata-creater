using TestDataCreater.Core;

namespace TestDataCreater.Tests;

[TestClass]
public sealed class ResultSetEditingTests
{
    [TestMethod]
    public void ColumnsAndRowsCanBeAddedRemovedAndReordered()
    {
        ResultSet resultSet = new("Editable");
        resultSet.AddColumn("A", CellValueKind.String, "a");
        resultSet.AddColumn("B", CellValueKind.String, "b");
        resultSet.AddColumn("C", CellValueKind.String, "c");
        ResultRow first = resultSet.AddRow("first");
        ResultRow second = resultSet.AddRow("second");

        resultSet.MoveColumn(2, 0);
        resultSet.MoveRow(1, 0);
        resultSet.RemoveColumn("b");
        resultSet.RemoveRow("first");

        CollectionAssert.AreEqual(new[] { "c", "a" }, resultSet.Columns.Select(column => column.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "second" }, resultSet.Rows.Select(row => row.Id).ToArray());
        Assert.IsFalse(second.Cells.ContainsKey("b"));
    }

    [TestMethod]
    public void ResetGridClearsColumnsRowsAndCellsWithoutChangingWorkspaceIdentity()
    {
        ResultSet resultSet = new("Users") { Id = "users" };
        resultSet.AddColumn("STATUS", CellValueKind.CustomExpression, "status");
        resultSet.AddColumn("AGE", CellValueKind.Int32, "age");
        ResultRow row = resultSet.AddRow("row-1");
        row.SetCell("status", new CellValue(CellValueKind.CustomExpression, "UserStatus.Active"));
        row.SetCell("age", new CellValue(CellValueKind.Int32, "42"));

        resultSet.ResetGrid();

        Assert.AreEqual("users", resultSet.Id);
        Assert.AreEqual("Users", resultSet.Name);
        Assert.AreEqual(1, resultSet.Columns.Count);
        Assert.AreEqual("COLUMN1", resultSet.Columns[0].Name);
        Assert.AreEqual(CellValueKind.String, resultSet.Columns[0].DefaultKind);
        Assert.AreEqual(1, resultSet.Rows.Count);
        Assert.AreEqual("", resultSet.Rows[0].GetCell(resultSet.Columns[0].Id).Text);
        Assert.IsFalse(resultSet.Columns.Any(column => column.Id == "status"));
        Assert.IsFalse(resultSet.Columns.Any(column => column.Id == "age"));
    }
}
