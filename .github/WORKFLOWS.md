# GitHub Actions Workflows

This repository uses GitHub Actions for continuous integration and deployment.

## Workflows

### 1. Build and Publish (`build-and-publish.yml`)

**Purpose**: Builds, tests, packages, and publishes to NuGet.org

**Triggers**:
- Push to `main` branch → Build and test only
- Push tag `v*.*.*` → Build, test, package, publish, and create GitHub release
- Manual trigger via workflow_dispatch

**Jobs**:

1. **build-and-test**
   - Restores dependencies
   - Builds in Release configuration
   - Runs all unit tests
   - Uploads build artifacts

2. **pack** (only on main or tags)
   - Creates NuGet packages (.nupkg)
   - Uploads packages as artifacts

3. **publish-nuget** (only on version tags)
   - Downloads packages
   - Publishes to NuGet.org
   - Creates summary in GitHub Actions UI

4. **create-github-release** (only on version tags)
   - Downloads packages
   - Creates GitHub release
   - Attaches NuGet packages
   - Uses RELEASE_NOTES.md if available

### 2. PR Check (`pr-check.yml`)

**Purpose**: Validates pull requests before merging

**Triggers**:
- Pull request opened/updated against `main`

**Checks**:
- Build verification
- All tests passing
- Code formatting verification
- Code coverage report (uploaded to Codecov)
- Test results report

## Setup Instructions

### 1. Configure Repository Secrets

Go to: `Settings` → `Secrets and variables` → `Actions`

**Required Secret**:

- **NUGET_API_KEY**
  - Get from: https://www.nuget.org/account/apikeys
  - Scope: Push new packages
  - Glob pattern: `SpecWorks.MarkMyWord*`
  - Expiration: Set appropriately

**How to create NuGet API key**:

1. Log in to https://www.nuget.org
2. Go to API Keys: https://www.nuget.org/account/apikeys
3. Click "Create"
4. Settings:
   - Key Name: `GitHub Actions - MarkMyWord`
   - Glob Pattern: `SpecWorks.MarkMyWord*`
   - Scopes: ✓ Push new packages and package versions
   - Expiration: 365 days (or as needed)
