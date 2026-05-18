namespace TestDataCreater.Core;

public sealed class CellValue
{
    public CellValue()
    {
    }

    public CellValue(CellValueKind kind, string? text = "")
    {
        Kind = kind;
        Text = text;
    }

    public CellValueKind Kind { get; set; } = CellValueKind.String;

    public string? Text { get; set; } = "";
}
