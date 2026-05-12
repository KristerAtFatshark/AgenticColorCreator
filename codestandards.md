## Curly Braces

Curly braces should be placed on new lines to facilitate easier visual block recognition.

Example:

```csharp
if (a == b)
{
	//code here
}
```

## Indentation Rules

- Use real tab characters for indentation in all repository files instead of groups of four spaces.
- Preserve tabs when editing existing files and correct space-based indentation to tabs when touching files.
- For new `*.cs`, `*.csproj`, and `*.xaml` files, use tab indentation from the start.
- Before completing a change, explicitly verify that every touched `*.cs`, `*.csproj`, and `*.xaml` file uses tab indentation for leading indentation.

## Line Endings

- Use `CRLF` line endings for all repository files.
- Preserve `CRLF` when editing existing files and normalize non-CRLF files to `CRLF` when touching them.
- For new files, write them with `CRLF` line endings from the start.
- After any file edit or file creation, explicitly verify the touched files still use `CRLF` and correct them immediately if they do not.

## Verification Rules

- Formatting verification is required, not optional.
- Before finishing a task that changes repository files, verify line endings and indentation for every touched file.
- If a touched file violates these formatting rules, fix it before considering the task complete.

## Naming Conventions

PascalCase is used for:
- Enums
- ClassNames
- MethodNames
- Constants
- Properties
- Delegate
- Public variables

camelCase is used for:
- Method arguments
- Local variables
- Fields in classes and structs

camelCase with a starting `_` is used for:
- Private variables
