# LogCleaner

A utility to clean up log folders on development servers by removing directories and log files older than a specified number of days. Targets folders matching the pattern `\Log\YYYYMMDD`.

## Building

Requires .NET 8.0 SDK or later.

```bash
cd LogCleaner
dotnet build
```

## Usage

```bash
dotnet run --project LogCleaner -- --path <path> [--daysToKeep <days>] [--readkey]
```

### Options

- `-p, --path` (required): Path to search for log folders
- `-d, --daysToKeep` (optional): Number of days from today to keep log folders/files (default: 7)
- `-r, --readkey` (optional): Await a key press before exit (useful when running inside Visual Studio)

### Example

```bash
dotnet run --project LogCleaner -- --path "C:\MyApp\Logs" --daysToKeep 30
```

## License

MIT License - see [LICENSE](LICENSE) file for details.
