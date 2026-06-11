using System.Globalization;
using System.Text;

namespace TestDataCreater.Core;

public sealed class HashMapCSharpExporter
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
        "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
        "virtual", "void", "volatile", "while"
    };

    public string Export(ResultSet resultSet, IReadOnlyList<ResultRow>? rows = null, string? variableName = null)
    {
        ArgumentNullException.ThrowIfNull(resultSet);

        IReadOnlyList<ResultRow> exportRows = rows ?? resultSet.Rows;
        string resolvedVariableName = string.IsNullOrWhiteSpace(variableName)
            ? ToVariableName(resultSet.Name)
            : variableName;

        return exportRows.Count switch
        {
            0 => $"var {resolvedVariableName} = new HashMap<HashMap>();",
            1 => ExportSingleRow(resultSet, exportRows[0], resolvedVariableName),
            _ => ExportMultipleRows(resultSet, exportRows, resolvedVariableName)
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

    private static string ToVariableName(string? workspaceName)
    {
        if (string.IsNullOrWhiteSpace(workspaceName))
        {
            return "data";
        }

        List<string> words = [];
        StringBuilder currentWord = new();

        foreach (char c in workspaceName.Trim())
        {
            if (IsVariableNameWordCharacter(c))
            {
                currentWord.Append(c);
                continue;
            }

            AddCurrentWord();
        }

        AddCurrentWord();

        if (words.Count == 0)
        {
            return "data";
        }

        StringBuilder variableName = new();
        for (int index = 0; index < words.Count; index++)
        {
            AppendCamelCaseWord(variableName, words[index], index == 0);
        }

        if (variableName.Length == 0)
        {
            return "data";
        }

        if (!IsCSharpIdentifierStartCharacter(variableName[0]))
        {
            variableName.Insert(0, '_');
        }

        string name = variableName.ToString();
        return CSharpKeywords.Contains(name) ? $"_{name}" : name;

        void AddCurrentWord()
        {
            if (currentWord.Length == 0)
            {
                return;
            }

            words.Add(currentWord.ToString());
            currentWord.Clear();
        }
    }

    private static void AppendCamelCaseWord(StringBuilder builder, string word, bool isFirstWord)
    {
        bool useLowercaseWord = word.Any(char.IsLetter) && word.Where(char.IsLetter).All(char.IsUpper);

        for (int index = 0; index < word.Length; index++)
        {
            char c = useLowercaseWord ? char.ToLowerInvariant(word[index]) : word[index];

            if (index == 0 && char.IsLetter(c))
            {
                c = isFirstWord ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c);
            }

            builder.Append(c);
        }
    }

    private static bool IsVariableNameWordCharacter(char c)
    {
        return c != '_' && IsCSharpIdentifierPartCharacter(c);
    }

    private static bool IsCSharpIdentifierStartCharacter(char c)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
        return c == '_' ||
            category is UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.LetterNumber;
    }

    private static bool IsCSharpIdentifierPartCharacter(char c)
    {
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
        return IsCSharpIdentifierStartCharacter(c) ||
            category is UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.ConnectorPunctuation
                or UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.Format;
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
