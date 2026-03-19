# ReadRestQueryable - AI Coding Agent Instructions

## Project Overview

**ReadRestQueryable** is a custom LINQ provider that converts LINQ query expressions into REST API query parameters, enabling type-safe data retrieval from REST services without manual URL construction.

### Core Concept
- **Single-pass expression evaluation**: Only the innermost `where` clause in the same LINQ statement is converted to query parameters; nested `where` clauses execute in-memory via LINQ-to-Objects
- **Custom IQueryProvider implementation**: `GenericProvider` and `AdgangsAdresseProvider` intercept LINQ expressions and translate them to REST API queries
- **REST API integration**: Currently targets DAWA (Danish Address Service) APIs but designed for generic provider extensibility

### Key Achievement
Users write natural LINQ like:
```csharp
var items = from a in new AdgangsAdresseRepository<AdgangsAdresse>()
            where a.Postnr == "5220" && a.Vejnavn == "Vestergade"
            select a;
```
which generates efficient API query: `?postnr=5220&vejnavn=Vestergade`

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

## Project Structure

```
ReadRestLib/               # Core custom LINQ provider
├── Providers/             # IQueryProvider implementations
│   ├── GenericProvider.cs # Generic orchestrator
│   └── AdgangsAdresseProvider.cs
├── Readers/               # REST API communication & deserialization
│   ├── GenericReader.cs
│   └── AdgangsAdresseReader.cs
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

2. **Create a Provider** (inherit `IQueryProvider`):
   - Follow `AdgangsAdresseProvider.cs` structure
   - Call `QueryVisitor.Visit()` to extract predicates
   - Use corresponding `*Reader` to fetch data

3. **Create a Reader** (inherit from `GenericReader<T>` or implement independently):
   - Extract BaseUrl via reflection
   - Append query string and call REST endpoint

### Expression Tree Handling

- **Parameters must stay unparameterized** in `Evaluator.PartialEval()` — constant arithmetic/method calls are pre-evaluated, parameters are preserved
- **Join expressions** are partially supported — extracted into metadata but full relational algebra not implemented; most filtering still happens in-memory
- **OrderBy/Select** after the initial `where` execute in-memory on fetched data (not pushed to API)
- **Only the first `where` in the same LINQ statement is converted to query parameters**; nested `where` clauses execute in-memory
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
```bash
sh -c "mcs TestRunner.cs -target:exe -out:TestRunner.exe && mono TestRunner.exe"
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

- **Query string not generated?** → Check QueryVisitor.Visit() logs; ensure `where` clause is in the outermost LINQ statement
- **Deserialization fails?** → Verify model property names match API JSON response (case-sensitive); test with IntegrationTest.cs
- **In-memory filtering slower than expected?** → OrderBy/Select not pushed to API; move all filtering to initial `where`
- **Type mismatch at expression evaluation?** → Check ExpressionTreeModifier is replacing the correct constant type
