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

## Line Endings

- Use `CRLF` line endings for all repository files.
- Preserve `CRLF` when editing existing files and normalize non-CRLF files to `CRLF` when touching them.
- After any file edit, explicitly verify the touched files still use `CRLF` and correct them immediately if they do not.

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
