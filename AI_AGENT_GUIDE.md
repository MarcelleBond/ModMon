# modmon CLI - AI Agent Integration Guide

This guide provides machine-readable documentation for AI agents to interact with the modmon CLI tool.

## Overview

The modmon CLI is a code generation tool for creating modular monolith applications with .NET. It supports both human-readable and JSON output formats, making it ideal for AI agent automation.

## Exit Codes

All commands return specific exit codes for precise error handling:

- `0` - Success
- `1` - General failure (unknown error)
- `2` - Invalid arguments (missing required options, invalid values)
- `3` - Resource already exists (solution/module already exists)
- `4` - Resource not found (solution not found, path doesn't exist)
- `5` - Validation failed (project structure invalid)
- `6` - External tool error (dotnet CLI failed, dotnet-ef not found)
- `7` - File system error (permission denied, disk full)
- `8` - Network error (NuGet package download failed)

## Global Options

All commands support these global options:

- `--format <human|json>` - Output format (default: human)
- `--verbose` - Enable verbose logging to console
- `--log-file <path>` - Write detailed logs to specified file

## Commands

### 1. Initialize New Project

**Command:** `modmon init --name <ProjectName> [options]`

**Options:**
- `--name <ProjectName>` (required) - Name of the project to create
- `--output <Path>` (optional) - Output directory (default: current directory)
- `--dry-run` (optional) - Preview operations without making changes
- `--format json` (optional) - Output as JSON
- `--verbose` (optional) - Enable verbose logging
- `--log-file <path>` (optional) - Log to file

**JSON Output (Success):**
```json
{
  "success": true,
  "message": "Project initialized successfully",
  "exitCode": 0,
  "timestamp": "2026-05-22T17:32:00Z",
  "command": "init",
  "projectName": "ECommerce",
  "solutionPath": "C:\\Projects\\ECommerce\\ECommerce.sln",
  "repoRoot": "C:\\Projects\\ECommerce",
  "createdProjects": [
    "ECommerce.Api",
    "ECommerce.SharedKernel"
  ],
  "createdFiles": [],
  "dryRun": false
}
```

**JSON Output (Dry Run):**
```json
{
  "success": true,
  "message": "Dry run completed. No changes made.",
  "exitCode": 0,
  "timestamp": "2026-05-22T17:32:00Z",
  "command": "init",
  "projectName": "ECommerce",
  "solutionPath": "C:\\Projects\\ECommerce\\ECommerce.sln",
  "repoRoot": "C:\\Projects\\ECommerce",
  "createdProjects": [
    "ECommerce.Api",
    "ECommerce.SharedKernel"
  ],
  "createdFiles": [],
  "dryRun": true,
  "operations": [
    {
      "type": "CreateSolution",
      "details": {
        "path": "C:\\Projects\\ECommerce\\ECommerce.sln"
      }
    }
  ]
}
```

**JSON Output (Error - Resource Already Exists):**
```json
{
  "success": false,
  "message": "Solution already exists",
  "exitCode": 3,
  "timestamp": "2026-05-22T17:32:00Z",
  "command": "init",
  "projectName": "ECommerce",
  "solutionPath": "C:\\Projects\\ECommerce\\ECommerce.sln",
  "errors": [
    "Solution file already exists: C:\\Projects\\ECommerce\\ECommerce.sln"
  ]
}
```

### 2. Add Module

**Command:** `modmon module add --name <ModuleName> [options]`

**Options:**
- `--name <ModuleName>` (required) - Name of the module to create
- `--output <Path>` (optional) - Project directory (default: current directory)
- `--dry-run` (optional) - Preview operations without making changes
- `--format json` (optional) - Output as JSON
- `--verbose` (optional) - Enable verbose logging
- `--log-file <path>` (optional) - Log to file

**JSON Output (Success):**
```json
{
  "success": true,
  "message": "Module added successfully",
  "exitCode": 0,
  "timestamp": "2026-05-22T17:35:00Z",
  "command": "module add",
  "moduleName": "Orders",
  "modulePath": "C:\\Projects\\ECommerce\\ECommerce.Orders",
  "migrationCreated": true,
  "updatedFiles": [
    "C:\\Projects\\ECommerce\\ECommerce.Api\\Extensions\\DependencyInjection.cs",
    "C:\\Projects\\ECommerce\\Dockerfile"
  ],
  "createdFiles": [],
  "dryRun": false
}
```

**JSON Output (Error - Module Already Exists):**
```json
{
  "success": false,
  "message": "Module already exists",
  "exitCode": 3,
  "timestamp": "2026-05-22T17:35:00Z",
  "command": "module add",
  "moduleName": "Orders",
  "modulePath": "C:\\Projects\\ECommerce\\ECommerce.Orders",
  "errors": [
    "Module directory already exists: C:\\Projects\\ECommerce\\ECommerce.Orders"
  ]
}
```

### 3. Get Project Information

**Command:** `modmon info [options]`

**Options:**
- `--path <Path>` (optional) - Project directory (default: current directory)
- `--format json` (optional) - Output as JSON

**JSON Output:**
```json
{
  "success": true,
  "message": "Project information retrieved successfully",
  "exitCode": 0,
  "timestamp": "2026-05-22T17:40:00Z",
  "command": "info",
  "projectName": "ECommerce",
  "solutionPath": "C:\\Projects\\ECommerce\\ECommerce.sln",
  "repoRoot": "C:\\Projects\\ECommerce",
  "modules": [
    {
      "name": "Orders",
      "path": "C:\\Projects\\ECommerce\\ECommerce.Orders",
      "hasDbContext": true,
      "hasMigrations": true
    },
    {
      "name": "Products",
      "path": "C:\\Projects\\ECommerce\\ECommerce.Products",
      "hasDbContext": true,
      "hasMigrations": false
    }
  ],
  "projects": [
    "ECommerce.Api",
    "ECommerce.SharedKernel",
    "ECommerce.Orders",
    "ECommerce.Products"
  ]
}
```

### 4. Validate Project Structure

**Command:** `modmon validate [options]`

**Options:**
- `--path <Path>` (optional) - Project directory (default: current directory)
- `--format json` (optional) - Output as JSON

**JSON Output (Valid):**
```json
{
  "success": true,
  "message": "Project structure is valid",
  "exitCode": 0,
  "timestamp": "2026-05-22T17:45:00Z",
  "command": "validate",
  "valid": true,
  "projectName": "ECommerce",
  "checks": [
    {
      "name": "Solution file exists",
      "passed": true
    },
    {
      "name": "API project exists",
      "passed": true
    },
    {
      "name": "SharedKernel project exists",
      "passed": true
    },
    {
      "name": "DI aggregator file exists",
      "passed": true
    },
    {
      "name": "Dockerfile exists",
      "passed": true
    }
  ]
}
```

**JSON Output (Invalid):**
```json
{
  "success": true,
  "message": "Project structure has issues",
  "exitCode": 5,
  "timestamp": "2026-05-22T17:45:00Z",
  "command": "validate",
  "valid": false,
  "projectName": "ECommerce",
  "checks": [
    {
      "name": "Solution file exists",
      "passed": true
    },
    {
      "name": "API project exists",
      "passed": false,
      "message": "Missing: ECommerce.Api"
    },
    {
      "name": "SharedKernel project exists",
      "passed": true
    },
    {
      "name": "DI aggregator file exists",
      "passed": false,
      "message": "Missing: C:\\Projects\\ECommerce\\ECommerce.Api\\Extensions\\DependencyInjection.cs"
    },
    {
      "name": "Dockerfile exists",
      "passed": true
    }
  ],
  "errors": [
    "API project not found: ECommerce.Api",
    "DI aggregator file not found: C:\\Projects\\ECommerce\\ECommerce.Api\\Extensions\\DependencyInjection.cs"
  ]
}
```

### 5. List Modules

**Command:** `modmon module list [options]`

**Options:**
- `--path <Path>` (optional) - Project directory (default: current directory)
- `--format json` (optional) - Output as JSON

**JSON Output:**
```json
{
  "success": true,
  "message": "Found 2 module(s)",
  "exitCode": 0,
  "timestamp": "2026-05-22T17:50:00Z",
  "command": "module list",
  "projectName": "ECommerce",
  "modules": [
    {
      "name": "Orders",
      "path": "C:\\Projects\\ECommerce\\ECommerce.Orders",
      "hasDbContext": true,
      "hasMigrations": true
    },
    {
      "name": "Products",
      "path": "C:\\Projects\\ECommerce\\ECommerce.Products",
      "hasDbContext": true,
      "hasMigrations": false
    }
  ],
  "count": 2
}
```

## AI Agent Workflow Examples

### Example 1: Create New Project with Validation

```bash
# Step 1: Initialize project with JSON output
modmon init --name MyApp --format json

# Step 2: Validate the structure
modmon validate --format json

# Step 3: Get project info
modmon info --format json
```

### Example 2: Add Module with Dry Run First

```bash
# Step 1: Preview module creation
modmon module add --name Users --dry-run --format json

# Step 2: If dry run looks good, create the module
modmon module add --name Users --format json

# Step 3: Verify module was added
modmon module list --format json
```

### Example 3: Error Handling

```bash
# Attempt to create duplicate project
modmon init --name MyApp --format json
# Returns exit code 3 with error details in JSON

# Parse JSON response and check exitCode field
# If exitCode != 0, read errors array for details
```

## JSON Schema Conventions

All JSON responses follow these conventions:

- **Property naming:** camelCase
- **Dates:** ISO 8601 format (e.g., "2026-05-22T17:32:00Z")
- **Null handling:** Null properties are omitted from output
- **Arrays:** Always arrays (never null), empty if no items
- **Booleans:** true/false (not strings)
- **Numbers:** Numeric types (not strings)
- **Enums:** String values for readability

## Common Response Fields

All command results include:

- `success` (boolean) - Whether the command succeeded
- `message` (string) - Human-readable summary
- `exitCode` (number) - Numeric exit code (0-8)
- `timestamp` (string) - ISO 8601 UTC timestamp
- `command` (string) - Command that was executed
- `errors` (array, optional) - List of error messages
- `warnings` (array, optional) - List of warning messages

## Prerequisites

Before using the modmon CLI, ensure:

1. .NET SDK 10.0 or later is installed
2. `dotnet-ef` tool is installed (for module creation with migrations)
3. PostgreSQL is available (for database operations)
4. Docker is installed (if using containerization)

## Error Recovery

When commands fail:

1. Check the `exitCode` to determine error type
2. Read the `errors` array for specific issues
3. Use `--verbose` flag for detailed logging
4. Use `--log-file` to capture full execution trace
5. Run `validate` command to check project structure

## Best Practices for AI Agents

1. **Always use `--format json`** for machine-readable output
2. **Check exit codes** before parsing JSON response
3. **Use dry-run mode** for preview before destructive operations
4. **Validate project structure** after major operations
5. **Enable verbose logging** when debugging issues
6. **Parse errors array** for specific failure reasons
7. **Use absolute paths** when specifying `--output` or `--path`
8. **Handle all exit codes** (0-8) appropriately
