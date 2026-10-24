# Boundary Validation Manual Test

This project exercises the real ModMon CLI as a human would use it. Each
run first executes `modmon init`, then adds `Orders` and `Products` with
`modmon module add`. It copies that generated project metadata into isolated
temporary scenarios, applies boundary violations, and invokes the real
`modmon validate` command. It does not call validator classes directly.

A generated, inspectable sample project is also kept in `ManualProject/`.
Each harness run additionally verifies that the real init workflow creates
`AGENTS.md` and all four `.agent/modmon/` guidance files with the project name.

## Run

From the repository root:

```powershell
dotnet run --project "Test Projects/BoundaryValidationManual"
```

The harness runs these scenarios:

- valid API, modules, and SharedKernel references — expected exit `0`
- module-to-module reference — expected exit `5`
- SharedKernel-to-module reference — expected exit `5`
- reference outside the selected solution — expected exit `5`
- dynamic `ProjectReference` expression — expected exit `5`
- project dependency cycle — expected exit `5`
- malformed project XML — expected exit `5`
- missing solution — expected exit `4`

The harness prints the JSON output from each CLI invocation and returns a
nonzero exit code if any actual result differs from the expected result.
