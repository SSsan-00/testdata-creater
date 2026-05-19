using System.Text;

namespace TestDataCreater.Core;

public static class CsvResultSetImporter
{
    public static int ImportInto(ResultSet resultSet, string csvText)
    {
        ArgumentNullException.ThrowIfNull(resultSet);

        List<List<string>> csvRows = ParseRows(csvText);
        if (csvRows.Count == 0)
        {
            return 0;
        }

        int columnCount = csvRows.Max(row => row.Count);
        List<string> headers = NormalizeHeaders(csvRows[0], columnCount);
        List<List<string>> dataRows = csvRows
            .Skip(1)
            .Where(row => row.Any(value => value.Length > 0))
            .ToList();

        if (ShouldReplaceCurrentTable(resultSet))
        {
            resultSet.Columns.Clear();
            resultSet.Rows.Clear();
        }

        List<ResultColumn> columns = ResolveColumns(resultSet, headers);

        foreach (List<string> rowValues in dataRows)
        {
            ResultRow row = resultSet.AddRow();
            for (int index = 0; index < columns.Count; index++)
            {
                string text = index < rowValues.Count ? rowValues[index] : "";
                row.SetCell(columns[index].Id, CreateImportedCellValue(text));
            }
        }

        return dataRows.Count;
    }

    public static CellValue CreateImportedCellValue(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return new CellValue(CellValueKind.Null, "null");
        }

        if (CellValueKindRules.GetAllowedKinds(trimmed).Contains(CellValueKind.Int32))
        {
            return new CellValue(CellValueKind.Int32, trimmed);
        }

        return new CellValue(CellValueKind.String, text);
    }

    private static List<ResultColumn> ResolveColumns(ResultSet resultSet, List<string> headers)
    {
        List<ResultColumn> columns = [];
        foreach (string header in headers)
        {
            ResultColumn? existingColumn = resultSet.Columns.FirstOrDefault(column =>
                string.Equals(column.Name, header, StringComparison.OrdinalIgnoreCase));

            columns.Add(existingColumn ?? resultSet.AddColumn(header));
        }

        return columns;
    }

    private static bool ShouldReplaceCurrentTable(ResultSet resultSet)
    {
        if (resultSet.Rows.Count == 0)
        {
            return true;
        }

        return resultSet.Rows.All(row =>
            resultSet.Columns.All(column =>
                string.IsNullOrWhiteSpace(row.GetCell(column.Id, column.DefaultKind).Text)));
    }

    private static List<string> NormalizeHeaders(IReadOnlyList<string> headerRow, int columnCount)
    {
        HashSet<string> usedNames = new(StringComparer.OrdinalIgnoreCase);
        List<string> headers = [];

        for (int index = 0; index < columnCount; index++)
        {
            string baseName = index < headerRow.Count && !string.IsNullOrWhiteSpace(headerRow[index])
                ? headerRow[index].Trim()
                : $"COLUMN{index + 1}";

            string name = baseName;
            int suffix = 2;
            while (!usedNames.Add(name))
            {
                name = $"{baseName}_{suffix}";
                suffix++;
            }

            headers.Add(name);
        }

        return headers;
    }

    private static List<List<string>> ParseRows(string csvText)
    {
        List<List<string>> rows = [];
        List<string> row = [];
        StringBuilder field = new();
        bool inQuotes = false;

        for (int index = 0; index < csvText.Length; index++)
        {
            char value = csvText[index];
            if (inQuotes)
            {
                if (value == '"')
                {
                    if (index + 1 < csvText.Length && csvText[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    field.Append(value);
                }

                continue;
            }

            switch (value)
            {
                case '"':
                    if (field.Length == 0)
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        field.Append(value);
                    }
                    break;
                case ',':
                    AddField(row, field);
                    break;
                case '\r':
                    if (index + 1 < csvText.Length && csvText[index + 1] == '\n')
                    {
                        index++;
                    }

                    AddRow(rows, row, field);
                    break;
                case '\n':
                    AddRow(rows, row, field);
                    break;
                default:
                    field.Append(value);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0 || csvText.EndsWith(",", StringComparison.Ordinal))
        {
            AddRow(rows, row, field);
        }

        return rows;
    }

    private static void AddField(List<string> row, StringBuilder field)
    {
        row.Add(field.ToString());
        field.Clear();
    }

    private static void AddRow(List<List<string>> rows, List<string> row, StringBuilder field)
    {
        AddField(row, field);

        if (row.Count > 1 || row.Any(value => value.Length > 0))
        {
            rows.Add([.. row]);
        }

        row.Clear();
    }
}
