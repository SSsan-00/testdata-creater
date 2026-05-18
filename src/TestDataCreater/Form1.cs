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
        Text = "TestDataCreater";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1024, 640);
        Font = new Font("Segoe UI", 9F);

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = SystemColors.Control
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Padding = new Padding(8, 0, 0, 0);

        root.Controls.Add(CreateHeaderPanel(), 0, 0);
        root.Controls.Add(CreateBodyPanel(), 0, 1);
        root.Controls.Add(_statusLabel, 0, 2);
        Controls.Add(root);

        FormClosing += (_, _) => SaveWorkspaceDocument();
    }

    private Control CreateHeaderPanel()
    {
        TableLayoutPanel header = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 6,
            Padding = new Padding(10, 8, 10, 8)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 96));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 8));

        Label nameLabel = new()
        {
            Text = "結果セット名",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _workspaceNameBox.Dock = DockStyle.Fill;
        _workspaceNameBox.Margin = new Padding(0, 2, 12, 2);
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

        header.Controls.Add(nameLabel, 0, 0);
        header.Controls.Add(_workspaceNameBox, 1, 0);
        header.Controls.Add(CreateButton("プレビュー", PreviewExport), 2, 0);
        header.Controls.Add(CreateButton("コピー", ExportToClipboard), 3, 0);
        header.Controls.Add(CreateButton("保存", SaveWorkspaceDocument), 4, 0);

        return header;
    }

    private Control CreateBodyPanel()
    {
        SplitContainer outer = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = 210
        };

        outer.Panel1.Controls.Add(CreateWorkspacePanel());
        outer.Panel2.Controls.Add(CreateEditorPanel());
        return outer;
    }

    private Control CreateWorkspacePanel()
    {
        TableLayoutPanel workspacePanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(8, 4, 6, 6)
        };
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));

        Label workspaceLabel = new()
        {
            Text = "ワークスペース",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _workspaceList.Dock = DockStyle.Fill;
        _workspaceList.DisplayMember = nameof(ResultSet.Name);
        _workspaceList.SelectedIndexChanged += (_, _) => SelectWorkspaceFromList();

        FlowLayoutPanel workspaceCommands = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        workspaceCommands.Controls.Add(CreateButton("追加", AddWorkspace));
        workspaceCommands.Controls.Add(CreateButton("削除", DeleteWorkspace));

        workspacePanel.Controls.Add(workspaceLabel, 0, 0);
        workspacePanel.Controls.Add(_workspaceList, 0, 1);
        workspacePanel.Controls.Add(workspaceCommands, 0, 2);
        return workspacePanel;
    }

    private Control CreateEditorPanel()
    {
        TableLayoutPanel editor = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(4, 4, 8, 6)
        };
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));

        ConfigureGrid();
        ConfigurePreviewBox();

        editor.Controls.Add(CreateGridCommandPanel(), 0, 0);
        editor.Controls.Add(_grid, 0, 1);
        editor.Controls.Add(CreatePreviewPanel(), 0, 2);
        return editor;
    }

    private Control CreateGridCommandPanel()
    {
        TableLayoutPanel commandPanel = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        commandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        commandPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380));

        FlowLayoutPanel editCommands = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        editCommands.Controls.Add(CreateButton("+ 行", AddRow));
        editCommands.Controls.Add(CreateButton("- 行", DeleteSelectedRows));
        editCommands.Controls.Add(CreateButton("↑ 行", () => MoveCurrentRow(-1)));
        editCommands.Controls.Add(CreateButton("↓ 行", () => MoveCurrentRow(1)));
        editCommands.Controls.Add(CreateButton("+ 列", AddColumn));
        editCommands.Controls.Add(CreateButton("- 列", DeleteCurrentColumn));
        editCommands.Controls.Add(CreateButton("← 列", () => MoveCurrentColumn(-1)));
        editCommands.Controls.Add(CreateButton("→ 列", () => MoveCurrentColumn(1)));

        _columnNameBox.Dock = DockStyle.Fill;
        _columnNameBox.Margin = new Padding(0, 20, 8, 20);
        _columnNameBox.TextChanged += (_, _) => RenameCurrentColumn();

        _cellKindBox.Dock = DockStyle.Fill;
        _cellKindBox.Margin = new Padding(0, 20, 0, 20);
        _cellKindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _cellKindBox.DataSource = Enum.GetValues<CellValueKind>();
        _cellKindBox.SelectedIndexChanged += (_, _) => ApplySelectedCellKind();

        TableLayoutPanel inspector = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1
        };
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        inspector.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

        inspector.Controls.Add(new Label { Text = "列名", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        inspector.Controls.Add(_columnNameBox, 1, 0);
        inspector.Controls.Add(new Label { Text = "型", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        inspector.Controls.Add(_cellKindBox, 3, 0);

        commandPanel.Controls.Add(editCommands, 0, 0);
        commandPanel.Controls.Add(inspector, 1, 0);
        return commandPanel;
    }

    private void ConfigurePreviewBox()
    {
        _previewBox.Dock = DockStyle.Fill;
        _previewBox.Multiline = true;
        _previewBox.ScrollBars = ScrollBars.Both;
        _previewBox.WordWrap = false;
        _previewBox.Font = new Font(FontFamily.GenericMonospace, 10);
        _previewBox.ReadOnly = true;
        _previewBox.BackColor = SystemColors.Window;
    }

    private Control CreatePreviewPanel()
    {
        TabControl tabs = new()
        {
            Dock = DockStyle.Fill
        };

        TabPage previewPage = new("C# プレビュー")
        {
            Padding = new Padding(4)
        };
        previewPage.Controls.Add(_previewBox);
        tabs.TabPages.Add(previewPage);
        return tabs;
    }

    private static Button CreateButton(string text, Action action)
    {
        Button button = new()
        {
            Text = text,
            AutoSize = true,
            Height = 28,
            Margin = new Padding(3, 5, 3, 5),
            Padding = new Padding(8, 0, 8, 0)
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
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
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
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Width = Math.Max(140, column.Name.Length * 12)
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
        SelectGridRow(_currentResultSet.Rows.Count - 1);
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
        SelectGridCell(0, _currentResultSet.Columns.Count - 1);
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
            _statusLabel.Text = "C# 初期化コードをクリップボードへコピーしました。";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"クリップボードへのコピーに失敗しました: {ex.Message}";
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

    private void SelectGridCell(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count || columnIndex < 0 || columnIndex >= _grid.Columns.Count)
        {
            return;
        }

        _grid.ClearSelection();
        _grid.CurrentCell = _grid.Rows[rowIndex].Cells[columnIndex];
    }
}
