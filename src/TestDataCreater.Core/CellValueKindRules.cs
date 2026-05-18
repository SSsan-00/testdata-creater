using System.Globalization;

namespace TestDataCreater.Core;

public static class CellValueKindRules
{
    public static string ToCSharpTypeName(CellValueKind kind)
    {
        return kind switch
        {
            CellValueKind.String => "string",
            CellValueKind.Int32 => "int",
            CellValueKind.Int64 => "long",
            CellValueKind.Decimal => "decimal",
            CellValueKind.Double => "double",
            CellValueKind.Boolean => "bool",
            CellValueKind.DateTime => "DateTime",
            CellValueKind.Guid => "Guid",
            CellValueKind.Null => "null",
            CellValueKind.DbNull => "DBNull.Value",
            CellValueKind.CustomExpression => "C# expression",
            _ => kind.ToString()
        };
    }

    public static IReadOnlyList<CellValueKind> GetAllowedKinds(string? text)
    {
        string value = text?.Trim() ?? "";
        List<CellValueKind> kinds = [CellValueKind.String];

        if (value.Length == 0)
        {
            kinds.Add(CellValueKind.Null);
            kinds.Add(CellValueKind.DbNull);
            kinds.Add(CellValueKind.CustomExpression);
            return kinds;
        }

        if (IsCanonicalInteger(value, int.MinValue, int.MaxValue))
        {
            kinds.Add(CellValueKind.Int32);
        }

        if (IsCanonicalInteger(value, long.MinValue, long.MaxValue))
        {
            kinds.Add(CellValueKind.Int64);
        }

        if (IsCanonicalDecimal(value))
        {
            kinds.Add(CellValueKind.Decimal);
        }

        if (IsCanonicalDouble(value))
        {
            kinds.Add(CellValueKind.Double);
        }

        if (bool.TryParse(value, out _))
        {
            kinds.Add(CellValueKind.Boolean);
        }

        if (!LooksLikePlainNumber(value) &&
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            kinds.Add(CellValueKind.DateTime);
        }

        if (Guid.TryParse(value, out _))
        {
            kinds.Add(CellValueKind.Guid);
        }

        if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            kinds.Add(CellValueKind.Null);
        }

        if (value.Equals("DBNull.Value", StringComparison.Ordinal))
        {
            kinds.Add(CellValueKind.DbNull);
        }

        kinds.Add(CellValueKind.CustomExpression);
        return kinds;
    }

    public static CellValueKind CoerceToAllowedKind(CellValueKind requestedKind, string? text)
    {
        IReadOnlyList<CellValueKind> allowedKinds = GetAllowedKinds(text);
        return allowedKinds.Contains(requestedKind) ? requestedKind : CellValueKind.String;
    }

    private static bool IsCanonicalInteger(string value, long min, long max)
    {
        if (!long.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long parsed))
        {
            return false;
        }

        return parsed >= min && parsed <= max && IsCanonicalIntegerText(value);
    }

    private static bool IsCanonicalIntegerText(string value)
    {
        string unsigned = value.StartsWith("-", StringComparison.Ordinal) || value.StartsWith("+", StringComparison.Ordinal)
            ? value[1..]
            : value;

        if (unsigned.Length == 0)
        {
            return false;
        }

        return unsigned.Length == 1 || !unsigned.StartsWith('0');
    }

    private static bool IsCanonicalDecimal(string value)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _) &&
            HasCanonicalIntegerPart(value);
    }

    private static bool IsCanonicalDouble(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) &&
            !double.IsNaN(parsed) &&
            !double.IsInfinity(parsed) &&
            HasCanonicalIntegerPart(value);
    }

    private static bool HasCanonicalIntegerPart(string value)
    {
        string unsigned = value.StartsWith("-", StringComparison.Ordinal) || value.StartsWith("+", StringComparison.Ordinal)
            ? value[1..]
            : value;

        int decimalIndex = unsigned.IndexOf('.');
        int exponentIndex = unsigned.IndexOfAny(['e', 'E']);
        int endIndex = new[] { decimalIndex, exponentIndex }
            .Where(index => index >= 0)
            .DefaultIfEmpty(unsigned.Length)
            .Min();

        string integerPart = unsigned[..endIndex];
        return integerPart.Length == 1 || !integerPart.StartsWith('0');
    }

    private static bool LooksLikePlainNumber(string value)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }
}
