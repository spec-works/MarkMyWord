# Publishing to NuGet - Instructions

## Packages Ready for Publication

### Version 0.2.0

Three packages have been created and are ready for publishing:

1. **SpecWorks.MarkMyWord.0.2.0.nupkg** (33 KB)
   - Main library package
   - Includes syntax highlighting for JSON, TypeSpec, and Bash

2. **SpecWorks.MarkMyWord.0.2.0.snupkg** (18 KB)
   - Symbol package for debugging support

3. **SpecWorks.MarkMyWord.CLI.0.2.0.nupkg** (2.3 MB)
   - .NET global tool for command-line usage

### Location

All packages are located at:
```
C:\src\github\spec-works\MarkMyWord\dotnet\packages\
```

## Publishing Steps

### Prerequisites

1. **NuGet Account**: Ensure you have a NuGet.org account
2. **API Key**: Get your API key from https://www.nuget.org/account/apikeys

### Option 1: Publish via Command Line

```bash
# Navigate to packages directory
cd C:\src\github\spec-works\MarkMyWord\dotnet\packages

# Set your API key (one time only)
dotnet nuget push *.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json

# Or publish each package individually:
dotnet nuget push SpecWorks.MarkMyWord.0.2.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push SpecWorks.MarkMyWord.0.2.0.snupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
dotnet nuget push SpecWorks.MarkMyWord.CLI.0.2.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

### Option 2: Publish via NuGet.org Web UI

1. Go to https://www.nuget.org/packages/manage/upload
2. Upload `SpecWorks.MarkMyWord.0.2.0.nupkg`
3. Upload `SpecWorks.MarkMyWord.0.2.0.snupkg`
4. Upload `SpecWorks.MarkMyWord.CLI.0.2.0.nupkg`
5. Follow the on-screen verification steps

## Post-Publication

### Verification

After publishing, verify the packages are available:

```bash
# Search for the library package
dotnet nuget search SpecWorks.MarkMyWord

# Install to test
dotnet add package SpecWorks.MarkMyWord --version 0.2.0

# Install CLI tool
dotnet tool install --global SpecWorks.MarkMyWord.CLI --version 0.2.0
```

### Documentation

The packages include:
- ✅ README.md (embedded in packages)
- ✅ License information (MIT)
- ✅ Project URL (GitHub)
- ✅ Repository URL (GitHub)
- ✅ Release notes (RELEASE_NOTES.md)
- ✅ Comprehensive tags for discoverability

### Package Metadata

**Library Package:**
- ID: SpecWorks.MarkMyWord
- Version: 0.2.0
- Description: A .NET library for converting CommonMark markdown to Microsoft Word (.docx) documents with syntax highlighting
- Tags: markdown, commonmark, word, docx, openxml, converter, document, office, syntax-highlighting, code

**CLI Package:**
- ID: SpecWorks.MarkMyWord.CLI
- Version: 0.2.0
- Command Name: `markmyword`
- Description: Command-line tool for converting CommonMark markdown files to Microsoft Word documents with syntax highlighting
- Tags: markdown, commonmark, word, docx, converter, cli, tool, command-line, syntax-highlighting

## Release Checklist

- [x] Version bumped to 0.2.0
- [x] README.md updated with syntax highlighting documentation
- [x] RELEASE_NOTES.md created
- [x] All 29 unit tests passing
- [x] Built in Release configuration
- [x] NuGet packages created successfully
- [x] Symbol package (.snupkg) included for library
- [x] CLI tool configured as global tool (`markmyword` command)
- [ ] Published to NuGet.org
- [ ] Verified package installation
- [ ] Created GitHub release tag (v0.2.0)

## GitHub Release

After publishing to NuGet, create a GitHub release:

1. Go to: https://github.com/spec-works/MarkMyWord/releases/new
2. Tag: `v0.2.0`
3. Title: `Version 0.2.0 - Syntax Highlighting`
4. Description: Copy from RELEASE_NOTES.md
5. Attach packages (optional):
   - SpecWorks.MarkMyWord.0.2.0.nupkg
   - SpecWorks.MarkMyWord.CLI.0.2.0.nupkg

## Installation Examples

After publication, users can install with:

### Library
```bash
dotnet add package SpecWorks.MarkMyWord
```

### CLI Tool
```bash
dotnet tool install --global SpecWorks.MarkMyWord.CLI
```

Then use it:
```bash
markmyword convert -i document.md
```

## Support

For issues or questions after publication:
- GitHub Issues: https://github.com/spec-works/MarkMyWord/issues
- Documentation: https://github.com/spec-works/MarkMyWord

## Notes

- The packages are targeting .NET 9.0
- Symbol package enables source code debugging
- CLI tool is configured to work as a global tool
- All dependencies are properly declared in the packages
- Package icons can be added in future versions via `<PackageIcon>` property

---

**Ready to publish!** All packages have been built, tested, and are ready for NuGet.org.
