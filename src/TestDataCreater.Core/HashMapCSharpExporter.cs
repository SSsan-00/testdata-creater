using System.Globalization;
using System.Text;

namespace TestDataCreater.Core;

public sealed class HashMapCSharpExporter
{
    public string Export(ResultSet resultSet, IReadOnlyList<ResultRow>? rows = null, string variableName = "data")
    {
        ArgumentNullException.ThrowIfNull(resultSet);

        IReadOnlyList<ResultRow> exportRows = rows ?? resultSet.Rows;

        return exportRows.Count switch
        {
            0 => $"var {variableName} = new HashMap<HashMap>();",
            1 => ExportSingleRow(resultSet, exportRows[0], variableName),
            _ => ExportMultipleRows(resultSet, exportRows, variableName)
        };
    }

    private static string ExportSingleRow(ResultSet resultSet, ResultRow row, string variableName)
    {
        StringBuilder builder = new();
        builder.AppendLine($"var {variableName} = new HashMap");
        builder.AppendLine("{");
        AppendRowEntries(builder, resultSet, row, "    ");
        builder.AppendLine("};");
        return builder.ToString();
    }

    private static string ExportMultipleRows(ResultSet resultSet, IReadOnlyList<ResultRow> rows, string variableName)
    {
        StringBuilder builder = new();
        builder.AppendLine($"var {variableName} = new HashMap<HashMap>");
        builder.AppendLine("{");

        for (int index = 0; index < rows.Count; index++)
        {
            builder.AppendLine($"    {{ {index}, new HashMap");
            builder.AppendLine("        {");
            AppendRowEntries(builder, resultSet, rows[index], "            ");
            builder.AppendLine("        }");
            builder.AppendLine("    },");
        }

        builder.AppendLine("};");
        return builder.ToString();
    }

    private static void AppendRowEntries(StringBuilder builder, ResultSet resultSet, ResultRow row, string indent)
    {
        foreach (ResultColumn column in resultSet.Columns)
        {
            CellValue value = row.GetCell(column.Id, column.DefaultKind);
            builder.Append(indent);
            builder.Append("{ ");
            builder.Append(ToStringLiteral(column.Name));
            builder.Append(", ");
            builder.Append(ToCSharpLiteral(value));
            builder.AppendLine(" },");
        }
    }

    private static string ToCSharpLiteral(CellValue value)
    {
        string text = value.Text ?? "";

        return value.Kind switch
        {
            CellValueKind.String => ToStringLiteral(text),
            CellValueKind.Int32 => string.IsNullOrWhiteSpace(text) ? "0" : text.Trim(),
            CellValueKind.Int64 => ToLongLiteral(text),
            CellValueKind.Decimal => ToDecimalLiteral(text),
            CellValueKind.Double => ToDoubleLiteral(text),
            CellValueKind.Boolean => ToBooleanLiteral(text),
            CellValueKind.DateTime => ToDateTimeLiteral(text),
            CellValueKind.Guid => string.IsNullOrWhiteSpace(text) ? "Guid.Empty" : $"Guid.Parse({ToStringLiteral(text.Trim())})",
            CellValueKind.Null => "null",
            CellValueKind.DbNull => "DBNull.Value",
            CellValueKind.CustomExpression => string.IsNullOrWhiteSpace(text) ? "null" : text.Trim(),
            _ => ToStringLiteral(text)
        };
    }

    private static string ToStringLiteral(string value)
    {
        StringBuilder builder = new(value.Length + 2);
        builder.Append('"');

        foreach (char c in value)
        {
            builder.Append(c switch
            {
                '\\' => "\\\\",
                '"' => "\\\"",
                '\r' => "\\r",
                '\n' => "\\n",
                '\t' => "\\t",
                _ => c.ToString()
            });
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static string ToLongLiteral(string text)
    {
        string trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "0L";
        }

        return trimmed.EndsWith('L') || trimmed.EndsWith('l') ? trimmed : $"{trimmed}L";
    }

    private static string ToDecimalLiteral(string text)
    {
        string trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "0m";
        }

        return trimmed.EndsWith('M') || trimmed.EndsWith('m') ? trimmed : $"{trimmed}m";
    }

    private static string ToDoubleLiteral(string text)
    {
        string trimmed = text.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "0d";
        }

        char last = trimmed[^1];
        return last is 'D' or 'd' or 'F' or 'f' ? trimmed : $"{trimmed}d";
    }

    private static string ToBooleanLiteral(string text)
    {
        return bool.TryParse(text, out bool value)
            ? value.ToString().ToLowerInvariant()
            : text.Trim();
    }

    private static string ToDateTimeLiteral(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "default(DateTime)";
        }

        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime) ||
            DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime))
        {
            return dateTime.Millisecond == 0
                ? $"new DateTime({dateTime.Year}, {dateTime.Month}, {dateTime.Day}, {dateTime.Hour}, {dateTime.Minute}, {dateTime.Second})"
                : $"new DateTime({dateTime.Year}, {dateTime.Month}, {dateTime.Day}, {dateTime.Hour}, {dateTime.Minute}, {dateTime.Second}, {dateTime.Millisecond})";
        }

        return $"DateTime.Parse({ToStringLiteral(text.Trim())})";
    }
}
