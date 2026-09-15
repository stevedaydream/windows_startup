# Project conventions

- Target .NET 8 and nullable reference types.
- Keep UI views, view models, domain models, and Windows integration services separate.
- Use async APIs for filesystem and process operations that can block the UI.
- Treat registry and filesystem paths as untrusted input; surface actionable errors to the UI.
- Add focused unit tests for command parsing and reversible startup-entry transformations.
