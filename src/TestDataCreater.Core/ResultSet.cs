namespace TestDataCreater.Core;

public sealed class ResultSet
{
    public ResultSet()
    {
    }

    public ResultSet(string name)
    {
        Name = name;
    }

    public string Id { get; set; } = ResultColumn.CreateId();

    public string Name { get; set; } = "Result Set";

    public List<ResultColumn> Columns { get; set; } = [];

    public List<ResultRow> Rows { get; set; } = [];

    public ResultColumn AddColumn(string name, CellValueKind defaultKind = CellValueKind.String, string? id = null)
    {
        ResultColumn column = new()
        {
            Id = string.IsNullOrWhiteSpace(id) ? ResultColumn.CreateId() : id,
            Name = name,
            DefaultKind = defaultKind
        };

        Columns.Add(column);

        foreach (ResultRow row in Rows)
        {
            row.SetCell(column.Id, new CellValue(defaultKind));
        }

        return column;
    }

    public void RemoveColumn(string columnId)
    {
        Columns.RemoveAll(column => column.Id == columnId);

        foreach (ResultRow row in Rows)
        {
            row.Cells.Remove(columnId);
        }
    }

    public void MoveColumn(int fromIndex, int toIndex)
    {
        Move(Columns, fromIndex, toIndex);
    }

    public ResultRow AddRow(string? id = null)
    {
        ResultRow row = new()
        {
            Id = string.IsNullOrWhiteSpace(id) ? ResultColumn.CreateId() : id
        };

        foreach (ResultColumn column in Columns)
        {
            row.SetCell(column.Id, new CellValue(column.DefaultKind));
        }

        Rows.Add(row);
        return row;
    }

    public void RemoveRow(string rowId)
    {
        Rows.RemoveAll(row => row.Id == rowId);
    }

    public void MoveRow(int fromIndex, int toIndex)
    {
        Move(Rows, fromIndex, toIndex);
    }

    public void ResetGrid()
    {
        Columns.Clear();
        Rows.Clear();
        AddColumn("COLUMN1");
        AddRow();
    }

    public override string ToString()
    {
        return Name;
    }

    private static void Move<T>(IList<T> items, int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(fromIndex));
        }

        if (toIndex < 0 || toIndex >= items.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(toIndex));
        }

        if (fromIndex == toIndex)
        {
            return;
        }

        T item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(toIndex, item);
    }
}
