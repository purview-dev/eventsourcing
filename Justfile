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

current_version := `node -p "require('./package.json').version"`

# Default recipe - list available recipes
[private]
default:
    just --list

# Open the solution in Visual Studio
vs:
    open "{{ solution_file }}"

# Open the solution in Visual Studio
vs-sg:
    open "{{ sg_solution_file }}"

# Build the solution for the specified configuration (default: Release)
build *args:
    echo "==> Building {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet build {{ solution_file }} --configuration {{ build_configuration }} {{ args }}

# Cleans the solution for the specified configuration (default: Release)
clean *args:
    echo "==> Cleaning {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet clean {{ solution_file }} --configuration {{ build_configuration }} {{ args }}

# Restore local .NET tools
tools:
    dotnet tool restore

# Restore NuGet packages for the solution
restore *args:
    dotnet restore {{ solution_file }} {{ args }}

# Displays the current package version from package.json
current_version:
    echo "==> Current version: {{ GREEN }}{{ current_version }}{{ NORMAL }} (defined in package.json and automatically included in the build output through the Purview.DotNetProjectSdk package)"

# Run source generator performance harness (pass --benchmark for larger runs)
perf-source-generator *args:
    dotnet run --project {{ sg_perf_tests }} --configuration {{ build_configuration }} -- {{ args }}

# Run SQL Server event/snapshot performance harness (pass --benchmark for larger runs)
perf-sql-server *args:
    dotnet run --project {{ sql_perf_tests }} --configuration {{ build_configuration }} -- {{ args }}

# Run tests for a specific project with a filter (e.g., "/*/*/*/*/", or "/*/*/*/*[Category=Unit]" to run just unit tests) and configuration (e.g., "Release")
test filter="/*/*/*/*/" *args:
    echo "==> Testing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ build_configuration }}{{ NORMAL }}) with filter {{ YELLOW }}{{ filter }}{{ NORMAL }}"
    dotnet test --project {{ solution_file }} --configuration {{ build_configuration }} --treenode-filter "{{ filter }}" --ignore-exit-code 8 {{ args }}

# Pack all packable projects
pack artifact_folder=artifacts_folder *args:
    echo "==> Packing {{ BLUE }}{{ solution_file }}{{ NORMAL }} ({{ GREEN }}{{ current_version }}{{ NORMAL }}) to {{ YELLOW }}{{ artifact_folder }}{{ NORMAL }}"
    dotnet pack "{{ solution_file }}" --configuration "{{ build_configuration }}" --output "{{ artifact_folder }}" {{ args }}

# Format the code with CSharpier
lint-fix:
    dotnet csharpier format {{ root_folder }}

# Check formatting with CSharpier
lint-check:
    dotnet csharpier check {{ root_folder }}
