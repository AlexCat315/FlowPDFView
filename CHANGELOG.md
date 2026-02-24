# Changelog

## Unreleased

- Refactor: extract `Abstractions` into new project `Flow.PDFView.Core` and add project reference from `Flow.PDFView`.
- API: Make `IPdfView` inherit `IPdfViewCore` to start incremental migration path.
- Docs: Add/fix bilingual (zh/en) XML comments for core abstraction types.
- Fix: Correct malformed XML documentation in `IPdfViewCore` interfaces and `PdfModels` to remove doc warnings.
- Build: Set `SkipValidateMauiImplicitPackageReferences` in `Flow.PDFView.Core` to suppress MA002.

Notes:
- Solution builds successfully after these changes, remaining warnings are mainly nullability and a hiding warning for rotation property.
