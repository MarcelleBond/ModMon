# ModMon Deployment Guide

This guide explains how to deploy ModMon as a .NET tool to GitHub Packages.

## Prerequisites

1. **GitHub Repository**: https://github.com/MarcelleBond/ModMon.git
2. **GitHub Personal Access Token (PAT)** with the following permissions:
   - `write:packages` - To publish packages
   - `read:packages` - To download packages
   - `repo` - For private repositories

## Creating a GitHub Personal Access Token

1. Go to GitHub Settings → Developer settings → Personal access tokens → Tokens (classic)
2. Click "Generate new token (classic)"
3. Give it a descriptive name (e.g., "ModMon NuGet Publishing")
4. Select scopes:
   - ✅ `write:packages`
   - ✅ `read:packages`
   - ✅ `repo` (if repository is private)
5. Click "Generate token"
6. **Copy the token immediately** (you won't be able to see it again)

## Publishing Methods

### Method 1: Automatic Publishing via GitHub Actions (Recommended)

The repository includes a GitHub Actions workflow that automatically publishes the package when you create a version tag.

#### Using Git Tags

```bash
# Ensure all changes are committed
git add .
git commit -m "Release v1.0.0"

# Create and push a version tag
git tag v1.0.0
git push origin v1.0.0
```

The workflow will automatically:
1. Build the project
2. Pack the NuGet package with version 1.0.0
3. Publish to GitHub Packages
4. Create a GitHub Release with the package attached

#### Manual Workflow Trigger

You can also manually trigger the workflow from GitHub:

1. Go to your repository on GitHub
2. Click "Actions" tab
3. Select "Publish NuGet Package" workflow
4. Click "Run workflow"
5. Enter the version number (e.g., `1.0.0`)
6. Click "Run workflow"

### Method 2: Manual Publishing from Command Line

If you prefer to publish manually:

```bash
# 1. Set your GitHub PAT as an environment variable
# Windows (PowerShell)
$env:GITHUB_TOKEN="your_github_pat_here"

# Linux/macOS
export GITHUB_TOKEN="your_github_pat_here"

# 2. Build and pack the project
dotnet pack --configuration Release -p:PackageVersion=1.0.0

# 3. Publish to GitHub Packages
dotnet nuget push ./nupkg/ModMon.1.0.0.nupkg \
  --api-key $env:GITHUB_TOKEN \
  --source "https://nuget.pkg.github.com/MarcelleBond/index.json"
```

## Version Management

### Semantic Versioning

Follow semantic versioning (MAJOR.MINOR.PATCH):

- **MAJOR** (1.x.x): Breaking changes
- **MINOR** (x.1.x): New features, backward compatible
- **PATCH** (x.x.1): Bug fixes, backward compatible

### Updating Version

Update the version in `modmon.csproj`:

```xml
<Version>1.1.0</Version>
```

Then create a corresponding git tag:

```bash
git tag v1.1.0
git push origin v1.1.0
```

## Installation for End Users

### Configure NuGet

Users need to configure their NuGet to access GitHub Packages:

**Windows**: `%APPDATA%\NuGet\NuGet.Config`  
**Linux/macOS**: `~/.nuget/NuGet/NuGet.Config`

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github-modmon" value="https://nuget.pkg.github.com/MarcelleBond/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github-modmon>
      <add key="Username" value="GITHUB_USERNAME" />
      <add key="ClearTextPassword" value="GITHUB_PAT" />
    </github-modmon>
  </packageSourceCredentials>
</configuration>
```

### Install the Tool

```bash
# Install specific version
dotnet tool install --global ModMon --version 1.0.0

# Install latest version
dotnet tool install --global ModMon

# Update to latest version
dotnet tool update --global ModMon

# Uninstall
dotnet tool uninstall --global ModMon
```

## Troubleshooting

### "401 Unauthorized" Error

**Problem**: Authentication failed when publishing or installing.

**Solution**:
1. Verify your GitHub PAT has the correct permissions (`write:packages` for publishing, `read:packages` for installing)
2. Ensure the PAT hasn't expired
3. Check that the username in NuGet.Config matches your GitHub username

### "Package Already Exists" Error

**Problem**: Trying to publish a version that already exists.

**Solution**:
1. Increment the version number in `modmon.csproj`
2. Create a new git tag with the new version
3. You cannot overwrite existing package versions on GitHub Packages

### "Unable to Load Package Source"

**Problem**: NuGet can't connect to GitHub Packages.

**Solution**:
1. Verify the source URL is correct: `https://nuget.pkg.github.com/MarcelleBond/index.json`
2. Check your internet connection
3. Ensure GitHub Packages is accessible (not blocked by firewall)

### Package Not Found After Publishing

**Problem**: Package published successfully but can't be installed.

**Solution**:
1. Wait a few minutes for GitHub to index the package
2. Verify the package is visible in your repository's "Packages" section
3. Ensure your NuGet.Config has the correct source URL
4. Clear NuGet cache: `dotnet nuget locals all --clear`

## Repository Setup Checklist

- [ ] Repository created at https://github.com/MarcelleBond/ModMon.git
- [ ] GitHub Actions enabled in repository settings
- [ ] GitHub PAT created with `write:packages` and `read:packages` permissions
- [ ] `.github/workflows/publish-nuget.yml` workflow file committed
- [ ] `modmon.csproj` configured with package metadata
- [ ] `README.md` with installation instructions
- [ ] `LICENSE` file included
- [ ] `.gitignore` configured to exclude build artifacts and nupkg files

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/publish-nuget.yml`) performs:

1. **Checkout**: Clones the repository
2. **Setup .NET**: Installs .NET SDK 10.0
3. **Restore**: Restores NuGet dependencies
4. **Build**: Builds in Release configuration
5. **Version**: Extracts version from git tag or workflow input
6. **Pack**: Creates NuGet package with specified version
7. **Publish**: Pushes package to GitHub Packages
8. **Release**: Creates GitHub Release with package artifact (for tag pushes)

## Best Practices

1. **Always test locally** before publishing:
   ```bash
   dotnet pack --configuration Release
   dotnet tool install --global --add-source ./nupkg ModMon
   modmon --help
   ```

2. **Use semantic versioning** consistently

3. **Create release notes** for each version in GitHub Releases

4. **Tag releases** with descriptive messages:
   ```bash
   git tag -a v1.0.0 -m "Initial release with core features"
   ```

5. **Keep documentation updated** with each release

6. **Test installation** from GitHub Packages before announcing new versions

## Security Notes

- **Never commit** your GitHub PAT to the repository
- Use GitHub Secrets for storing PAT in GitHub Actions (automatically available as `secrets.GITHUB_TOKEN`)
- Regularly rotate your GitHub PAT
- Use the minimum required permissions for PATs
- For private packages, ensure only authorized users have access

## Support

For issues or questions:
- Create an issue at https://github.com/MarcelleBond/ModMon/issues
- Check existing documentation in `MODMON_USER_GUIDE.md` and `AI_AGENT_GUIDE.md`
