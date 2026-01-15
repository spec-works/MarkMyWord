# GitHub Actions Setup Checklist

## ✅ One-Time Setup

### 1. Repository Configuration
- [ ] Push workflow files to repository
  ```bash
  git add .github/
  git commit -m "Add GitHub Actions workflows"
  git push origin main
  ```

### 2. NuGet API Key
- [ ] Go to https://www.nuget.org/account/apikeys
- [ ] Create new API key:
  - Name: `GitHub Actions - MarkMyWord`
  - Glob: `SpecWorks.MarkMyWord*`
  - Scope: Push new packages
  - Expiration: 365 days
- [ ] Copy the API key

### 3. GitHub Secret
- [ ] Go to repo: `Settings` → `Secrets and variables` → `Actions`
- [ ] Click "New repository secret"
- [ ] Name: `NUGET_API_KEY`
- [ ] Paste API key
- [ ] Save

### 4. GitHub Actions Settings
- [ ] `Settings` → `Actions` → `General`
- [ ] Allow all actions and reusable workflows
- [ ] Workflow permissions: Read and write
- [ ] Save changes

### 5. Verify Setup
- [ ] Push a test commit to main
- [ ] Check Actions tab - workflow should run
- [ ] Verify build succeeds

## 📦 Release Checklist

Use this checklist for each release:

### Pre-Release
- [ ] All tests passing locally: `dotnet test -c Release`
- [ ] Code formatted: `dotnet format`
- [ ] No uncommitted changes: `git status`
- [ ] On main branch: `git branch --show-current`
- [ ] Pull latest: `git pull origin main`

### Version Update
- [ ] Update version in `MarkMyWord.csproj`
- [ ] Update version in `MarkMyWord.CLI.csproj`
- [ ] Update `RELEASE_NOTES.md`
- [ ] Update `README.md` (if needed)

### Quick Method - Use Script
- [ ] Run: `./scripts/release.sh X.Y.Z` (Linux/Mac)
- [ ] Or: `.\scripts\release.ps1 -Version X.Y.Z` (Windows)
- [ ] Review changes
- [ ] Confirm commit
- [ ] Tag created automatically

### Manual Method
- [ ] Commit: `git commit -m "Release vX.Y.Z"`
- [ ] Tag: `git tag vX.Y.Z`
- [ ] Verify: `git log -1`, `git show vX.Y.Z`

### Push Release
- [ ] Push commits: `git push origin main`
- [ ] Push tag: `git push origin vX.Y.Z`

### Monitor
- [ ] Watch Actions tab for workflow progress
- [ ] Verify workflow completes (takes ~3-5 minutes)
- [ ] Check NuGet.org for new packages:
  - https://www.nuget.org/packages/SpecWorks.MarkMyWord
  - https://www.nuget.org/packages/SpecWorks.MarkMyWord.CLI
- [ ] Verify GitHub release created:
  - https://github.com/spec-works/MarkMyWord/releases

### Verify Installation
- [ ] Test library: `dotnet add package SpecWorks.MarkMyWord --version X.Y.Z`
- [ ] Test CLI: `dotnet tool install --global SpecWorks.MarkMyWord.CLI --version X.Y.Z`
- [ ] Run CLI: `markmyword --version`

## 🔍 Quick Tests

### Test Locally Before Release
```bash
cd dotnet
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o packages
```

### Validate Release Readiness
```bash
./scripts/validate-release.sh
```

### Test Workflow Without Publishing
```bash
# Create test tag
git tag v0.2.0-test
git push origin v0.2.0-test

# Check Actions tab - workflow runs but doesn't publish

# Clean up
git tag -d v0.2.0-test
git push origin :refs/tags/v0.2.0-test
```

## 🚨 Troubleshooting

### Workflow Not Running
1. Check Actions enabled: `Settings` → `Actions`
2. Verify workflow file syntax (YAML)
3. Check branch name matches trigger

### Build Fails
1. Run locally first: `dotnet build -c Release`
2. Check workflow logs: Actions → Failed run
3. Fix errors, commit, push

### Publish Fails
1. Check `NUGET_API_KEY` secret exists
2. Verify API key not expired (NuGet.org)
3. Ensure version incremented (no duplicate)

### Release Not Created
1. Check workflow permissions: `Settings` → `Actions` → `General`
2. Ensure "Read and write" permissions enabled
3. Verify tag format: `v*.*.*` (e.g., `v0.2.0`)

## 📞 Support

- Detailed docs: `.github/WORKFLOWS.md`
- Full setup guide: `GITHUB_ACTIONS_SETUP.md`
- Issues: https://github.com/spec-works/MarkMyWord/issues

---

**Quick Commands**

```bash
# Check status
git status
git log -1

# Release (script method)
./scripts/release.sh 0.3.0

# Push release
git push origin main && git push origin v0.3.0

# Watch workflow
# Go to: https://github.com/spec-works/MarkMyWord/actions
```
