# GitHub Actions Setup Guide

Complete guide to setting up automated builds, testing, and publishing for MarkMyWord.

## 📁 Files Created

### Workflow Files (`.github/workflows/`)

1. **build-and-publish.yml** - Main CI/CD pipeline
   - Builds and tests on every push
   - Creates packages on main branch
   - Publishes to NuGet on version tags
   - Creates GitHub releases

2. **pr-check.yml** - Pull request validation
   - Runs tests on PRs
   - Verifies code formatting
   - Reports test results
   - Generates coverage reports

### Documentation (`.github/`)

3. **WORKFLOWS.md** - Complete workflow documentation
   - Detailed setup instructions
   - Troubleshooting guide
   - Best practices
   - Security considerations

4. **workflows/README.md** - Quick reference guide
   - Common commands
   - Status links
   - Quick troubleshooting

### Scripts (`scripts/`)

5. **release.sh** - Bash release automation
6. **release.ps1** - PowerShell release automation
7. **validate-release.sh** - Pre-release validation

## 🚀 Quick Setup (5 Steps)

### Step 1: Add Workflow Files to Repository

```bash
git add .github/
git commit -m "Add GitHub Actions workflows"
git push origin main
```

### Step 2: Create NuGet API Key

1. Go to https://www.nuget.org/account/apikeys
2. Click "Create"
3. Configure:
   - **Key Name**: `GitHub Actions - MarkMyWord`
   - **Glob Pattern**: `SpecWorks.MarkMyWord*`
   - **Scopes**: ✓ Push new packages and package versions
   - **Expiration**: 365 days
4. Copy the generated key

### Step 3: Add Secret to GitHub

1. Go to repository: `Settings` → `Secrets and variables` → `Actions`
2. Click "New repository secret"
3. Name: `NUGET_API_KEY`
4. Value: Paste the API key from Step 2
5. Click "Add secret"

### Step 4: Enable GitHub Actions

1. Go to: `Settings` → `Actions` → `General`
2. Select: "Allow all actions and reusable workflows"
3. Workflow permissions: "Read and write permissions"
4. Save changes

### Step 5: Test the Workflow

Push a change to trigger the workflow:

```bash
# Make a small change
echo "" >> README.md

# Commit and push
git add README.md
git commit -m "Test GitHub Actions"
git push origin main

# Check workflow status
# Go to: Actions tab in GitHub
```

## 📊 Workflow Triggers

| Event | Workflow | Actions |
|-------|----------|---------|
| Push to main | build-and-publish | Build, test, create packages |
| Push tag `v*.*.*` | build-and-publish | Build, test, package, **publish to NuGet**, create GitHub release |
| Pull request | pr-check | Build, test, formatting check |
| Manual | build-and-publish | All actions (via workflow_dispatch) |

## 🏷️ Publishing a Release

### Automated Method (Recommended)

Use the release scripts:

**Linux/macOS:**
```bash
./scripts/release.sh 0.3.0
```

**Windows:**
```powershell
.\scripts\release.ps1 -Version 0.3.0
```

The script will:
1. ✓ Validate version format
2. ✓ Check you're on main branch
3. ✓ Verify no uncommitted changes
4. ✓ Update version in .csproj files
5. ✓ Prompt for RELEASE_NOTES.md
6. ✓ Commit changes
7. ✓ Create git tag
8. ✓ Show push instructions

Then push:
```bash
git push origin main
git push origin v0.3.0
```

### Manual Method

```bash
# 1. Update versions in both .csproj files
<Version>0.3.0</Version>

# 2. Update RELEASE_NOTES.md

# 3. Commit
git add .
git commit -m "Release v0.3.0"

# 4. Create tag
git tag v0.3.0

# 5. Push
git push origin main
git push origin v0.3.0
```

## 🔍 Monitoring

### Check Workflow Status

**In GitHub UI:**
1. Go to repository
2. Click "Actions" tab
3. View all workflow runs
4. Click individual runs for logs

**Direct Links:**
- All workflows: `https://github.com/spec-works/MarkMyWord/actions`
- Build workflow: `https://github.com/spec-works/MarkMyWord/actions/workflows/build-and-publish.yml`
- PR workflow: `https://github.com/spec-works/MarkMyWord/actions/workflows/pr-check.yml`

### Check NuGet Publication

- Library: `https://www.nuget.org/packages/SpecWorks.MarkMyWord`
- CLI: `https://www.nuget.org/packages/SpecWorks.MarkMyWord.CLI`

### Check GitHub Releases

`https://github.com/spec-works/MarkMyWord/releases`

## 🔒 Security Setup

### API Key Best Practices

✅ **Do:**
- Store API key as repository secret (never in code)
- Set expiration (365 days recommended)
- Use scoped keys (push only, specific package pattern)
- Rotate keys periodically
- Delete keys when compromised

❌ **Don't:**
- Commit API keys to repository
- Share keys via email/chat
- Use keys without expiration
- Give more permissions than needed

### Workflow Permissions

Current configuration:
- Default: `read` access to repository
- Publish job: `contents: write` for creating releases

### Dependabot (Recommended)

Enable for automatic security updates:

1. `Settings` → `Code security and analysis`
2. Enable "Dependabot alerts"
3. Enable "Dependabot security updates"
4. Create `.github/dependabot.yml`:

