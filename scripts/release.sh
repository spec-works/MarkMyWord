#!/bin/bash
# Release automation script for MarkMyWord
# Usage: ./scripts/release.sh <version>
# Example: ./scripts/release.sh 0.3.0

set -e

if [ -z "$1" ]; then
    echo "❌ Error: Version number required"
    echo "Usage: ./scripts/release.sh <version>"
    echo "Example: ./scripts/release.sh 0.3.0"
    exit 1
fi

VERSION="$1"
TAG="v${VERSION}"

echo "🚀 Preparing release ${TAG}"
echo ""

# Validate version format
if ! [[ $VERSION =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[a-z0-9.]+)?$ ]]; then
    echo "❌ Error: Invalid version format"
    echo "Expected: MAJOR.MINOR.PATCH or MAJOR.MINOR.PATCH-prerelease"
    echo "Examples: 0.3.0, 1.0.0-beta.1"
    exit 1
fi

# Check if we're on main branch
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
if [ "$CURRENT_BRANCH" != "main" ]; then
    echo "⚠️  Warning: Not on main branch (current: ${CURRENT_BRANCH})"
    read -p "Continue anyway? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
fi

# Check for uncommitted changes
if [ -n "$(git status --porcelain)" ]; then
    echo "❌ Error: You have uncommitted changes"
    git status --short
    exit 1
fi

# Check if tag already exists
if git rev-parse "$TAG" >/dev/null 2>&1; then
    echo "❌ Error: Tag ${TAG} already exists"
    exit 1
fi

# Update version in .csproj files
echo "📝 Updating version in .csproj files..."
sed -i "s/<Version>.*<\/Version>/<Version>${VERSION}<\/Version>/g" dotnet/src/MarkMyWord/MarkMyWord.csproj
sed -i "s/<Version>.*<\/Version>/<Version>${VERSION}<\/Version>/g" dotnet/src/MarkMyWord.CLI/MarkMyWord.CLI.csproj

# Check if RELEASE_NOTES.md exists and prompt to edit
if [ ! -f "dotnet/RELEASE_NOTES.md" ]; then
    echo "⚠️  Warning: RELEASE_NOTES.md not found"
    read -p "Create RELEASE_NOTES.md? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        cat > dotnet/RELEASE_NOTES.md <<EOF
# Release Notes

## Version ${VERSION} ($(date +%Y-%m-%d))

### New Features

- TODO: Add features

### Bug Fixes

- TODO: Add fixes

### Breaking Changes

- None
EOF
        echo "✏️  Please edit dotnet/RELEASE_NOTES.md with release details"
        read -p "Press Enter when done..."
    fi
else
    echo "✏️  Please review dotnet/RELEASE_NOTES.md"
    read -p "Open in editor? (y/N): " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        ${EDITOR:-nano} dotnet/RELEASE_NOTES.md
    fi
fi

# Show changes
echo ""
echo "📋 Changes to be committed:"
git diff dotnet/src/MarkMyWord/MarkMyWord.csproj
git diff dotnet/src/MarkMyWord.CLI/MarkMyWord.CLI.csproj

echo ""
read -p "Commit these changes? (y/N): " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Aborted"
    git checkout dotnet/src/MarkMyWord/MarkMyWord.csproj dotnet/src/MarkMyWord.CLI/MarkMyWord.CLI.csproj
    exit 1
fi

# Commit version bump
echo "💾 Committing version bump..."
git add dotnet/src/MarkMyWord/MarkMyWord.csproj
git add dotnet/src/MarkMyWord.CLI/MarkMyWord.CLI.csproj
if [ -f "dotnet/RELEASE_NOTES.md" ]; then
    git add dotnet/RELEASE_NOTES.md
fi
git commit -m "Bump version to ${VERSION}"

# Create tag
echo "🏷️  Creating tag ${TAG}..."
git tag -a "$TAG" -m "Release ${TAG}"

# Show summary
echo ""
echo "✅ Release prepared successfully!"
echo ""
echo "📋 Summary:"
echo "   Version: ${VERSION}"
echo "   Tag: ${TAG}"
echo "   Branch: ${CURRENT_BRANCH}"
echo ""
echo "🚀 Next steps:"
echo "   1. Review the commit and tag:"
echo "      git log -1"
echo "      git show ${TAG}"
echo ""
echo "   2. Push to GitHub:"
echo "      git push origin ${CURRENT_BRANCH}"
echo "      git push origin ${TAG}"
echo ""
echo "   3. Monitor GitHub Actions:"
echo "      https://github.com/spec-works/MarkMyWord/actions"
echo ""
echo "   4. Verify NuGet publication:"
echo "      https://www.nuget.org/packages/SpecWorks.MarkMyWord"
echo "      https://www.nuget.org/packages/SpecWorks.MarkMyWord.CLI"
echo ""
echo "ℹ️  To undo (before pushing):"
echo "   git tag -d ${TAG}"
echo "   git reset --hard HEAD~1"