5. Copy the key (you won't see it again!)
6. Add to GitHub repository secrets as `NUGET_API_KEY`

### 2. Enable GitHub Actions

1. Go to repository `Settings` → `Actions` → `General`
2. Enable "Allow all actions and reusable workflows"
3. Workflow permissions: "Read and write permissions"
4. Save changes

### 3. Configure Branch Protection (Optional but Recommended)

`Settings` → `Branches` → `Add rule` for `main`:

- ✓ Require status checks to pass before merging
  - ✓ Require branches to be up to date
  - Select: `Build and Test PR`
- ✓ Require pull request reviews before merging
- ✓ Require linear history

## Publishing a New Version

### Automatic Publishing (Recommended)

1. **Update version numbers** in both `.csproj` files:
   ```xml
   <Version>0.3.0</Version>
   ```

2. **Update RELEASE_NOTES.md** with changes

3. **Commit and push** to main:
   ```bash
   git add .
   git commit -m "Bump version to 0.3.0"
   git push origin main
   ```

4. **Create and push a version tag**:
   ```bash
   git tag v0.3.0
   git push origin v0.3.0
   ```

5. **GitHub Actions will automatically**:
   - Build the project
   - Run tests
   - Create NuGet packages
   - Publish to NuGet.org
   - Create a GitHub release with packages attached

### Manual Publishing

If you need to publish manually:

```bash
# Build and test
dotnet build -c Release
dotnet test -c Release

# Create packages
dotnet pack dotnet/src/MarkMyWord/MarkMyWord.csproj -c Release -o packages
dotnet pack dotnet/src/MarkMyWord.CLI/MarkMyWord.CLI.csproj -c Release -o packages

# Publish
dotnet nuget push packages/*.nupkg --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
```

## Versioning Strategy

### Semantic Versioning (SemVer)

Format: `MAJOR.MINOR.PATCH` (e.g., `v0.2.0`)

- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Pre-release Versions

For alpha/beta/rc releases:

```bash
git tag v0.3.0-beta.1
git push origin v0.3.0-beta.1
```

The workflow automatically marks GitHub releases as "pre-release" for:
- Tags containing `alpha`
- Tags containing `beta`
- Tags containing `rc`

### Version Workflow

```
feature branch → PR → main (0.2.0) → tag v0.2.1 → NuGet + GitHub Release
                                   → tag v0.3.0-beta.1 → NuGet (pre-release)
                                   → tag v0.3.0 → NuGet + GitHub Release
```

## Workflow Outputs

### Build Artifacts

Available for 7 days after build:
- Library binaries
- CLI binaries

### NuGet Packages

Available for 30 days:
- SpecWorks.MarkMyWord.{version}.nupkg
- SpecWorks.MarkMyWord.{version}.snupkg
- SpecWorks.MarkMyWord.CLI.{version}.nupkg

### GitHub Releases

Permanent:
- Release notes (from RELEASE_NOTES.md)
- NuGet packages attached
- Auto-generated release notes (if RELEASE_NOTES.md missing)
- Source code archives

## Monitoring

### Check Workflow Status

- Go to: `Actions` tab in GitHub
- View all workflow runs
- Click on individual runs for detailed logs

### NuGet Package Status

- Check: https://www.nuget.org/packages/SpecWorks.MarkMyWord
- Check: https://www.nuget.org/packages/SpecWorks.MarkMyWord.CLI

### Notifications

Configure notifications in GitHub:
- `Settings` → `Notifications`
- Watch: "Releases only" or "All activity"

## Troubleshooting

### Build Fails

1. Check .NET version matches (currently 9.0.x)
2. Verify all dependencies are restored
3. Check test results in workflow logs

### Tests Fail

1. Review test output in workflow logs
2. Run tests locally: `dotnet test`
3. Fix failing tests before merging

### Publishing Fails

**"Package already exists" error**:
- Version already published to NuGet
- Increment version number
- Create new tag

**"Unauthorized" error**:
- Check NUGET_API_KEY secret is set correctly
- Verify API key hasn't expired
- Ensure API key has push permissions

**"Missing required metadata" error**:
- Verify .csproj has all required NuGet properties
- Check README.md is included

### GitHub Release Fails

**"Resource not accessible" error**:
- Verify workflow has `contents: write` permission
- Check repository settings allow workflow write access

## Best Practices

### 1. Always Test Locally First

```bash
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release
```

### 2. Use Feature Branches

```
feature/syntax-highlighting → PR → main
bugfix/code-block-spacing → PR → main
```

### 3. Keep RELEASE_NOTES.md Updated

Update before creating a tag for better release documentation.

### 4. Use Draft Releases for Testing

1. Create tag with `-test` suffix: `v0.2.0-test`
2. Workflow creates draft release
3. Review before making public
4. Delete test tag when satisfied

### 5. Monitor Package Downloads

Check NuGet.org statistics to track adoption and identify issues.

## Advanced Usage

### Manual Workflow Trigger

Go to: `Actions` → `Build, Test, and Publish` → `Run workflow`

Options:
- Branch: Select branch
- Click "Run workflow"

### Skipping CI

Add to commit message:
```
[skip ci]
or
[ci skip]
```

Example:
```bash
git commit -m "Update documentation [skip ci]"
```

### Running Specific Jobs

Workflows are configured with job dependencies. You can't run individual jobs from UI, but you can:

1. Comment out jobs in workflow file temporarily
2. Use workflow_dispatch with inputs to control behavior
3. Create separate workflows for specific tasks

## Security Considerations

### API Key Protection

- ✓ Store API key as secret (never in code)
- ✓ Set expiration on API keys
- ✓ Use scoped keys (push only, specific packages)
- ✓ Rotate keys periodically

### Workflow Permissions

- Use minimum required permissions
- `contents: write` only for GitHub release job
- `packages: write` if publishing to GitHub Packages

### Dependency Security

- Dependabot enabled for security updates
- Review dependency updates before merging
- Pin action versions: `@v4` not `@latest`

## Future Enhancements

Potential workflow improvements:

- [ ] Add code coverage badges
- [ ] Publish to GitHub Packages
- [ ] Multi-platform builds (Windows, macOS, Linux)
- [ ] Performance benchmarking
- [ ] Documentation generation and deployment
- [ ] Automated changelog generation
- [ ] Slack/Discord notifications
- [ ] Release preview comments on PRs

## Support

For workflow issues:
- Check workflow logs in Actions tab
- Review this documentation
- Open issue on GitHub

---

**Maintained by**: SpecWorks
**Last Updated**: 2026-01-14