```yaml
version: 2
updates:
  - package-ecosystem: "nuget"
    directory: "/dotnet"
    schedule:
      interval: "weekly"

  - package-ecosystem: "github-actions"
    directory: "/"
    schedule:
      interval: "weekly"
```

## 🧪 Testing Workflows

### Test Without Publishing

Create a test tag with `-test` suffix:

```bash
git tag v0.2.0-test
git push origin v0.2.0-test
```

The workflow will:
- ✓ Build and test
- ✓ Create packages
- ✗ Skip NuGet publishing (only publishes on `v*.*.*` without suffix)

Delete test tag when done:
```bash
git tag -d v0.2.0-test
git push origin :refs/tags/v0.2.0-test
```

### Local Validation

Before pushing:

```bash
# Validate everything is ready
./scripts/validate-release.sh

# Build and test locally
cd dotnet
dotnet build -c Release
dotnet test -c Release

# Create packages locally
dotnet pack -c Release -o packages
```

## 📋 Pre-Release Checklist

Before creating a release tag:

- [ ] All tests passing locally
- [ ] Version updated in both .csproj files
- [ ] RELEASE_NOTES.md updated
- [ ] README.md updated (if needed)
- [ ] No uncommitted changes
- [ ] On main branch
- [ ] GitHub Actions enabled
- [ ] NUGET_API_KEY secret configured
- [ ] Previous release published successfully

Run validation script:
```bash
./scripts/validate-release.sh
```

## 🐛 Troubleshooting

### Workflow Doesn't Trigger

**Check:**
1. GitHub Actions enabled in settings
2. Workflow file syntax (YAML indentation)
3. Push includes workflow file changes
4. Branch protection rules

**Fix:**
```bash
# Re-enable Actions
Settings → Actions → General → Allow all actions

# Validate YAML syntax
cat .github/workflows/build-and-publish.yml | python -c 'import yaml, sys; yaml.safe_load(sys.stdin)'
```

### Build Fails

**Common causes:**
- Wrong .NET version
- Missing dependencies
- Test failures

**Fix:**
```bash
# Check locally first
dotnet build -c Release
dotnet test -c Release

# Check workflow logs
# Go to Actions tab → Failed run → View logs
```

### Tests Fail

**Check:**
```bash
# Run tests locally with details
dotnet test --verbosity detailed

# Run specific test
dotnet test --filter "TestName"

# Check test output in workflow logs
```

### Publish Fails

**"Package already exists"**
- Version already published
- Increment version number
- Create new tag

**"Unauthorized"**
```bash
# Check secret is set
Settings → Secrets → NUGET_API_KEY

# Verify API key hasn't expired
# Check on NuGet.org → API Keys

# Test API key locally
dotnet nuget push packages/*.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
```

**"Missing metadata"**
- Check .csproj has all required properties
- Verify README.md is included
- Check license expression is valid

### GitHub Release Fails

**"Resource not accessible"**
```bash
# Check permissions
Settings → Actions → General → Workflow permissions
# Set to: Read and write permissions
```

## 📈 Advanced Configuration

### Multi-Platform Builds

Add to workflow:

```yaml
strategy:
  matrix:
    os: [ubuntu-latest, windows-latest, macos-latest]
runs-on: ${{ matrix.os }}
```

### Code Coverage

Already included in pr-check.yml. View at:
- Codecov: Configure at https://codecov.io

### Automated Changelog

Add to workflow:

```yaml
- name: Generate changelog
  uses: TriPSs/conventional-changelog-action@v3
  with:
    github-token: ${{ secrets.GITHUB_TOKEN }}
```

### Slack Notifications

```yaml
- name: Slack notification
  uses: 8398a7/action-slack@v3
  with:
    status: ${{ job.status }}
    webhook_url: ${{ secrets.SLACK_WEBHOOK }}
```

## 🔄 Version Strategy

### Semantic Versioning

- **MAJOR**: Breaking changes (1.0.0 → 2.0.0)
- **MINOR**: New features (0.2.0 → 0.3.0)
- **PATCH**: Bug fixes (0.2.0 → 0.2.1)

### Pre-releases

- **Alpha**: `v0.3.0-alpha.1`
- **Beta**: `v0.3.0-beta.1`
- **RC**: `v0.3.0-rc.1`

Workflow automatically marks as pre-release.

### Example Version Flow

```
0.1.0 → 0.2.0-beta.1 → 0.2.0-beta.2 → 0.2.0-rc.1 → 0.2.0 → 0.2.1 → 0.3.0
```

## 📚 Additional Resources

- **GitHub Actions Documentation**: https://docs.github.com/actions
- **NuGet Publishing**: https://learn.microsoft.com/nuget/nuget-org/publish-a-package
- **Semantic Versioning**: https://semver.org
- **.NET CLI**: https://learn.microsoft.com/dotnet/core/tools

## 🆘 Support

**Issues with workflows?**
1. Check `.github/WORKFLOWS.md` for detailed documentation
2. Review workflow logs in Actions tab
3. Run validation script: `./scripts/validate-release.sh`
4. Open issue: https://github.com/spec-works/MarkMyWord/issues

---

**Setup complete!** 🎉

Your repository is now configured for automated CI/CD with GitHub Actions.
