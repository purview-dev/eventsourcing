set quiet

# Variables
root_folder := "./src"
test_root := root_folder + "/tests"

solution_file := root_folder + "/EventSourcing.slnx"
sg_solution_file := root_folder + "/SourceGeneration.slnf"

sg_perf_tests := test_root + "/SourceGenerator.PerformanceTests/SourceGenerator.PerformanceTests.csproj"
sql_perf_tests := test_root + "/SqlServer.PerformanceTests/SqlServer.PerformanceTests.csproj"

build_configuration := "Release"
artifacts_folder := "./artifacts"

pipeline_solution := "build/Pipeline.slnx"
pipeline_project := "build/PipelineCLI/PipelineCLI.csproj"

current_version := `node -p "require('./package.json').version"`

[private]
default:
    just --list

# Run the PR pipeline (restore, build, lint, tests)
[group('Pipeline')]
pipeline-pr *args:
    echo "Running PR pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} {{ args }}

# Run the build pipeline (restore, build, lint)
[group('Pipeline')]
pipeline-build *args:
    echo "Running build pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build:RunTests=false --Release:Mode=None {{ args }} 

# Run the release pipeline (restore, build, lint, tests, pack, publish, GitHub release)
[group('Pipeline')]
pipeline-release *args:
    echo "Running release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release:Mode=NuGet {{ args }}

# Run the release pipeline (restore, build, lint, tests, pack, local nuget publish)
# Note: `just` runs recipes through the shell, which strips backslashes from unquoted arguments.
# Always use forward slashes for the feed path, e.g.
# just pipeline-local-release --PublishLocalNuGet:LocalFeedPath=p:/_sync-projects/.local-nuget/
[group('Pipeline')]
pipeline-local-release *args:
    echo "Running local release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release:Mode=LocalNuGet {{ args }}

# Run the pipeline with tests enabled
[group('Pipeline')]
pipeline-tests *args:
    echo "Running tests pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build:RunTests=true --Release:Mode=None {{ args }}

# Open the solution in Visual Studio/ Registered application
[group('Utilities')]
vs:
    open {{ solution_file }}

# Open the solution in Visual Studio/ Registered application
[group('Utilities')]
vs-pipeline:
    open {{ pipeline_solution }}

# Open the solution in Visual Studio
[group('Utilities')]
vs-sg:
    open "{{ sg_solution_file }}"

# Build the solution for the specified configuration (default: Release)
[group('Build and Test')]
build *args:
    echo "==> Building {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet build {{ solution_file }} --configuration {{ build_configuration }} {{ args }}

# Cleans the solution for the specified configuration (default: Release)
[group('Build and Test')]
clean *args:
    echo "==> Cleaning {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet clean {{ solution_file }} --configuration {{ build_configuration }} {{ args }}

# Restore local .NET tools
[group('Utilities')]
tools:
    dotnet tool restore

# Restore NuGet packages for the solution
[group('Build and Test')]
restore *args:
    dotnet restore {{ solution_file }} {{ args }}

# Displays the current package version from package.json
[group('Build and Test')]
current_version:
    echo "==> Current version: {{ GREEN }}{{ current_version }}{{ NORMAL }} (defined in package.json and automatically included in the build output through the Purview.DotNetProjectSdk package)"

# Run source generator performance harness (pass --benchmark for larger runs)
[group('Performance Tests')]
perf-source-generator *args:
    dotnet run --project {{ sg_perf_tests }} --configuration {{ build_configuration }} -- {{ args }}

# Run SQL Server event/snapshot performance harness (pass --benchmark for larger runs)
[group('Performance Tests')]
perf-sql-server *args:
    dotnet run --project {{ sql_perf_tests }} --configuration {{ build_configuration }} -- {{ args }}

# Run tests for a specific project with a filter (e.g., "/*/*/*/*/", or "/*/*/*/*[Category=Unit]" to run just unit tests) and configuration (e.g., "Release")
[group('Build and Test')]
test filter="/*/*/*/*/" *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ build_configuration }}{{ NORMAL }}) with filter {{ YELLOW }}{{ filter }}{{ NORMAL }}"
    dotnet test --project {{ solution_file }} --configuration {{ build_configuration }} --treenode-filter "{{ filter }}" --ignore-exit-code 8 {{ args }}

# Pack all packable projects
[group('Build and Test')]
pack artifact_folder=artifacts_folder *args:
    echo "==> Packing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) to {{ YELLOW }}{{ artifact_folder }}{{ NORMAL }}"
    dotnet pack "{{ solution_file }}" --configuration "{{ build_configuration }}" --output "{{ artifact_folder }}" {{ args }}

# Format the code with CSharpier
[group('Utilities')]
lint-fix:
    dotnet csharpier format {{ root_folder }}

# Check formatting with CSharpier
[group('Utilities')]
lint-check:
    dotnet csharpier check {{ root_folder }}
