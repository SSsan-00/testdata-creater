using TestDataCreater.Core;

namespace TestDataCreater.Tests;

[TestClass]
public sealed class HashMapCSharpExporterTests
{
    [TestMethod]
    public void ExportSingleRowUsesHashMapInitializer()
    {
        ResultSet resultSet = CreateUserResultSet();
        ResultRow row = resultSet.AddRow();
        row.SetCell("user_id", new CellValue(CellValueKind.Int32, "1"));
        row.SetCell("user_name", new CellValue(CellValueKind.String, "Alice"));

        string code = new HashMapCSharpExporter().Export(resultSet);

        StringAssert.Contains(code, "var users = new HashMap");
        Assert.IsFalse(code.Contains("HashMap<HashMap>", StringComparison.Ordinal));
        StringAssert.Contains(code, "{ \"USER_ID\", 1 },");
        StringAssert.Contains(code, "{ \"USER_NAME\", \"Alice\" },");
    }

    [TestMethod]
    public void ExportMultipleRowsUsesHashMapOfHashMapWithSequentialKeys()
    {
        ResultSet resultSet = CreateUserResultSet();
        ResultRow first = resultSet.AddRow();
        first.SetCell("user_id", new CellValue(CellValueKind.Int32, "1"));
        first.SetCell("user_name", new CellValue(CellValueKind.String, "First"));
        ResultRow second = resultSet.AddRow();
        second.SetCell("user_id", new CellValue(CellValueKind.Int32, "2"));
        second.SetCell("user_name", new CellValue(CellValueKind.String, "Second"));

        resultSet.MoveRow(1, 0);

        string code = new HashMapCSharpExporter().Export(resultSet);

        StringAssert.Contains(code, "var users = new HashMap<HashMap>");
        StringAssert.Contains(code, "{ 0, new HashMap");
        StringAssert.Contains(code, "{ 1, new HashMap");
        Assert.IsTrue(code.IndexOf("\"Second\"", StringComparison.Ordinal) < code.IndexOf("\"First\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExportRowsUsesSelectedDisplayOrderAndRenumbersOuterKeys()
    {
        ResultSet resultSet = CreateUserResultSet();
        ResultRow first = resultSet.AddRow();
        first.SetCell("user_id", new CellValue(CellValueKind.Int32, "1"));
        first.SetCell("user_name", new CellValue(CellValueKind.String, "First"));
        ResultRow second = resultSet.AddRow();
        second.SetCell("user_id", new CellValue(CellValueKind.Int32, "2"));
        second.SetCell("user_name", new CellValue(CellValueKind.String, "Second"));
        ResultRow third = resultSet.AddRow();
        third.SetCell("user_id", new CellValue(CellValueKind.Int32, "3"));
        third.SetCell("user_name", new CellValue(CellValueKind.String, "Third"));

        string code = new HashMapCSharpExporter().Export(resultSet, [third, first]);

        StringAssert.Contains(code, "{ 0, new HashMap");
        StringAssert.Contains(code, "{ 1, new HashMap");
        Assert.IsTrue(code.IndexOf("\"Third\"", StringComparison.Ordinal) < code.IndexOf("\"First\"", StringComparison.Ordinal));
        Assert.IsFalse(code.Contains("\"Second\"", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ExportUsesWorkspaceNameAsDefaultVariableName()
    {
        ResultSet resultSet = new("Customer Orders");
        resultSet.AddColumn("ORDER_ID", CellValueKind.Int32, "order_id");
        resultSet.AddRow();

        string code = new HashMapCSharpExporter().Export(resultSet);

        StringAssert.Contains(code, "var customerOrders = new HashMap");
    }

    [TestMethod]
    public void ExportSanitizesWorkspaceNameForCSharpVariableName()
    {
        ResultSet resultSet = new("123 order-data!");
        resultSet.AddColumn("ORDER_ID", CellValueKind.Int32, "order_id");
        resultSet.AddRow();

        string code = new HashMapCSharpExporter().Export(resultSet);

        StringAssert.Contains(code, "var _123OrderData = new HashMap");
    }

    [TestMethod]
    public void ExportUsesExplicitVariableNameWhenProvided()
    {
        ResultSet resultSet = CreateUserResultSet();
        resultSet.AddRow();

        string code = new HashMapCSharpExporter().Export(resultSet, variableName: "data");

        StringAssert.Contains(code, "var data = new HashMap");
    }

    [TestMethod]
    public void ExportFallsBackToDataWhenWorkspaceNameIsNull()
    {
        ResultSet resultSet = CreateUserResultSet();
        resultSet.Name = null!;
        resultSet.AddRow();

        string code = new HashMapCSharpExporter().Export(resultSet);

        StringAssert.Contains(code, "var data = new HashMap");
    }

    [TestMethod]
    public void ExportEscapesStringsAndSupportsSpecialValueKinds()
    {
        ResultSet resultSet = new("Types");
        resultSet.AddColumn("TEXT", CellValueKind.String, "text");
        resultSet.AddColumn("AMOUNT", CellValueKind.Decimal, "amount");
        resultSet.AddColumn("WHEN", CellValueKind.DateTime, "when");
        resultSet.AddColumn("EMPTY", CellValueKind.Null, "empty");
        resultSet.AddColumn("DB_EMPTY", CellValueKind.DbNull, "db_empty");
        resultSet.AddColumn("CUSTOM", CellValueKind.CustomExpression, "custom");

        ResultRow row = resultSet.AddRow();
        row.SetCell("text", new CellValue(CellValueKind.String, "line1\n\"line2\""));
        row.SetCell("amount", new CellValue(CellValueKind.Decimal, "1200.50"));
        row.SetCell("when", new CellValue(CellValueKind.DateTime, "2026-05-18 10:30:00"));
        row.SetCell("empty", new CellValue(CellValueKind.Null));
        row.SetCell("db_empty", new CellValue(CellValueKind.DbNull));
        row.SetCell("custom", new CellValue(CellValueKind.CustomExpression, "OrderStatus.Completed"));

        string code = new HashMapCSharpExporter().Export(resultSet);

        StringAssert.Contains(code, "{ \"TEXT\", \"line1\\n\\\"line2\\\"\" },");
        StringAssert.Contains(code, "{ \"AMOUNT\", 1200.50m },");
        StringAssert.Contains(code, "{ \"WHEN\", new DateTime(2026, 5, 18, 10, 30, 0) },");
        StringAssert.Contains(code, "{ \"EMPTY\", null },");
        StringAssert.Contains(code, "{ \"DB_EMPTY\", DBNull.Value },");
        StringAssert.Contains(code, "{ \"CUSTOM\", OrderStatus.Completed },");
    }

    private static ResultSet CreateUserResultSet()
    {
        ResultSet resultSet = new("Users");
        resultSet.AddColumn("USER_ID", CellValueKind.Int32, "user_id");
        resultSet.AddColumn("USER_NAME", CellValueKind.String, "user_name");
        return resultSet;
    }
}
