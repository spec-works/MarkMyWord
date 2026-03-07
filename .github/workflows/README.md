# GitHub Actions Quick Reference

## 🚀 Publishing a New Version

```bash
# 1. Update version in .csproj files
# MarkMyWord.csproj and MarkMyWord.CLI.csproj
<Version>0.3.0</Version>

# 2. Update RELEASE_NOTES.md

# 3. Commit changes
git add .
git commit -m "Release v0.3.0"
git push

# 4. Create and push tag
git tag v0.3.0
git push origin v0.3.0

# ✅ Automated workflow will:
#    - Build and test
#    - Create NuGet packages
#    - Publish to NuGet.org
#    - Create GitHub release
```

## 📋 Workflow Files

| File | Purpose | Trigger |
|------|---------|---------|
| `build-and-publish.yml` | Build, test, publish | Push to main, tags `v*.*.*` |
| `pr-check.yml` | Validate PRs | Pull requests to main |

## 🔑 Required Secrets

Configure in: `Settings` → `Secrets and variables` → `Actions`

- **NUGET_API_KEY**: Get from https://www.nuget.org/account/apikeys

## 📊 Workflow Status

Check: https://github.com/spec-works/MarkMyWord/actions

## 📦 Published Packages

- Library: https://www.nuget.org/packages/SpecWorks.MarkMyWord
- CLI Tool: https://www.nuget.org/packages/SpecWorks.MarkMyWord.CLI

## 🛠️ Common Commands

### Local Build & Test
```bash
cd dotnet
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

### Create Packages Locally
```bash
dotnet pack -c Release -o packages
```

### Publish Manually
```bash
dotnet nuget push packages/*.nupkg \
  --api-key YOUR_KEY \
  --source https://api.nuget.org/v3/index.json
```

## 🐛 Troubleshooting

### Build Fails
- Check .NET 9.0 SDK installed
- Verify dependencies restored
- Review workflow logs

### Tests Fail
- Run locally: `dotnet test`
- Check test output in Actions
- Fix tests before pushing

### Publish Fails
- "Package exists": Bump version number
- "Unauthorized": Check NUGET_API_KEY
- "Metadata missing": Verify .csproj properties

## 📚 Full Documentation

See: [WORKFLOWS.md](WORKFLOWS.md)

## 🆘 Need Help?

Open issue: https://github.com/spec-works/MarkMyWord/issues/new
