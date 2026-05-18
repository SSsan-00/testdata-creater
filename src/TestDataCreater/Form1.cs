using TestDataCreater.Core;

namespace TestDataCreater;

public partial class Form1 : Form
{
    private readonly WorkspaceStore _store = new();
    private readonly HashMapCSharpExporter _exporter = new();
    private readonly string _workspacePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TestDataCreater",
        "workspace.json");

    private WorkspaceDocument _document = new();
    private ResultSet? _currentResultSet;
    private bool _loading;
    private int _dragRowIndex = -1;
    private Point _dragStartPoint;

    private readonly ListBox _workspaceList = new();
    private readonly TextBox _workspaceNameBox = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _previewBox = new();
    private readonly TextBox _columnNameBox = new();
    private readonly ComboBox _cellKindBox = new();
    private readonly Label _statusLabel = new();

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        LoadWorkspaceDocument();
    }

    private void BuildUi()
    {
        Text = "TestDataCreater - HashMap Exporter";
        Width = 1200;
        Height = 780;
        MinimumSize = new Size(960, 640);

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        FlowLayoutPanel toolbar = CreateToolbar();
        SplitContainer mainSplit = CreateMainSplit();

        _previewBox.Dock = DockStyle.Fill;
        _previewBox.Multiline = true;
        _previewBox.ScrollBars = ScrollBars.Both;
        _previewBox.WordWrap = false;
        _previewBox.Font = new Font(FontFamily.GenericMonospace, 10);

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(mainSplit, 0, 1);
        root.Controls.Add(_previewBox, 0, 2);
        root.Controls.Add(_statusLabel, 0, 3);
        Controls.Add(root);

        FormClosing += (_, _) => SaveWorkspaceDocument();
    }

    private FlowLayoutPanel CreateToolbar()
    {
        FlowLayoutPanel toolbar = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(6),
            WrapContents = false
        };

        toolbar.Controls.Add(CreateButton("Add row", AddRow));
        toolbar.Controls.Add(CreateButton("Delete row", DeleteSelectedRows));
        toolbar.Controls.Add(CreateButton("Row up", () => MoveCurrentRow(-1)));
        toolbar.Controls.Add(CreateButton("Row down", () => MoveCurrentRow(1)));
        toolbar.Controls.Add(CreateButton("Add column", AddColumn));
        toolbar.Controls.Add(CreateButton("Delete column", DeleteCurrentColumn));
        toolbar.Controls.Add(CreateButton("Column left", () => MoveCurrentColumn(-1)));
        toolbar.Controls.Add(CreateButton("Column right", () => MoveCurrentColumn(1)));
        toolbar.Controls.Add(CreateButton("Preview", PreviewExport));
        toolbar.Controls.Add(CreateButton("Export copy", ExportToClipboard));
        toolbar.Controls.Add(CreateButton("Save", SaveWorkspaceDocument));

        return toolbar;
    }

    private SplitContainer CreateMainSplit()
    {
        SplitContainer outer = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 230
        };

        TableLayoutPanel workspacePanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(6)
        };
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        _workspaceNameBox.Dock = DockStyle.Fill;
        _workspaceNameBox.TextChanged += (_, _) =>
        {
            if (_loading || _currentResultSet is null)
            {
                return;
            }

            _currentResultSet.Name = string.IsNullOrWhiteSpace(_workspaceNameBox.Text)
                ? "Result Set"
                : _workspaceNameBox.Text.Trim();
            _workspaceList.Refresh();
            SaveWorkspaceDocument();
        };

        _workspaceList.Dock = DockStyle.Fill;
        _workspaceList.DisplayMember = nameof(ResultSet.Name);
        _workspaceList.SelectedIndexChanged += (_, _) => SelectWorkspaceFromList();

        workspacePanel.Controls.Add(new Label { Text = "Workspace", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        workspacePanel.Controls.Add(_workspaceNameBox, 0, 1);
        workspacePanel.Controls.Add(_workspaceList, 0, 2);
        workspacePanel.Controls.Add(CreateButton("Add workspace", AddWorkspace), 0, 3);
        workspacePanel.Controls.Add(CreateButton("Delete workspace", DeleteWorkspace), 0, 4);

        SplitContainer editorSplit = new()
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 720
        };

        ConfigureGrid();
        editorSplit.Panel1.Controls.Add(_grid);
        editorSplit.Panel2.Controls.Add(CreateInspectorPanel());

        outer.Panel1.Controls.Add(workspacePanel);
        outer.Panel2.Controls.Add(editorSplit);
        return outer;
    }

    private Panel CreateInspectorPanel()
    {
        TableLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            Padding = new Padding(8)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _columnNameBox.Dock = DockStyle.Fill;
        _columnNameBox.TextChanged += (_, _) => RenameCurrentColumn();

        _cellKindBox.Dock = DockStyle.Fill;
        _cellKindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _cellKindBox.DataSource = Enum.GetValues<CellValueKind>();
        _cellKindBox.SelectedIndexChanged += (_, _) => ApplySelectedCellKind();

        panel.Controls.Add(new Label { Text = "Column name", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        panel.Controls.Add(_columnNameBox, 0, 1);
        panel.Controls.Add(new Label(), 0, 2);
        panel.Controls.Add(new Label { Text = "Selected cell type", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 3);
        panel.Controls.Add(_cellKindBox, 0, 4);
        panel.Controls.Add(new Label(), 0, 5);
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Use CustomExpression for arbitrary C# values such as OrderStatus.Completed or new Money(...).",
            AutoSize = false
        }, 0, 6);

        return panel;
    }

    private static Button CreateButton(string text, Action action)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = true,
            Margin = new Padding(3)
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowDrop = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = true;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.MultiSelect = true;
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.RowHeadersWidth = 54;

        _grid.CellEndEdit += (_, _) => SyncGridToModel();
        _grid.SelectionChanged += (_, _) => RefreshInspector();
        _grid.ColumnDisplayIndexChanged += (_, _) =>
        {
            if (!_loading)
            {
                BeginInvoke(SyncColumnOrderFromGrid);
            }
        };
        _grid.MouseDown += GridMouseDown;
        _grid.MouseMove += GridMouseMove;
        _grid.DragOver += (_, e) => e.Effect = DragDropEffects.Move;
        _grid.DragDrop += GridDragDrop;
    }

    private void LoadWorkspaceDocument()
    {
        _document = _store.Load(_workspacePath);

        if (_document.ResultSets.Count == 0)
        {
            _document = WorkspaceDocument.CreateDefault();
        }

        ReloadWorkspaceList();

        ResultSet? active = _document.ResultSets.FirstOrDefault(resultSet => resultSet.Id == _document.ActiveResultSetId)
            ?? _document.ResultSets.FirstOrDefault();

        if (active is not null)
        {
            _workspaceList.SelectedItem = active;
        }
    }

    private void ReloadWorkspaceList()
    {
        _loading = true;
        _workspaceList.Items.Clear();

        foreach (ResultSet resultSet in _document.ResultSets)
        {
            _workspaceList.Items.Add(resultSet);
        }

        _loading = false;
    }

    private void SelectWorkspaceFromList()
    {
        if (_loading)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet = _workspaceList.SelectedItem as ResultSet;
        _document.ActiveResultSetId = _currentResultSet?.Id;
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void LoadGrid()
    {
        _loading = true;
        _grid.Columns.Clear();
        _grid.Rows.Clear();

        _workspaceNameBox.Text = _currentResultSet?.Name ?? "";

        if (_currentResultSet is null)
        {
            _loading = false;
            return;
        }

        foreach (ResultColumn column in _currentResultSet.Columns)
        {
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = column.Id,
                HeaderText = column.Name,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        foreach (ResultRow resultRow in _currentResultSet.Rows)
        {
            int rowIndex = _grid.Rows.Add();
            DataGridViewRow gridRow = _grid.Rows[rowIndex];
            gridRow.Tag = resultRow.Id;
            gridRow.HeaderCell.Value = (rowIndex + 1).ToString();

            foreach (ResultColumn column in _currentResultSet.Columns)
            {
                gridRow.Cells[column.Id].Value = resultRow.GetCell(column.Id, column.DefaultKind).Text;
            }
        }

        _loading = false;
        _grid.ClearSelection();
        _grid.CurrentCell = null;
        RefreshInspector();
        PreviewExport();
    }

    private void SyncGridToModel()
    {
        if (_loading || _currentResultSet is null)
        {
            return;
        }

        SyncColumnOrderFromGrid();

        foreach (DataGridViewRow gridRow in _grid.Rows)
        {
            if (gridRow.Tag is not string rowId)
            {
                continue;
            }

            ResultRow? resultRow = _currentResultSet.Rows.FirstOrDefault(row => row.Id == rowId);
            if (resultRow is null)
            {
                continue;
            }

            foreach (DataGridViewColumn gridColumn in _grid.Columns)
            {
                CellValue cellValue = resultRow.GetCell(gridColumn.Name);
                cellValue.Text = Convert.ToString(gridRow.Cells[gridColumn.Index].Value) ?? "";
            }
        }
    }

    private void SyncColumnOrderFromGrid()
    {
        if (_loading || _currentResultSet is null || _grid.Columns.Count != _currentResultSet.Columns.Count)
        {
            return;
        }

        List<string> orderedIds = _grid.Columns
            .Cast<DataGridViewColumn>()
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.Name)
            .ToList();

        _currentResultSet.Columns = orderedIds
            .Select(id => _currentResultSet.Columns.First(column => column.Id == id))
            .ToList();
    }

    private void AddWorkspace()
    {
        SyncGridToModel();
        ResultSet resultSet = new($"Result Set {_document.ResultSets.Count + 1}");
        resultSet.AddColumn("COLUMN1");
        resultSet.AddRow();
        _document.ResultSets.Add(resultSet);
        _document.ActiveResultSetId = resultSet.Id;
        ReloadWorkspaceList();
        _workspaceList.SelectedItem = resultSet;
        SaveWorkspaceDocument();
    }

    private void DeleteWorkspace()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        _document.ResultSets.Remove(_currentResultSet);

        if (_document.ResultSets.Count == 0)
        {
            _document = WorkspaceDocument.CreateDefault();
        }

        ReloadWorkspaceList();
        _workspaceList.SelectedItem = _document.ResultSets[0];
        SaveWorkspaceDocument();
    }

    private void AddRow()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet.AddRow();
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void DeleteSelectedRows()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        SyncGridToModel();
        List<string> rowIds = GetSelectedRowIds();

        if (rowIds.Count == 0 && _grid.CurrentRow?.Tag is string currentId)
        {
            rowIds.Add(currentId);
        }

        foreach (string rowId in rowIds)
        {
            _currentResultSet.RemoveRow(rowId);
        }

        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void MoveCurrentRow(int direction)
    {
        if (_currentResultSet is null || _grid.CurrentCell is null)
        {
            return;
        }

        int fromIndex = _grid.CurrentCell.RowIndex;
        int toIndex = Math.Clamp(fromIndex + direction, 0, _currentResultSet.Rows.Count - 1);

        if (fromIndex == toIndex)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet.MoveRow(fromIndex, toIndex);
        LoadGrid();
        SelectGridRow(toIndex);
        SaveWorkspaceDocument();
    }

    private void AddColumn()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet.AddColumn($"COLUMN{_currentResultSet.Columns.Count + 1}");
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void DeleteCurrentColumn()
    {
        if (_currentResultSet is null || _grid.CurrentCell is null || _currentResultSet.Columns.Count == 0)
        {
            return;
        }

        SyncGridToModel();
        string columnId = _grid.Columns[_grid.CurrentCell.ColumnIndex].Name;
        _currentResultSet.RemoveColumn(columnId);
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void MoveCurrentColumn(int direction)
    {
        if (_currentResultSet is null || _grid.CurrentCell is null)
        {
            return;
        }

        SyncGridToModel();
        int fromIndex = _currentResultSet.Columns.FindIndex(column => column.Id == _grid.Columns[_grid.CurrentCell.ColumnIndex].Name);
        int toIndex = Math.Clamp(fromIndex + direction, 0, _currentResultSet.Columns.Count - 1);

        if (fromIndex < 0 || fromIndex == toIndex)
        {
            return;
        }

        _currentResultSet.MoveColumn(fromIndex, toIndex);
        LoadGrid();
        _grid.CurrentCell = _grid.Rows.Count > 0 && _grid.Columns.Count > 0 ? _grid.Rows[0].Cells[toIndex] : null;
        SaveWorkspaceDocument();
    }

    private void RenameCurrentColumn()
    {
        if (_loading || _currentResultSet is null || _grid.CurrentCell is null)
        {
            return;
        }

        string columnId = _grid.Columns[_grid.CurrentCell.ColumnIndex].Name;
        ResultColumn? column = _currentResultSet.Columns.FirstOrDefault(item => item.Id == columnId);

        if (column is null)
        {
            return;
        }

        column.Name = string.IsNullOrWhiteSpace(_columnNameBox.Text) ? "Column" : _columnNameBox.Text.Trim();
        _grid.Columns[_grid.CurrentCell.ColumnIndex].HeaderText = column.Name;
        SaveWorkspaceDocument();
        PreviewExport();
    }

    private void ApplySelectedCellKind()
    {
        if (_loading || _currentResultSet is null || _cellKindBox.SelectedItem is not CellValueKind kind)
        {
            return;
        }

        if (_grid.CurrentCell is not null)
        {
            string currentColumnId = _grid.Columns[_grid.CurrentCell.ColumnIndex].Name;
            ResultColumn? currentColumn = _currentResultSet.Columns.FirstOrDefault(column => column.Id == currentColumnId);
            if (currentColumn is not null)
            {
                currentColumn.DefaultKind = kind;
            }
        }

        foreach (DataGridViewCell gridCell in _grid.SelectedCells)
        {
            if (gridCell.RowIndex < 0 || gridCell.ColumnIndex < 0)
            {
                continue;
            }

            DataGridViewRow gridRow = _grid.Rows[gridCell.RowIndex];
            DataGridViewColumn gridColumn = _grid.Columns[gridCell.ColumnIndex];

            if (gridRow.Tag is not string rowId)
            {
                continue;
            }

            ResultRow? resultRow = _currentResultSet.Rows.FirstOrDefault(row => row.Id == rowId);
            if (resultRow is not null)
            {
                resultRow.GetCell(gridColumn.Name).Kind = kind;
            }
        }

        SaveWorkspaceDocument();
        PreviewExport();
    }

    private void RefreshInspector()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;

        if (_currentResultSet is null || _grid.CurrentCell is null)
        {
            _columnNameBox.Text = "";
            _cellKindBox.SelectedIndex = -1;
            _loading = false;
            return;
        }

        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];
        ResultColumn? resultColumn = _currentResultSet.Columns.FirstOrDefault(column => column.Id == gridColumn.Name);
        _columnNameBox.Text = resultColumn?.Name ?? gridColumn.HeaderText;

        CellValue? cellValue = CurrentCellValue();
        _cellKindBox.SelectedItem = cellValue?.Kind ?? CellValueKind.String;

        _loading = false;
    }

    private CellValue? CurrentCellValue()
    {
        if (_currentResultSet is null || _grid.CurrentCell is null)
        {
            return null;
        }

        DataGridViewRow gridRow = _grid.Rows[_grid.CurrentCell.RowIndex];
        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];

        return gridRow.Tag is string rowId
            ? _currentResultSet.Rows.FirstOrDefault(row => row.Id == rowId)?.GetCell(gridColumn.Name)
            : null;
    }

    private List<string> GetSelectedRowIds()
    {
        HashSet<int> selectedIndexes = _grid.SelectedCells
            .Cast<DataGridViewCell>()
            .Where(cell => cell.RowIndex >= 0)
            .Select(cell => cell.RowIndex)
            .ToHashSet();

        foreach (DataGridViewRow selectedRow in _grid.SelectedRows)
        {
            selectedIndexes.Add(selectedRow.Index);
        }

        return selectedIndexes
            .OrderBy(index => index)
            .Select(index => _grid.Rows[index].Tag as string)
            .Where(rowId => rowId is not null)
            .Cast<string>()
            .ToList();
    }

    private IReadOnlyList<ResultRow> GetRowsForExport()
    {
        if (_currentResultSet is null)
        {
            return [];
        }

        SyncGridToModel();
        List<string> selectedRowIds = GetSelectedRowIds();

        if (selectedRowIds.Count == 0)
        {
            return _currentResultSet.Rows;
        }

        HashSet<string> selected = selectedRowIds.ToHashSet();
        return _currentResultSet.Rows.Where(row => selected.Contains(row.Id)).ToList();
    }

    private void PreviewExport()
    {
        if (_currentResultSet is null)
        {
            _previewBox.Text = "";
            return;
        }

        _previewBox.Text = _exporter.Export(_currentResultSet, GetRowsForExport());
    }

    private void ExportToClipboard()
    {
        PreviewExport();

        try
        {
            Clipboard.SetText(_previewBox.Text);
            _statusLabel.Text = "Exported C# initializer to clipboard.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"Clipboard export failed: {ex.Message}";
        }

        SaveWorkspaceDocument();
    }

    private void SaveWorkspaceDocument()
    {
        if (_loading)
        {
            return;
        }

        SyncGridToModel();
        _document.ActiveResultSetId = _currentResultSet?.Id;
        _store.Save(_workspacePath, _document);
    }

    private void GridMouseDown(object? sender, MouseEventArgs e)
    {
        DataGridView.HitTestInfo hit = _grid.HitTest(e.X, e.Y);
        _dragRowIndex = hit.RowIndex;
        _dragStartPoint = e.Location;
    }

    private void GridMouseMove(object? sender, MouseEventArgs e)
    {
        if ((e.Button & MouseButtons.Left) != MouseButtons.Left || _dragRowIndex < 0)
        {
            return;
        }

        Rectangle dragBounds = new(
            _dragStartPoint.X - SystemInformation.DragSize.Width / 2,
            _dragStartPoint.Y - SystemInformation.DragSize.Height / 2,
            SystemInformation.DragSize.Width,
            SystemInformation.DragSize.Height);

        if (!dragBounds.Contains(e.Location))
        {
            _grid.DoDragDrop(_dragRowIndex, DragDropEffects.Move);
        }
    }

    private void GridDragDrop(object? sender, DragEventArgs e)
    {
        if (_currentResultSet is null || e.Data?.GetData(typeof(int)) is not int fromIndex)
        {
            return;
        }

        Point clientPoint = _grid.PointToClient(new Point(e.X, e.Y));
        int toIndex = _grid.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

        if (toIndex < 0 || fromIndex == toIndex)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet.MoveRow(fromIndex, toIndex);
        LoadGrid();
        SelectGridRow(toIndex);
        SaveWorkspaceDocument();
    }

    private void SelectGridRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count || _grid.Columns.Count == 0)
        {
            return;
        }

        _grid.ClearSelection();
        _grid.CurrentCell = _grid.Rows[rowIndex].Cells[0];
        _grid.Rows[rowIndex].Selected = true;
    }
}
