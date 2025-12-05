# IniFileClass

A thread-safe INI file reader/writer for .NET with support for comments and Shift-JIS encoding.

## Features

- **Thread-Safe**: Process-level file locking prevents concurrent access issues
- **Comment Preservation**: Maintains comments when reading and writing INI files
- **Shift-JIS Support**: Default encoding for Japanese environments (Code Page 932)
- **Quoted Values**: Handles values containing spaces via quote wrapping
- **Cross-Platform .NET**: Works with both .NET Framework 4.8+ and .NET Core 3.1+

## Supported Frameworks

- **.NET Framework 4.8+**
- **.NET Core 3.1+**
- **.NET 5.0+** (including .NET 6.0, 7.0, 8.0)

## Installation

### From Source
Copy `IniFileClass.cs` and `IniValue.cs` to your project.

### NuGet (Future)
```
dotnet add package IniFileUtils
```

## Dependencies

**For .NET Core/.NET 5+:**
- `System.Text.Encoding.CodePages` (4.7.0 or later) - Required for Shift-JIS support

**For .NET Framework:**
- No additional dependencies required

## Usage

### Basic Read/Write

```csharp
using IniFileUtils;

// Create and write INI file
var ini = new IniFileClass("app.ini");
ini.SetValue("Database", "Host", "localhost");
ini.SetValue("Database", "Port", "5432");
ini.SetValue("Database", "Password", "my pass", useQuotes: true);
ini.Save();

// Read from INI file
var ini = new IniFileClass("app.ini");
string host = ini.GetValue("Database", "Host");  // "localhost"
string port = ini.GetValue("Database", "Port");  // "5432"
```

### Working with Comments

```csharp
var ini = new IniFileClass("app.ini");

// Add section comment
ini.SetComment("Database", "Database configuration");

// Add key comment
ini.AddKeyComment("Database", "Host", "Database server hostname");

// Remove comment
ini.RemoveComment("Database", "Database configuration");

// Clear all comments
ini.ClearAllComments();

ini.Save();
```

### Removing Keys/Sections

```csharp
var ini = new IniFileClass("app.ini");

// Remove a key
ini.RemoveKey("Database", "Host");

// Remove a key and section if empty
ini.RemoveKey("OldSection", "OldKey", removeEmptySection: true);

// Remove entire section
ini.RemoveSection("OldSettings");

ini.Save();
```

### Using Different Encoding

```csharp
// Use UTF-8 instead of Shift-JIS
var ini = new IniFileClass("app.ini", Encoding.UTF8);
ini.SetValue("Settings", "Name", "テスト");
ini.Save();
```

### Thread-Safe Multi-threaded Access

```csharp
// Multiple threads can safely access the same INI file
var ini1 = new IniFileClass("app.ini");
var ini2 = new IniFileClass("app.ini");

Task.Run(() => {
    ini1.SetValue("Settings", "Key1", "Value1");
    ini1.Save();  // Automatically locks during write
});

Task.Run(() => {
    ini2.SetValue("Settings", "Key2", "Value2");
    ini2.Save();  // Waits for ini1 to complete
});
```

## Thread Safety

- **Process-Level**: Thread-safe within a single process using internal file-based locking
- **Multi-Process**: Not suitable for concurrent access from multiple processes
- **Limitation**: Locking is per-file; different files can be accessed concurrently

For multi-process scenarios, consider implementing additional file-locking mechanisms (e.g., OS-level file locks).

## API Reference

### IniFileClass Constructor

```csharp
public IniFileClass(string filePath = null, Encoding encoding = null)
```

- `filePath`: Path to the INI file. If null, creates an empty in-memory structure
- `encoding`: Character encoding (defaults to Shift-JIS)

### Methods

#### Read Operations

| Method | Description |
|--------|-------------|
| `GetValue(section, key)` | Returns the value string or null if not found |

#### Write Operations

| Method | Description |
|--------|-------------|
| `SetValue(section, key, value, useQuotes)` | Sets a key-value pair (creates section if needed) |
| `RemoveKey(section, key, removeEmptySection)` | Removes a key, optionally removing the section if empty |
| `RemoveSection(section)` | Removes an entire section |
| `Save(filePath)` | Saves to file (thread-safe). Optional new file path. |

#### Comment Operations

| Method | Description |
|--------|-------------|
| `SetComment(section, comment, prefix)` | Adds a section comment (prefix: `;`, `#`, or `//`) |
| `RemoveComment(section, comment)` | Removes a section comment |
| `AddKeyComment(section, key, comment)` | Adds a comment to a specific key |
| `ClearAllComments()` | Removes all comments from the INI file |

## INI File Format

```ini
; Section comment
[Database]
; Key comment
Host=localhost
Port=5432
Password="my pass word"

[Logging]
Level=Info
```

## Limitations

- **Encoding**: Assumes uniform encoding throughout the file
- **Multi-Process**: Not safe for concurrent multi-process access
- **Data Types**: All values are strings; type conversion must be handled by the caller
- **Key Order**: Comment association is section-level; individual key-level comments are preserved

## Thread Safety Notes

- Locking is **per-file path**. Multiple instances of `IniFileClass` pointing to the same file are safely synchronized.
- Locking is **within-process only**. Multiple separate processes accessing the same file are not protected.
- Locks are automatically cleaned up when the object is garbage collected.

## License

Free

## Contributing

Contributions, bug reports, and feature requests are welcome on GitHub.

## Version History

### 1.0.0 (Initial Release)
- Core INI file read/write functionality
- Thread-safe process-level locking
- Comment preservation
- Shift-JIS encoding support
- .NET Framework 4.8+ and .NET Core 3.1+ compatibility

