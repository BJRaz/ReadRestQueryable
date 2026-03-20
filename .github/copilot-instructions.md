# ReadRestQueryable - AI Coding Agent Instructions

## Project Overview

**ReadRestQueryable** is a custom LINQ provider that converts LINQ query expressions into REST API query parameters, enabling type-safe data retrieval from REST services without manual URL construction.

### Core Concept
- **Single-pass expression evaluation**: Only the innermost `where` clause in the same LINQ statement is converted to query parameters; nested `where` clauses execute in-memory via LINQ-to-Objects
- **Custom IQueryProvider implementation**: `GenericProvider` intercepts LINQ expressions and translates them to REST API queries
- **REST API integration**: Uses `GenericProvider` and `GenericReader<T>` to work with any REST API by leveraging the `[BaseUrl]` attribute on model classes

### Key Achievement
Users write natural LINQ like:
```csharp
var items = from a in new DAWARepository<AdgangsAdresse>()
            where a.Postnr == "5220" && a.Vejnavn.StartsWith("Vester")
                  && (a.Kommunekode == "0101" || a.Kommunekode == "0202")
            select a;
```
which generates efficient API query: `?postnr=5220&q=Vester*&kommunekode=0101|0202`

## Architecture: Expression Tree Processing Pipeline

The system uses a **three-stage expression tree transformation** (see `GenericProvider.Execute()`):

1. **QueryVisitor** (`Visitors/QueryVisitor.cs`): Walks the expression tree to extract REST-friendly predicates
   - Evaluates `where` clauses to build query strings
   - Handles `Join` expressions (stores join metadata for later evaluation)
   - Returns a string query like `"postnr=5220&vejnavn=Vestergade"`

2. **GenericReader** (`Readers/GenericReader.cs`): Fetches and deserializes JSON
   - Uses `BaseUrlAttribute` on model classes to determine REST endpoint
   - Appends the query string built by QueryVisitor
   - Returns `IEnumerable<T>` via Newtonsoft.Json deserialization

3. **ExpressionTreeModifier** (`Visitors/ExpressionTreeModifier.cs`): Replaces DAWARepository constants with the IEnumerable result
   - Enables remaining LINQ operations (OrderBy, Select projections) to execute in-memory via LINQ-to-Objects
   - this is necessary because only the first `where` is converted to query parameters; subsequent operations must work with the fetched data
   - Preserves parameter nodes to maintain LINQ query structure
   - Avoids mutating the original expression tree, instead creates a new modified tree
   - do not attempt to replace any constants other than the DAWARepository<T> instance, as this risks breaking the expression tree structure and causing runtime errors
   - does not attempt to push OrderBy/Select to the API, as this is not supported by the current implementation and would require a more complex translation layer

## Project Structure

```
ReadRestLib/               # Core custom LINQ provider
├── Providers/             # IQueryProvider implementations
│   └── GenericProvider.cs # Generic orchestrator for all model types
├── Readers/               # REST API communication & deserialization
│   └── GenericReader.cs   # Generic deserializer for all models
├── Visitors/              # Expression tree transformations
│   ├── QueryVisitor.cs    # Extract REST predicates
│   ├── ExpressionTreeModifier.cs # Replace constants
│   ├── Evaluator.cs       # Partial expression evaluation (MS-LPL licensed)
│   └── EvaluateVisitor.cs
├── Model/                 # Data classes with BaseUrlAttribute
├── Attributes/            # BaseUrlAttribute marks REST endpoint
└── Utilities/             # ReflectionCache

ReadRestLib.Tests/         # NUnit test suite (xUnit adapter)
├── EvaluatorTests.cs
├── ExpressionTest.cs
└── IntegrationTest.cs     # Live API calls (needs internet)

ReadRestApp/              # Console example usage
```

## Critical Patterns & Conventions

### Adding Support for New REST Endpoints

1. **Create model class** with `[BaseUrl]` attribute:
   ```csharp
   [BaseUrl(@"https://api.example.com/resources")]
   public class Resource { public string Id { get; set; } }
   ```

2. **Use GenericProvider and GenericReader** — no provider-specific classes needed:
   - `GenericProvider` uses reflection to determine the correct reader based on the model's `BaseUrl`
   - `GenericReader<T>` uses reflection to read the `BaseUrl` from the model class
   - Both work out-of-the-box for any model with the `[BaseUrl]` attribute

### Expression Tree Handling

#### Supported REST Operators
- **`Equal` (`==`)**: Converted to REST query parameters (e.g., `postnr=5000`)
- **`And`/`AndAlso` (`&&`)**: Combines multiple predicates in REST query with `&`
- **`StartsWith("X")`**: Translated to `q=X*` REST query parameter (always uses `q` parameter name, regardless of which property calls `StartsWith`)
- **`Or`/`OrElse` (`||`) on same property**: Collapses to pipe-delimited values (e.g., `a.Kommunekode == "0101" || a.Kommunekode == "0202"` → `kommunekode=0101|0202`). Supports chaining 3+ values. **Only supported when all sides reference the same property name.**

