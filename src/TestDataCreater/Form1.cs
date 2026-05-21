using TestDataCreater.Core;

namespace TestDataCreater;

public partial class Form1 : Form
{
    private const string RowCommandColumnName = "__row_command";
    private const string AddColumnCommandColumnName = "__add_column";
    private const string ColumnCommandRowTag = "__column_commands";
    private const int GridCommandColumnWidth = 72;
    private const int WorkspacePanelMinWidth = 280;

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
    private TextBox? _activeEditingTextBox;
    private string? _editingColumnHeaderId;
    private bool _previewSplitterInitialized;

    private readonly ListBox _workspaceList = new();
    private readonly DataGridView _grid = new();
    private readonly TextBox _previewBox = new();
    private readonly TextBox _columnNameBox = new();
    private readonly TextBox _columnHeaderEditBox = new();
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
        Text = "HashMap Maker";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1100, 680);
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
            ColumnCount = 9,
            Padding = new Padding(10, 8, 10, 8)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));

        Label appNameLabel = new()
        {
            Text = "HashMap Maker",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font(Font, FontStyle.Bold)
        };

        ConfigureInspectorControls();

        header.Controls.Add(appNameLabel, 0, 0);
        header.Controls.Add(new Label { Text = "列名", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 0);
        header.Controls.Add(_columnNameBox, 3, 0);
        header.Controls.Add(new Label { Text = "型", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 4, 0);
        header.Controls.Add(_cellKindBox, 5, 0);
        header.Controls.Add(CreateHeaderButton("CSVインポート", ImportCsvFromFile), 6, 0);
        header.Controls.Add(CreateHeaderButton("コピー", ExportToClipboard), 7, 0);
        header.Controls.Add(CreateHeaderButton("クリア", ClearWorkspace), 8, 0);

        return header;
    }

    private Control CreateBodyPanel()
    {
        SplitContainer outer = new()
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            SplitterDistance = WorkspacePanelMinWidth,
            Panel1MinSize = WorkspacePanelMinWidth
        };
        outer.HandleCreated += (_, _) => outer.BeginInvoke((MethodInvoker)(() => EnsureWorkspacePanelWidth(outer)));
        outer.SizeChanged += (_, _) => EnsureWorkspacePanelWidth(outer);

        outer.Panel1.Controls.Add(CreateWorkspacePanel());
        outer.Panel2.Controls.Add(CreateEditorPanel());
        return outer;
    }

    private static void EnsureWorkspacePanelWidth(SplitContainer outer)
    {
        if (outer.Width <= 0 || outer.SplitterDistance >= WorkspacePanelMinWidth)
        {
            return;
        }

        int maximumSplitterDistance = outer.Width - outer.Panel2MinSize - outer.SplitterWidth;
        if (maximumSplitterDistance >= WorkspacePanelMinWidth)
        {
            outer.SplitterDistance = WorkspacePanelMinWidth;
        }
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
        workspacePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

        Label workspaceLabel = new()
        {
            Text = "ワークスペース",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _workspaceList.Dock = DockStyle.Fill;
        _workspaceList.DisplayMember = nameof(ResultSet.Name);
        _workspaceList.SelectedIndexChanged += (_, _) => SelectWorkspaceFromList();
        _workspaceList.DoubleClick += (_, _) => RenameSelectedWorkspace();

        TableLayoutPanel workspaceCommands = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(0, 4, 0, 0)
        };
        workspaceCommands.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        workspaceCommands.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        workspaceCommands.Controls.Add(CreateWorkspaceButton("追加", AddWorkspace), 0, 0);
        workspaceCommands.Controls.Add(CreateWorkspaceButton("削除", DeleteWorkspace), 1, 0);

        workspacePanel.Controls.Add(workspaceLabel, 0, 0);
        workspacePanel.Controls.Add(_workspaceList, 0, 1);
        workspacePanel.Controls.Add(workspaceCommands, 0, 2);
        return workspacePanel;
    }

    private Control CreateEditorPanel()
    {
        SplitContainer editor = new()
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 7,
            BackColor = SystemColors.Control
        };
        editor.SizeChanged += (_, _) => ConfigurePreviewSplitter(editor);

        ConfigureGrid();
        ConfigurePreviewBox();

        editor.Panel1.Padding = new Padding(4, 4, 8, 3);
        editor.Panel2.Padding = new Padding(4, 3, 8, 6);
        editor.Panel1.Controls.Add(_grid);
        editor.Panel2.Controls.Add(CreatePreviewPanel());
        return editor;
    }

    private void ConfigurePreviewSplitter(SplitContainer editor)
    {
        int minimumGridHeight = 240;
        int minimumPreviewHeight = 110;

        if (editor.Height <= minimumGridHeight + minimumPreviewHeight + editor.SplitterWidth)
        {
            return;
        }

        editor.Panel1MinSize = minimumGridHeight;
        editor.Panel2MinSize = minimumPreviewHeight;

        if (_previewSplitterInitialized)
        {
            return;
        }

        _previewSplitterInitialized = true;
        int previewHeight = 180;
        editor.SplitterDistance = Math.Max(
            minimumGridHeight,
            editor.Height - previewHeight - editor.SplitterWidth);
    }

    private void ConfigureInspectorControls()
    {
        _columnNameBox.Dock = DockStyle.Fill;
        _columnNameBox.Margin = new Padding(0, 2, 10, 2);
        _columnNameBox.TextChanged += (_, _) => RenameCurrentColumn();

        _cellKindBox.Dock = DockStyle.Fill;
        _cellKindBox.Margin = new Padding(0, 2, 10, 2);
        _cellKindBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _cellKindBox.Format += (_, e) =>
        {
            if (e.ListItem is CellValueKind kind)
            {
                e.Value = CellValueKindRules.ToCSharpTypeName(kind);
            }
        };
        _cellKindBox.DataSource = Enum.GetValues<CellValueKind>();
        _cellKindBox.SelectedIndexChanged += (_, _) => ApplySelectedCellKind();
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
        GroupBox previewPanel = new()
        {
            Text = "プレビュー",
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 18, 8, 8)
        };
        previewPanel.Controls.Add(_previewBox);
        return previewPanel;
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

    private static Button CreateWorkspaceButton(string text, Action action)
    {
        Button button = CreateButton(text, action);
        button.AutoSize = false;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(0, 0, 6, 4);
        return button;
    }

    private static Button CreateHeaderButton(string text, Action action)
    {
        Button button = CreateButton(text, action);
        button.AutoSize = false;
        button.Dock = DockStyle.Fill;
        button.Margin = new Padding(4, 2, 0, 2);
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
        _grid.RowHeadersWidth = 48;
        _grid.ColumnHeadersHeight = 32;
        _grid.RowTemplate.Height = 30;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.RowHeadersDefaultCellStyle.BackColor = SystemColors.Control;

        _grid.CellBeginEdit += GridCellBeginEdit;
        _grid.CellClick += GridCellClick;
        _grid.CellEndEdit += GridCellEndEdit;
        _grid.CellValueChanged += GridCellValueChanged;
        _grid.ColumnHeaderMouseClick += GridColumnHeaderMouseClick;
        _grid.EditingControlShowing += GridEditingControlShowing;
        _grid.SelectionChanged += (_, _) => RefreshInspector();
        _grid.ColumnDisplayIndexChanged += (_, _) =>
        {
            if (!_loading)
            {
                BeginInvoke(() =>
                {
                    EnsureSpecialColumnDisplayOrder();
                    SyncColumnOrderFromGrid();
                    PreviewExport();
                    SaveWorkspaceDocument();
                });
            }
        };
        _grid.MouseDown += GridMouseDown;
        _grid.MouseMove += GridMouseMove;
        _grid.DragOver += (_, e) => e.Effect = DragDropEffects.Move;
        _grid.DragDrop += GridDragDrop;

        _columnHeaderEditBox.Visible = false;
        _columnHeaderEditBox.BorderStyle = BorderStyle.FixedSingle;
        _columnHeaderEditBox.LostFocus += (_, _) => FinishColumnHeaderEdit(commit: true);
        _columnHeaderEditBox.KeyDown += ColumnHeaderEditBoxKeyDown;
        _grid.Controls.Add(_columnHeaderEditBox);
    }

    private void LoadWorkspaceDocument()
    {
        _document = _store.Load(_workspacePath);

        if (_document.ResultSets.Count == 0)
        {
            _document = WorkspaceDocument.CreateDefault();
        }

        ResultSet? active = _document.ResultSets.FirstOrDefault(resultSet => resultSet.Id == _document.ActiveResultSetId)
            ?? _document.ResultSets.FirstOrDefault();

        if (active is not null)
        {
            _currentResultSet = active;
            ReloadWorkspaceList(active);
            LoadGrid();
        }
    }

    private void ReloadWorkspaceList(ResultSet? selectedResultSet = null)
    {
        _loading = true;
        _workspaceList.BeginUpdate();
        try
        {
            _workspaceList.Items.Clear();

            foreach (ResultSet resultSet in _document.ResultSets)
            {
                _workspaceList.Items.Add(resultSet);
            }

            if (selectedResultSet is not null && _document.ResultSets.Contains(selectedResultSet))
            {
                _workspaceList.SelectedItem = selectedResultSet;
            }
        }
        finally
        {
            _workspaceList.EndUpdate();
            _loading = false;
        }
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
        FinishColumnHeaderEdit(commit: false);
        _grid.Columns.Clear();
        _grid.Rows.Clear();

        if (_currentResultSet is null)
        {
            _loading = false;
            return;
        }

        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = RowCommandColumnName,
            HeaderText = "行",
            Width = GridCommandColumnWidth,
            Frozen = true,
            ReadOnly = true,
            FlatStyle = FlatStyle.Flat,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = SystemColors.Control
            },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        _grid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = AddColumnCommandColumnName,
            HeaderText = "列",
            Width = GridCommandColumnWidth,
            Frozen = true,
            ReadOnly = true,
            FlatStyle = FlatStyle.Flat,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = SystemColors.Control
            },
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

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

        AddColumnCommandRow();

        foreach (ResultRow resultRow in _currentResultSet.Rows)
        {
            int rowIndex = _grid.Rows.Add();
            DataGridViewRow gridRow = _grid.Rows[rowIndex];
            gridRow.Tag = resultRow.Id;
            gridRow.HeaderCell.Value = GridRowIndexToModelIndex(rowIndex).ToString();
            gridRow.Cells[RowCommandColumnName].Value = "- 行";
            if (_grid.Columns[AddColumnCommandColumnName] is { } addColumnCommandColumn)
            {
                int addColumnCommandIndex = addColumnCommandColumn.Index;
                gridRow.Cells[addColumnCommandIndex] = new DataGridViewButtonCell
                {
                    Value = "コピー",
                    FlatStyle = FlatStyle.Flat
                };
                gridRow.Cells[addColumnCommandIndex].ReadOnly = true;
                gridRow.Cells[addColumnCommandIndex].Style.BackColor = SystemColors.Control;
            }

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

    private void AddColumnCommandRow()
    {
        int rowIndex = _grid.Rows.Add();
        DataGridViewRow row = _grid.Rows[rowIndex];
        row.Tag = ColumnCommandRowTag;
        row.ReadOnly = true;
        row.Frozen = true;
        row.Height = 32;
        row.DefaultCellStyle.BackColor = SystemColors.Control;
        row.DefaultCellStyle.ForeColor = SystemColors.ControlText;
        row.HeaderCell.Value = "";
        row.Cells[RowCommandColumnName].Value = "+ 行";
        row.Cells[AddColumnCommandColumnName].Value = "+ 列";

        foreach (DataGridViewColumn column in _grid.Columns)
        {
            if (IsDataColumn(column))
            {
                row.Cells[column.Index] = new DataGridViewButtonCell
                {
                    Value = "- 列",
                    FlatStyle = FlatStyle.Flat
                };
                row.Cells[column.Name].Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
                row.Cells[column.Name].Style.BackColor = SystemColors.Control;
            }
        }
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
                if (!IsDataColumn(gridColumn))
                {
                    continue;
                }

                CellValue cellValue = resultRow.GetCell(gridColumn.Name);
                cellValue.Text = Convert.ToString(gridRow.Cells[gridColumn.Index].Value) ?? "";
            }
        }
    }

    private void SyncColumnOrderFromGrid()
    {
        if (_loading || _currentResultSet is null)
        {
            return;
        }

        List<string> orderedIds = _grid.Columns
            .Cast<DataGridViewColumn>()
            .Where(IsDataColumn)
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.Name)
            .ToList();

        if (orderedIds.Count != _currentResultSet.Columns.Count)
        {
            return;
        }

        _currentResultSet.Columns = orderedIds
            .Select(id => _currentResultSet.Columns.First(column => column.Id == id))
            .ToList();
    }

    private void EnsureSpecialColumnDisplayOrder()
    {
        if (_grid.Columns[RowCommandColumnName] is { } rowCommandColumn && rowCommandColumn.DisplayIndex != 0)
        {
            rowCommandColumn.DisplayIndex = 0;
        }

        if (_grid.Columns[AddColumnCommandColumnName] is { } addColumnCommandColumn &&
            addColumnCommandColumn.DisplayIndex != 1)
        {
            addColumnCommandColumn.DisplayIndex = 1;
        }
    }

    private void GridCellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
    {
        e.Cancel = !IsEditableDataCell(e.RowIndex, e.ColumnIndex);
    }

    private void GridCellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        HandleGridCommandCell(e.RowIndex, e.ColumnIndex);
    }

    private void GridColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex < 0 || !IsDataColumn(_grid.Columns[e.ColumnIndex]))
        {
            return;
        }

        BeginColumnHeaderEdit(e.ColumnIndex);
    }

    private void GridCellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (!IsEditableDataCell(e.RowIndex, e.ColumnIndex))
        {
            return;
        }

        SyncSingleCell(e.RowIndex, e.ColumnIndex);
        RefreshInspector();
        PreviewExport();
        SaveWorkspaceDocument();
    }

    private void GridCellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (_loading || !IsEditableDataCell(e.RowIndex, e.ColumnIndex))
        {
            return;
        }

        SyncSingleCell(e.RowIndex, e.ColumnIndex);
        RefreshInspector();
        PreviewExport();
    }

    private void GridEditingControlShowing(object? sender, DataGridViewEditingControlShowingEventArgs e)
    {
        if (_activeEditingTextBox is not null)
        {
            _activeEditingTextBox.TextChanged -= ActiveEditingTextBoxTextChanged;
        }

        _activeEditingTextBox = e.Control as TextBox;

        if (_activeEditingTextBox is not null)
        {
            _activeEditingTextBox.TextChanged += ActiveEditingTextBoxTextChanged;
        }
    }

    private void ActiveEditingTextBoxTextChanged(object? sender, EventArgs e)
    {
        if (_loading || sender is not TextBox textBox || _grid.CurrentCell is null ||
            !IsEditableDataCell(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex))
        {
            return;
        }

        SyncSingleCell(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex, textBox.Text);
        RefreshInspector();
        PreviewExport();
    }

    private void HandleGridCommandCell(int rowIndex, int columnIndex)
    {
        if (_currentResultSet is null)
        {
            return;
        }

        DataGridViewColumn column = _grid.Columns[columnIndex];
        object? rowTag = _grid.Rows[rowIndex].Tag;

        if (rowTag as string == ColumnCommandRowTag)
        {
            if (column.Name == RowCommandColumnName)
            {
                AddRow();
            }
            else if (column.Name == AddColumnCommandColumnName)
            {
                AddColumn();
            }
            else if (IsDataColumn(column))
            {
                DeleteColumn(column.Name);
            }

            return;
        }

        if (IsDataGridRowIndex(rowIndex) && column.Name == RowCommandColumnName)
        {
            DeleteRowAtGridIndex(rowIndex);
            return;
        }

        if (IsDataGridRowIndex(rowIndex) && column.Name == AddColumnCommandColumnName)
        {
            CopyRowAtGridIndex(rowIndex);
        }
    }

    private void SyncSingleCell(int rowIndex, int columnIndex, string? editedText = null)
    {
        if (_currentResultSet is null || !IsEditableDataCell(rowIndex, columnIndex))
        {
            return;
        }

        DataGridViewRow gridRow = _grid.Rows[rowIndex];
        DataGridViewColumn gridColumn = _grid.Columns[columnIndex];

        if (gridRow.Tag is not string rowId)
        {
            return;
        }

        ResultRow? resultRow = _currentResultSet.Rows.FirstOrDefault(row => row.Id == rowId);
        if (resultRow is null)
        {
            return;
        }

        CellValue cellValue = resultRow.GetCell(gridColumn.Name);
        cellValue.Text = editedText ?? Convert.ToString(gridRow.Cells[columnIndex].Value) ?? "";
        cellValue.Kind = CellValueKindRules.CoerceToAllowedKind(cellValue.Kind, cellValue.Text);
    }

    private void DeleteRowAtGridIndex(int rowIndex)
    {
        if (_currentResultSet is null || !IsDataGridRowIndex(rowIndex) || _grid.Rows[rowIndex].Tag is not string rowId)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet.RemoveRow(rowId);
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void CopyRowAtGridIndex(int rowIndex)
    {
        if (_currentResultSet is null || !IsDataGridRowIndex(rowIndex))
        {
            return;
        }

        int sourceIndex = GridRowIndexToModelIndex(rowIndex);
        if (sourceIndex < 0 || sourceIndex >= _currentResultSet.Rows.Count)
        {
            return;
        }

        SyncGridToModel();
        ResultRow sourceRow = _currentResultSet.Rows[sourceIndex];
        ResultRow copiedRow = new();

        foreach (ResultColumn column in _currentResultSet.Columns)
        {
            CellValue sourceCell = sourceRow.GetCell(column.Id, column.DefaultKind);
            copiedRow.SetCell(column.Id, new CellValue(sourceCell.Kind, sourceCell.Text));
        }

        int insertIndex = sourceIndex + 1;
        _currentResultSet.Rows.Insert(insertIndex, copiedRow);
        LoadGrid();
        SelectGridRow(insertIndex);
        SaveWorkspaceDocument();
    }

    private void DeleteColumn(string columnId)
    {
        if (_currentResultSet is null)
        {
            return;
        }

        SyncGridToModel();
        _currentResultSet.RemoveColumn(columnId);
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private bool IsEditableDataCell(int rowIndex, int columnIndex)
    {
        return IsDataGridRowIndex(rowIndex) &&
            columnIndex >= 0 &&
            columnIndex < _grid.Columns.Count &&
            IsDataColumn(_grid.Columns[columnIndex]);
    }

    private bool IsDataGridRowIndex(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= _grid.Rows.Count)
        {
            return false;
        }

        string? tag = _grid.Rows[rowIndex].Tag as string;
        return tag is not null && tag != ColumnCommandRowTag;
    }

    private static bool IsDataColumn(DataGridViewColumn column)
    {
        return column.Name != RowCommandColumnName && column.Name != AddColumnCommandColumnName;
    }

    private static int GridRowIndexToModelIndex(int gridRowIndex)
    {
        return gridRowIndex - 1;
    }

    private void AddWorkspace()
    {
        SyncGridToModel();
        ResultSet resultSet = new($"Result Set {_document.ResultSets.Count + 1}");
        resultSet.AddColumn("COLUMN1");
        resultSet.AddRow();
        _document.ResultSets.Add(resultSet);
        _document.ActiveResultSetId = resultSet.Id;
        _currentResultSet = resultSet;
        ReloadWorkspaceList(resultSet);
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void RenameSelectedWorkspace()
    {
        if (_workspaceList.SelectedItem is not ResultSet resultSet)
        {
            return;
        }

        string? newName = PromptWorkspaceName(resultSet.Name);
        if (newName is null)
        {
            return;
        }

        resultSet.Name = string.IsNullOrWhiteSpace(newName) ? "Result Set" : newName.Trim();
        ReloadWorkspaceList(resultSet);
        SaveWorkspaceDocument();
    }

    private string? PromptWorkspaceName(string currentName)
    {
        using Form dialog = new()
        {
            Text = "ワークスペース名",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(380, 118),
            Font = Font
        };

        TextBox nameBox = new()
        {
            Dock = DockStyle.Top,
            Text = currentName,
            Margin = new Padding(0, 0, 0, 12)
        };

        Button okButton = new()
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Width = 88
        };

        Button cancelButton = new()
        {
            Text = "キャンセル",
            DialogResult = DialogResult.Cancel,
            Width = 88
        };

        FlowLayoutPanel commands = new()
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 36
        };
        commands.Controls.Add(cancelButton);
        commands.Controls.Add(okButton);

        TableLayoutPanel content = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.Controls.Add(new Label { Text = "ワークスペース名", Dock = DockStyle.Fill }, 0, 0);
        content.Controls.Add(nameBox, 0, 1);
        content.Controls.Add(commands, 0, 2);

        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.Add(content);

        DialogResult result = dialog.ShowDialog(this);
        return result == DialogResult.OK ? nameBox.Text : null;
    }

    private void DeleteWorkspace()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        if (!ConfirmDestructiveAction(
            "ワークスペースの削除",
            $"ワークスペース「{_currentResultSet.Name}」を削除します。よろしいですか？"))
        {
            return;
        }

        _document.ResultSets.Remove(_currentResultSet);

        if (_document.ResultSets.Count == 0)
        {
            _document = WorkspaceDocument.CreateDefault();
        }

        _currentResultSet = _document.ResultSets[0];
        _document.ActiveResultSetId = _currentResultSet.Id;
        ReloadWorkspaceList(_currentResultSet);
        LoadGrid();
        SaveWorkspaceDocument();
    }

    private void ClearWorkspace()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        if (!ConfirmDestructiveAction(
            "入力内容のクリア",
            $"ワークスペース「{_currentResultSet.Name}」のグリッド入力内容をクリアして初期状態に戻します。よろしいですか？"))
        {
            return;
        }

        SyncGridToModel();
        if (!_document.ResetResultSetGrid(_currentResultSet.Id))
        {
            return;
        }

        ReloadWorkspaceList(_currentResultSet);
        LoadGrid();
        SaveWorkspaceDocument();
        _statusLabel.Text = $"ワークスペース「{_currentResultSet.Name}」の入力内容をクリアしました。";
    }

    private bool ConfirmDestructiveAction(string title, string message)
    {
        return MessageBox.Show(
            this,
            message,
            title,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.OK;
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
        if (_currentResultSet is null || _grid.CurrentCell is null || !IsDataGridRowIndex(_grid.CurrentCell.RowIndex))
        {
            return;
        }

        int fromIndex = GridRowIndexToModelIndex(_grid.CurrentCell.RowIndex);
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
        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];
        if (!IsDataColumn(gridColumn))
        {
            return;
        }

        string columnId = gridColumn.Name;
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
        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];
        if (!IsDataColumn(gridColumn))
        {
            return;
        }

        int fromIndex = _currentResultSet.Columns.FindIndex(column => column.Id == gridColumn.Name);
        int toIndex = Math.Clamp(fromIndex + direction, 0, _currentResultSet.Columns.Count - 1);

        if (fromIndex < 0 || fromIndex == toIndex)
        {
            return;
        }

        _currentResultSet.MoveColumn(fromIndex, toIndex);
        LoadGrid();
        SelectGridCell(0, toIndex);
        SaveWorkspaceDocument();
    }

    private void RenameCurrentColumn()
    {
        if (_loading || _currentResultSet is null || _grid.CurrentCell is null)
        {
            return;
        }

        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];
        if (!IsDataColumn(gridColumn))
        {
            return;
        }

        string columnId = gridColumn.Name;
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

    private void BeginColumnHeaderEdit(int columnIndex)
    {
        if (_currentResultSet is null || columnIndex < 0 || columnIndex >= _grid.Columns.Count)
        {
            return;
        }

        DataGridViewColumn gridColumn = _grid.Columns[columnIndex];
        if (!IsDataColumn(gridColumn))
        {
            return;
        }

        Rectangle headerBounds = _grid.GetCellDisplayRectangle(columnIndex, -1, cutOverflow: true);
        if (headerBounds.Width <= 8 || headerBounds.Height <= 8)
        {
            return;
        }

        _editingColumnHeaderId = gridColumn.Name;
        _columnHeaderEditBox.Bounds = new Rectangle(
            headerBounds.Left + 2,
            headerBounds.Top + 2,
            Math.Max(40, headerBounds.Width - 4),
            Math.Max(22, headerBounds.Height - 4));
        _columnHeaderEditBox.Text = gridColumn.HeaderText;
        _columnHeaderEditBox.Visible = true;
        _columnHeaderEditBox.BringToFront();
        _columnHeaderEditBox.Focus();
        _columnHeaderEditBox.SelectAll();
    }

    private void ColumnHeaderEditBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true;
            FinishColumnHeaderEdit(commit: true);
        }
        else if (e.KeyCode == Keys.Escape)
        {
            e.SuppressKeyPress = true;
            FinishColumnHeaderEdit(commit: false);
        }
    }

    private void FinishColumnHeaderEdit(bool commit)
    {
        if (_editingColumnHeaderId is null)
        {
            return;
        }

        string columnId = _editingColumnHeaderId;
        _editingColumnHeaderId = null;
        _columnHeaderEditBox.Visible = false;

        if (!commit || _currentResultSet is null)
        {
            return;
        }

        string columnName = string.IsNullOrWhiteSpace(_columnHeaderEditBox.Text)
            ? "Column"
            : _columnHeaderEditBox.Text.Trim();

        ResultColumn? column = _currentResultSet.Columns.FirstOrDefault(item => item.Id == columnId);
        if (column is null || _grid.Columns[columnId] is not { } gridColumn)
        {
            return;
        }

        column.Name = columnName;
        gridColumn.HeaderText = columnName;

        if (_grid.CurrentCell is not null && _grid.Columns[_grid.CurrentCell.ColumnIndex].Name == columnId)
        {
            _columnNameBox.Text = columnName;
        }

        SaveWorkspaceDocument();
        PreviewExport();
    }

    private void ApplySelectedCellKind()
    {
        if (_loading || _currentResultSet is null || _cellKindBox.SelectedItem is not CellValueKind kind)
        {
            return;
        }

        if (_grid.CurrentCell is not null && IsEditableDataCell(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex))
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
            if (!IsEditableDataCell(gridCell.RowIndex, gridCell.ColumnIndex))
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

        if (!IsEditableDataCell(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex))
        {
            _columnNameBox.Text = "";
            _cellKindBox.DataSource = Array.Empty<CellValueKind>();
            _loading = false;
            return;
        }

        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];
        ResultColumn? resultColumn = _currentResultSet.Columns.FirstOrDefault(column => column.Id == gridColumn.Name);
        _columnNameBox.Text = resultColumn?.Name ?? gridColumn.HeaderText;

        CellValue? cellValue = CurrentCellValue();
        UpdateCellKindChoices(cellValue);

        _loading = false;
    }

    private CellValue? CurrentCellValue()
    {
        if (_currentResultSet is null || _grid.CurrentCell is null ||
            !IsEditableDataCell(_grid.CurrentCell.RowIndex, _grid.CurrentCell.ColumnIndex))
        {
            return null;
        }

        DataGridViewRow gridRow = _grid.Rows[_grid.CurrentCell.RowIndex];
        DataGridViewColumn gridColumn = _grid.Columns[_grid.CurrentCell.ColumnIndex];

        return gridRow.Tag is string rowId
            ? _currentResultSet.Rows.FirstOrDefault(row => row.Id == rowId)?.GetCell(gridColumn.Name)
            : null;
    }

    private void UpdateCellKindChoices(CellValue? cellValue)
    {
        if (cellValue is null)
        {
            _cellKindBox.DataSource = Array.Empty<CellValueKind>();
            return;
        }

        CellValueKind allowedKind = CellValueKindRules.CoerceToAllowedKind(cellValue.Kind, cellValue.Text);
        if (allowedKind != cellValue.Kind)
        {
            cellValue.Kind = allowedKind;
        }

        _cellKindBox.DataSource = CellValueKindRules.GetAllowedKinds(cellValue.Text).ToList();
        _cellKindBox.SelectedItem = cellValue.Kind;
    }

    private List<string> GetSelectedRowIds()
    {
        HashSet<int> selectedIndexes = _grid.SelectedCells
            .Cast<DataGridViewCell>()
            .Where(cell => IsDataGridRowIndex(cell.RowIndex))
            .Select(cell => cell.RowIndex)
            .ToHashSet();

        foreach (DataGridViewRow selectedRow in _grid.SelectedRows)
        {
            if (IsDataGridRowIndex(selectedRow.Index))
            {
                selectedIndexes.Add(selectedRow.Index);
            }
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
        return _currentResultSet.Rows;
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

    private void ImportCsvFromFile()
    {
        if (_currentResultSet is null)
        {
            return;
        }

        using OpenFileDialog dialog = new()
        {
            Title = "CSVをインポート",
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SyncGridToModel();
            string csvText = File.ReadAllText(dialog.FileName);
            int importedRows = CsvResultSetImporter.ImportInto(_currentResultSet, csvText);
            LoadGrid();
            SaveWorkspaceDocument();
            _statusLabel.Text = $"{importedRows} 行のCSVデータをインポートしました。";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = $"CSVインポートに失敗しました: {ex.Message}";
        }
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
        if (hit.ColumnIndex >= 0 && _grid.Columns[hit.ColumnIndex].Name == AddColumnCommandColumnName)
        {
            _dragRowIndex = -1;
            return;
        }

        _dragRowIndex = IsDataGridRowIndex(hit.RowIndex) ? hit.RowIndex : -1;
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
        if (_currentResultSet is null || e.Data?.GetData(typeof(int)) is not int fromGridIndex ||
            !IsDataGridRowIndex(fromGridIndex))
        {
            return;
        }

        Point clientPoint = _grid.PointToClient(new Point(e.X, e.Y));
        int toGridIndex = _grid.HitTest(clientPoint.X, clientPoint.Y).RowIndex;

        if (toGridIndex < 0 || toGridIndex == fromGridIndex)
        {
            return;
        }

        int fromIndex = GridRowIndexToModelIndex(fromGridIndex);
        int toIndex = toGridIndex >= _grid.Rows.Count - 1
            ? _currentResultSet.Rows.Count - 1
            : Math.Clamp(GridRowIndexToModelIndex(toGridIndex), 0, _currentResultSet.Rows.Count - 1);

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

    private void SelectGridRow(int rowIndex)
    {
        int gridRowIndex = rowIndex + 1;
        int firstDataColumnIndex = _grid.Columns
            .Cast<DataGridViewColumn>()
            .Where(IsDataColumn)
            .OrderBy(column => column.DisplayIndex)
            .Select(column => column.Index)
            .FirstOrDefault(-1);

        if (gridRowIndex < 0 || gridRowIndex >= _grid.Rows.Count || firstDataColumnIndex < 0)
        {
            return;
        }

        _grid.ClearSelection();
        _grid.CurrentCell = _grid.Rows[gridRowIndex].Cells[firstDataColumnIndex];
        _grid.Rows[gridRowIndex].Selected = true;
    }

    private void SelectGridCell(int rowIndex, int columnIndex)
    {
        int gridRowIndex = rowIndex + 1;
        if (_currentResultSet is null || columnIndex < 0 || columnIndex >= _currentResultSet.Columns.Count)
        {
            return;
        }

        string columnId = _currentResultSet.Columns[columnIndex].Id;
        DataGridViewColumn? gridColumn = _grid.Columns[columnId];
        if (gridColumn is null)
        {
            return;
        }

        int gridColumnIndex = gridColumn.Index;

        if (gridRowIndex < 0 || gridRowIndex >= _grid.Rows.Count || gridColumnIndex < 0 || gridColumnIndex >= _grid.Columns.Count)
        {
            return;
        }

        _grid.ClearSelection();
        _grid.CurrentCell = _grid.Rows[gridRowIndex].Cells[gridColumnIndex];
    }
}
