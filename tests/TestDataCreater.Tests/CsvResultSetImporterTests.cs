using TestDataCreater.Core;

namespace TestDataCreater.Tests;

[TestClass]
public sealed class CsvResultSetImporterTests
{
    [TestMethod]
    public void ImportIntoReplacesBlankTableAndInfersCellKinds()
    {
        ResultSet resultSet = new("CSV");
        resultSet.AddColumn("COLUMN1");
        resultSet.AddRow();

        int importedRows = CsvResultSetImporter.ImportInto(
            resultSet,
            "ID,NAME,MEMO\r\n1,Alice,null\r\n02,Bob,\"hello, csv\"");

        Assert.AreEqual(2, importedRows);
        CollectionAssert.AreEqual(new[] { "ID", "NAME", "MEMO" }, resultSet.Columns.Select(column => column.Name).ToArray());
        Assert.AreEqual(CellValueKind.Int32, resultSet.Rows[0].GetCell(resultSet.Columns[0].Id).Kind);
        Assert.AreEqual("1", resultSet.Rows[0].GetCell(resultSet.Columns[0].Id).Text);
        Assert.AreEqual(CellValueKind.Null, resultSet.Rows[0].GetCell(resultSet.Columns[2].Id).Kind);
        Assert.AreEqual(CellValueKind.String, resultSet.Rows[1].GetCell(resultSet.Columns[0].Id).Kind);
        Assert.AreEqual("02", resultSet.Rows[1].GetCell(resultSet.Columns[0].Id).Text);
        Assert.AreEqual("hello, csv", resultSet.Rows[1].GetCell(resultSet.Columns[2].Id).Text);
    }

    [TestMethod]
    public void ImportIntoAppendsRowsWhenTableHasData()
    {
        ResultSet resultSet = new("Existing");
        ResultColumn id = resultSet.AddColumn("ID");
        ResultRow existingRow = resultSet.AddRow();
        existingRow.SetCell(id.Id, new CellValue(CellValueKind.String, "existing"));

        int importedRows = CsvResultSetImporter.ImportInto(resultSet, "ID,VALUE\n10,New");

        Assert.AreEqual(1, importedRows);
        CollectionAssert.AreEqual(new[] { "ID", "VALUE" }, resultSet.Columns.Select(column => column.Name).ToArray());
        CollectionAssert.AreEqual(
            new[] { "existing", "10" },
            resultSet.Rows.Select(row => row.GetCell(resultSet.Columns[0].Id).Text).ToArray());
        Assert.AreEqual(CellValueKind.Int32, resultSet.Rows[1].GetCell(resultSet.Columns[0].Id).Kind);
        Assert.AreEqual("New", resultSet.Rows[1].GetCell(resultSet.Columns[1].Id).Text);
    }
}