#### Silently Skipped Operators (Applied In-Memory)
The following operators are silently dropped from the REST query string and instead applied in-memory by LINQ-to-Objects after the API response is fetched:
- **`NotEqual` (`!=`)**: Silently skipped; filtered in-memory
- **`GreaterThan`, `LessThan`, `GreaterThanOrEqual`, `LessThanOrEqual`** (`>`, `<`, `>=`, `<=`): Silently skipped
- **`Or`/`OrElse` (`||`) on different properties**: Silently skipped (e.g., `a.Kommunekode == "0101" || a.Postnr == "5000"` cannot be represented in REST; entire expression is dropped and filtered in-memory)
- **`Contains`, `EndsWith`** and other method calls: Silently skipped; only `StartsWith` is translated to REST

#### LINQ Operation Behavior
- **Parameters must stay unparameterized** in `Evaluator.PartialEval()` — constant arithmetic/method calls are pre-evaluated, parameters are preserved
- **Only the first `where` in the same LINQ statement is converted to query parameters**; nested `where` clauses execute in-memory
- **OrderBy/Select** after the initial `where` execute in-memory on fetched data (not pushed to API)
- **Join expressions** are partially supported — extracted into metadata, separate REST calls made, join completed in-memory; full relational algebra not implemented
- **ExpressionTreeModifier** must only replace constants of type `DAWARepository<T>` with the fetched `IEnumerable<T>`; any other replacements risk breaking the expression tree structure

### Newtonsoft.Json Deserialization

- All model classes must be JSON-serializable (public properties)
- `GenericReader<T>` uses `JsonConvert.DeserializeObject<IEnumerable<T>>()`
- Test against actual API responses in `IntegrationTest.cs`

## Build & Test Workflow

### Build
```bash
dotnet build /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

### Run Tests
**Always use TestRunner, not `dotnet test`** (dotnet test fails on macOS due to x86 testhost limitations):
```bash
mcs TestRunner.cs -target:exe -out:TestRunner.exe && mono TestRunner.exe
```
Or via VS Code task: `Run Test Task`

### Run Example App
```bash
dotnet run --project ReadRestApp/ReadRestApp.csproj
```

## Licensing & Third-Party Code

- **Evaluator.cs**: Copied from Microsoft "LINQ to TerraServer Provider Sample" under MS-LPL license (included in file header)
- **Dependencies**: Newtonsoft.Json (12.0.1), RestSharp (106.6.9), NUnit (3.13.2)

## When Modifying Expression Visitors

1. **Always preserve parameter nodes** — breaking this breaks LINQ parameter binding
2. **Test with unit tests first** (EvaluatorTests.cs) before integration testing
3. **ExpressionTreeModifier must only replace constants** — changes to Visit logic risk breaking the type system
4. **Avoid mutating expression trees** — ExpressionVisitor pattern creates new trees by design

## Common Debugging Points

- **Query string not generated?** → Check QueryVisitor.Visit() logs; ensure `where` clause is in the outermost LINQ statement; verify operators are supported (`==`, `&&`, `StartsWith`, `||` on same property)
- **`StartsWith` not appearing in REST query?** → Verify the method call is `StartsWith` (not `Contains` or `EndsWith`); it always maps to `q=X*` parameter regardless of which property calls it
- **`||` not appearing in REST query?** → Only works when both sides are `==` on the **same property** (e.g., `a.Kommunekode == "X" || a.Kommunekode == "Y"`); different properties are silently skipped to in-memory
- **Deserialization fails?** → Verify model property names match API JSON response (case-sensitive); test with IntegrationTest.cs
- **In-memory filtering slower than expected?** → OrderBy/Select not pushed to API; move all filtering to initial `where`; `!=`, `Contains`, `EndsWith` are in-memory only
- **Type mismatch at expression evaluation?** → Check ExpressionTreeModifier is replacing the correct constant type
- **Empty log output?** → Query may have thrown an exception during evaluation; add exception handling and check TestRunner output for error details

## Known Limitations

1. **`NotEqual` (`!=`) silently skipped**: Dropped from REST query; filtered in-memory. This means ALL rows matching the equality predicates are fetched, then `!=` is applied in-memory
2. **Comparison operators silently skipped**: No `>`, `<`, `>=`, `<=` in REST queries; applied in-memory
3. **`Or` on different properties silently skipped**: `a.X == "1" || a.Y == "2"` cannot be represented in REST; entire `||` expression is dropped and filtered in-memory
4. **`Contains` and `EndsWith` not translated to REST**: Only `StartsWith` maps to `q=X*`; `Contains` and `EndsWith` are silently skipped to in-memory
5. **`StartsWith` always uses `q` parameter**: Regardless of which property calls `StartsWith`, the REST parameter is always `q` (matching DAWA API convention)
6. **No pagination**: `Skip()` and `Take()` not pushed to API; all results fetched then filtered in-memory
7. **No sorting at REST level**: `OrderBy` applied after fetch; for large result sets, consider filtering more aggressively in `where`
