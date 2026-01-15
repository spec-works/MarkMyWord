# Release automation script for MarkMyWord (PowerShell)
# Usage: .\scripts\release.ps1 -Version <version>
# Example: .\scripts\release.ps1 -Version 0.3.0

param(
    [Parameter(Mandatory=$true)]
    [string]$Version
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Preparing release v$Version" -ForegroundColor Green
Write-Host ""

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+(-[a-z0-9.]+)?$') {
    Write-Host "❌ Error: Invalid version format" -ForegroundColor Red
    Write-Host "Expected: MAJOR.MINOR.PATCH or MAJOR.MINOR.PATCH-prerelease"
    Write-Host "Examples: 0.3.0, 1.0.0-beta.1"
    exit 1
}

$Tag = "v$Version"

# Check if we're on main branch
$CurrentBranch = git rev-parse --abbrev-ref HEAD
if ($CurrentBranch -ne "main") {
    Write-Host "⚠️  Warning: Not on main branch (current: $CurrentBranch)" -ForegroundColor Yellow
    $Continue = Read-Host "Continue anyway? (y/N)"
    if ($Continue -ne "y" -and $Continue -ne "Y") {
        exit 1
    }
}

# Check for uncommitted changes
$Status = git status --porcelain
if ($Status) {
    Write-Host "❌ Error: You have uncommitted changes" -ForegroundColor Red
    git status --short
    exit 1
}

# Check if tag already exists
try {
    git rev-parse $Tag 2>$null
    Write-Host "❌ Error: Tag $Tag already exists" -ForegroundColor Red
    exit 1
}
catch {
    # Tag doesn't exist, continue
}

# Update version in .csproj files
Write-Host "📝 Updating version in .csproj files..." -ForegroundColor Cyan

$LibraryProject = "dotnet\src\MarkMyWord\MarkMyWord.csproj"
$CLIProject = "dotnet\src\MarkMyWord.CLI\MarkMyWord.CLI.csproj"

(Get-Content $LibraryProject) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $LibraryProject
(Get-Content $CLIProject) -replace '<Version>.*</Version>', "<Version>$Version</Version>" | Set-Content $CLIProject

# Check if RELEASE_NOTES.md exists
$ReleaseNotes = "dotnet\RELEASE_NOTES.md"
if (-not (Test-Path $ReleaseNotes)) {
    Write-Host "⚠️  Warning: RELEASE_NOTES.md not found" -ForegroundColor Yellow
    $Create = Read-Host "Create RELEASE_NOTES.md? (y/N)"
    if ($Create -eq "y" -or $Create -eq "Y") {
        $Date = Get-Date -Format "yyyy-MM-dd"
        @"
# Release Notes

## Version $Version ($Date)

### New Features

- TODO: Add features

### Bug Fixes

- TODO: Add fixes

### Breaking Changes

- None
"@ | Out-File -FilePath $ReleaseNotes -Encoding UTF8
        Write-Host "✏️  Please edit $ReleaseNotes with release details" -ForegroundColor Yellow
        Read-Host "Press Enter when done"
    }
}
else {
    Write-Host "✏️  Please review $ReleaseNotes" -ForegroundColor Yellow
    $Open = Read-Host "Open in notepad? (y/N)"
    if ($Open -eq "y" -or $Open -eq "Y") {
        notepad $ReleaseNotes
    }
}

# Show changes
Write-Host ""
Write-Host "📋 Changes to be committed:" -ForegroundColor Cyan
git diff $LibraryProject
git diff $CLIProject

Write-Host ""
$Commit = Read-Host "Commit these changes? (y/N)"
if ($Commit -ne "y" -and $Commit -ne "Y") {
    Write-Host "❌ Aborted" -ForegroundColor Red
    git checkout $LibraryProject
    git checkout $CLIProject
    exit 1
}

# Commit version bump
Write-Host "💾 Committing version bump..." -ForegroundColor Cyan
git add $LibraryProject
git add $CLIProject
if (Test-Path $ReleaseNotes) {
    git add $ReleaseNotes
}
git commit -m "Bump version to $Version"

# Create tag
Write-Host "🏷️  Creating tag $Tag..." -ForegroundColor Cyan
git tag -a $Tag -m "Release $Tag"

# Show summary
Write-Host ""
Write-Host "✅ Release prepared successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 Summary:" -ForegroundColor Cyan
Write-Host "   Version: $Version"
Write-Host "   Tag: $Tag"
Write-Host "   Branch: $CurrentBranch"
Write-Host ""
Write-Host "🚀 Next steps:" -ForegroundColor Yellow
Write-Host "   1. Review the commit and tag:"
Write-Host "      git log -1"
Write-Host "      git show $Tag"
Write-Host ""
Write-Host "   2. Push to GitHub:"
Write-Host "      git push origin $CurrentBranch"
Write-Host "      git push origin $Tag"
Write-Host ""
Write-Host "   3. Monitor GitHub Actions:"
Write-Host "      https://github.com/spec-works/MarkMyWord/actions"
Write-Host ""
Write-Host "   4. Verify NuGet publication:"
Write-Host "      https://www.nuget.org/packages/SpecWorks.MarkMyWord"
Write-Host "      https://www.nuget.org/packages/SpecWorks.MarkMyWord.CLI"
Write-Host ""
Write-Host "ℹ️  To undo (before pushing):" -ForegroundColor Blue
Write-Host "   git tag -d $Tag"
Write-Host "   git reset --hard HEAD~1"
