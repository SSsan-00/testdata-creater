# TestDataCreater

C# WinForms tool for editing query result-set test data and exporting it as HashMap initializer code.

## Features

- Manage multiple result-set workspaces.
- Add, delete, and reorder rows and columns.
- Drag rows in the grid and drag columns by their headers.
- Persist workspace data under the user's application data folder.
- Preview generated C# code before exporting.
- Copy exported code to the clipboard.
- Export one row as `new HashMap`.
- Export multiple rows as `new HashMap<HashMap>` with outer keys renumbered from `0`.
- Choose value kinds such as string, numeric values, bool, DateTime, Guid, null, DBNull.Value, and custom C# expressions.

## Requirements

- .NET 9 SDK
- Windows environment for running the WinForms UI

## Build

Run this from the repository root:

```bash
dotnet build TestDataCreater.sln
```

On macOS or Linux, the project can be restored and built as a Windows-targeting project, but the WinForms UI must be run on Windows.

## Test

MSTest tests live under `tests/` and are intended for repository development only.

```bash
dotnet test TestDataCreater.sln
```

The bootstrap project does not expand test source files.

## Bootstrap without downloading the repository

Copy [TestDataCreater.Bootstrap.csproj](TestDataCreater.Bootstrap.csproj) into an empty folder and run:

```bash
dotnet build TestDataCreater.Bootstrap.csproj
dotnet build TestDataCreater.sln
```

The bootstrap build expands the solution, Core project, WinForms project, and source files into the current folder. It does not expand MSTest source files.

If files already exist, the bootstrap leaves them unchanged. To regenerate them, run:

```bash
dotnet build TestDataCreater.Bootstrap.csproj /p:BootstrapOverwrite=true
```
