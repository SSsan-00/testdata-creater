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
}
