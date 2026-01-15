#!/bin/bash
# Validate project is ready for release
# Usage: ./scripts/validate-release.sh

set -e

echo "🔍 Validating project for release..."
echo ""

ERRORS=0
WARNINGS=0

# Check .NET SDK
echo "📦 Checking .NET SDK..."
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo "   ✓ .NET SDK installed: $DOTNET_VERSION"

    if [[ $DOTNET_VERSION == 9.* ]]; then
        echo "   ✓ .NET 9.0 detected"
    else
        echo "   ⚠️  Warning: .NET 9.0 recommended (found: $DOTNET_VERSION)"
        WARNINGS=$((WARNINGS + 1))
    fi
else
    echo "   ❌ .NET SDK not found"
    ERRORS=$((ERRORS + 1))
fi
echo ""

# Check Git status
echo "📋 Checking Git status..."
if [ -d .git ]; then
    echo "   ✓ Git repository detected"

    BRANCH=$(git rev-parse --abbrev-ref HEAD)
    echo "   Current branch: $BRANCH"

    if [ -n "$(git status --porcelain)" ]; then
        echo "   ⚠️  Warning: Uncommitted changes detected"
        WARNINGS=$((WARNINGS + 1))
    else
        echo "   ✓ Working directory clean"
    fi
else
    echo "   ❌ Not a Git repository"
    ERRORS=$((ERRORS + 1))
fi
echo ""

# Check project files
echo "📁 Checking project files..."
if [ -f "dotnet/src/MarkMyWord/MarkMyWord.csproj" ]; then
    echo "   ✓ MarkMyWord.csproj found"
else
    echo "   ❌ MarkMyWord.csproj not found"
    ERRORS=$((ERRORS + 1))
fi

if [ -f "dotnet/src/MarkMyWord.CLI/MarkMyWord.CLI.csproj" ]; then
    echo "   ✓ MarkMyWord.CLI.csproj found"
else
    echo "   ❌ MarkMyWord.CLI.csproj not found"
    ERRORS=$((ERRORS + 1))
fi

if [ -f "dotnet/README.md" ]; then
    echo "   ✓ README.md found"
else
    echo "   ⚠️  Warning: README.md not found"
    WARNINGS=$((WARNINGS + 1))
fi

if [ -f "dotnet/RELEASE_NOTES.md" ]; then
    echo "   ✓ RELEASE_NOTES.md found"
else
    echo "   ⚠️  Warning: RELEASE_NOTES.md not found (recommended)"
    WARNINGS=$((WARNINGS + 1))
fi
echo ""

# Build project
echo "🔨 Building project..."
cd dotnet
if dotnet restore > /dev/null 2>&1; then
    echo "   ✓ Dependencies restored"
else
    echo "   ❌ Failed to restore dependencies"
    ERRORS=$((ERRORS + 1))
fi

if dotnet build --configuration Release --no-restore > /dev/null 2>&1; then
    echo "   ✓ Build succeeded"
else
    echo "   ❌ Build failed"
    ERRORS=$((ERRORS + 1))
fi
echo ""

# Run tests
echo "🧪 Running tests..."
if dotnet test --configuration Release --no-build --verbosity quiet > /dev/null 2>&1; then
    echo "   ✓ All tests passed"
else
    echo "   ❌ Tests failed"
    ERRORS=$((ERRORS + 1))
fi
echo ""

# Check version consistency
echo "🔢 Checking version consistency..."
LIB_VERSION=$(grep -oP '<Version>\K[^<]+' src/MarkMyWord/MarkMyWord.csproj | head -1)
CLI_VERSION=$(grep -oP '<Version>\K[^<]+' src/MarkMyWord.CLI/MarkMyWord.CLI.csproj | head -1)

echo "   Library version: $LIB_VERSION"
echo "   CLI version: $CLI_VERSION"

if [ "$LIB_VERSION" = "$CLI_VERSION" ]; then
    echo "   ✓ Versions match"
else
    echo "   ❌ Version mismatch!"
    ERRORS=$((ERRORS + 1))
fi

if [[ $LIB_VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-z0-9.]+)?$ ]]; then
    echo "   ✓ Version format valid"
else
    echo "   ❌ Invalid version format"
    ERRORS=$((ERRORS + 1))
fi
cd ..
echo ""

# Check GitHub Actions workflow
echo "🔄 Checking GitHub Actions..."
if [ -f ".github/workflows/build-and-publish.yml" ]; then
    echo "   ✓ Build and publish workflow found"
else
    echo "   ⚠️  Warning: Build and publish workflow not found"
    WARNINGS=$((WARNINGS + 1))
fi

if [ -f ".github/workflows/pr-check.yml" ]; then
    echo "   ✓ PR check workflow found"
else
    echo "   ⚠️  Warning: PR check workflow not found"
    WARNINGS=$((WARNINGS + 1))
fi
echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
    echo "✅ Project is ready for release!"
    echo ""
    echo "🚀 Next steps:"
    echo "   1. Run: ./scripts/release.sh <version>"
    echo "   2. Or manually:"
    echo "      - Update version in .csproj files"
    echo "      - Update RELEASE_NOTES.md"
    echo "      - Commit and push"
    echo "      - Create and push tag: git tag v<version>"
    exit 0
elif [ $ERRORS -eq 0 ]; then
    echo "⚠️  Project ready with $WARNINGS warning(s)"
    echo ""
    echo "Consider addressing warnings before release."
    exit 0
else
    echo "❌ Project not ready for release"
    echo "   Errors: $ERRORS"
    echo "   Warnings: $WARNINGS"
    echo ""
    echo "Please fix errors before releasing."
    exit 1
fi
