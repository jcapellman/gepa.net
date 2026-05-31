# Reusable Skills & Components

This document outlines the reusable "skills" extracted from the GEPA.NET implementation.

## ✅ Created: Gepa.Net.Client NuGet Package

**Location**: `src/Gepa.Net.Client/`

### What It Is
A standalone .NET library that any C# developer can use to integrate GEPA optimization without rewriting HTTP client logic.

### Features
- ✅ **IGepaClient interface** - Dependency injection ready
- ✅ **Strongly typed models** - Full IntelliSense support
- ✅ **Extension methods** - `services.AddGepaClient(...)`
- ✅ **Async/await** - Non-blocking operations
- ✅ **Polling helper** - `WaitForCompletionAsync()` with configurable intervals
- ✅ **Logging** - ILogger integration
- ✅ **Cancellation** - CancellationToken throughout
- ✅ **Error handling** - Custom GepaClientException

### Usage

```csharp
// Install package
dotnet add package Gepa.Net.Client

// Configure (Program.cs)
builder.Services.AddGepaClient(options =>
{
	options.ServiceUrl = "http://gepa-service:8000";
	options.CallbackBaseUrl = "https://your-api.com";
});

// Use (any service/controller)
public class MyService
{
	private readonly IGepaClient _gepaClient;

	public MyService(IGepaClient gepaClient) 
		=> _gepaClient = gepaClient;

	public async Task<string> OptimizeAsync(string prompt)
	{
		var request = new OptimizationRequest
		{
			PromptId = "prompt-1",
			SeedPrompt = prompt,
			TrainingSet = examples,
			ValidationSet = validationExamples
		};

		var jobId = await _gepaClient.TriggerOptimizationAsync(request);

		// Wait for completion with polling
		var result = await _gepaClient.WaitForCompletionAsync(
			jobId, 
			pollingInterval: TimeSpan.FromSeconds(5),
			timeout: TimeSpan.FromMinutes(30)
		);

		return result.OptimizedPrompt;
	}
}
```

### Publishing to NuGet

```bash
cd src/Gepa.Net.Client

# Build package
dotnet pack -c Release

# Publish (requires NuGet API key)
dotnet nuget push bin/Release/Gepa.Net.Client.1.0.0.nupkg \
	--api-key <your-key> \
	--source https://api.nuget.org/v3/index.json
```

---

## 🎯 Additional Skills That Could Be Extracted

### 1. **Project Template: ASP.NET + GEPA**

A `dotnet new` template for instant scaffolding:

```bash
dotnet new install Gepa.Net.Templates
dotnet new gepa-api -n MyPromptService
```

Would generate:
- ✅ Controllers with GEPA integration
- ✅ Docker Compose setup
- ✅ AWS CloudFormation templates
- ✅ Sample tests
- ✅ GitHub Actions CI/CD

**Effort**: 4-8 hours  
**Value**: High - reduces setup time from hours to minutes

### 2. **Python Package: gepa-dotnet-adapter**

A Python adapter for the GEPA library specifically for .NET integration:

```python
from gepa.adapters import DotNetRestAdapter

adapter = DotNetRestAdapter(
	api_url="https://your-csharp-api.com",
	auth_token="your-token"
)

result = gepa.optimize(
	adapter=adapter,
	seed_candidate={"system_prompt": "..."},
	trainset=trainset,
	valset=valset
)
```

**Effort**: 6-12 hours  
**Value**: Medium - makes Python→C# integration more official

### 3. **Azure Bicep Templates**

Alternative to CloudFormation for Azure deployments:

```
azure-bicep/
├── main.bicep
├── container-apps.bicep
├── cosmos-db.bicep
└── service-bus.bicep
```

**Effort**: 8-16 hours  
**Value**: High - Azure is popular in enterprise .NET shops

### 4. **GitHub Copilot Instructions**

A `.github/copilot-instructions.md` for this pattern:

```markdown
# Async ML Job Processing Pattern

When integrating external ML services (like GEPA):

1. Use HTTP client with async/await
2. Return job ID immediately (202 Accepted)
3. Implement polling endpoint for status
4. Consider webhook callbacks for completion
5. Store job state (DynamoDB/SQL)
6. Add timeout handling
7. Implement cancellation support

Example:
[code snippet from this repo]
```

**Effort**: 2-4 hours  
**Value**: Medium - helps AI assistants understand the pattern

### 5. **Helm Chart for Kubernetes**

For Kubernetes deployments instead of ECS:

```yaml
helm install gepa-net ./helm-chart \
	--set gepaClient.serviceUrl=http://gepa-service:8000 \
	--set openai.apiKey=$OPENAI_API_KEY
```

**Effort**: 8-12 hours  
**Value**: High - Kubernetes is widely used

### 6. **Visual Studio Extension**

A VS extension that scaffolds GEPA integration:

- Right-click project → "Add GEPA Integration"
- Auto-installs Gepa.Net.Client
- Adds configuration
- Creates sample controller

**Effort**: 20-40 hours  
**Value**: Very High - reduces friction significantly

---

## 📊 Skill Value Matrix

| Skill | Reusability | Effort | Impact | Priority |
|-------|-------------|--------|--------|----------|
| **Gepa.Net.Client** | ⭐⭐⭐⭐⭐ | Low | High | ✅ **DONE** |
| Project Template | ⭐⭐⭐⭐ | Low | High | 🔥 Recommended |
| Azure Bicep | ⭐⭐⭐⭐ | Medium | High | 🔥 Recommended |
| Helm Chart | ⭐⭐⭐⭐ | Medium | High | 🔥 Recommended |
| Python Adapter | ⭐⭐⭐ | Medium | Medium | 💡 Nice to have |
| Copilot Instructions | ⭐⭐⭐ | Low | Medium | 💡 Nice to have |
| VS Extension | ⭐⭐⭐⭐⭐ | High | Very High | 🚀 Future |

---

## 🎓 Pattern as a "Skill"

The **architectural pattern itself** is the most valuable skill:

### Pattern: Async ML Service Integration

**Problem**: Integrate long-running ML workloads (minutes/hours) into REST APIs without blocking.

**Solution**:
1. **Trigger endpoint** returns job ID immediately (202 Accepted)
2. **Background worker** processes async
3. **Status endpoint** for polling
4. **Webhook callback** for completion notification
5. **Job store** tracks state (in-memory → DynamoDB → SQL)

**Implementation**:
```
Client → REST API → Job Queue → ML Worker
		   ↓           ↓            ↓
		 Job ID      Status      Result
```

**When to use**:
- ✅ Operations taking >5 seconds
- ✅ Expensive compute (GPU, large models)
- ✅ External service calls
- ✅ User needs immediate response

**Variations**:
- **Simple**: HTTP only (this repo)
- **Queue-based**: SQS/Service Bus
- **Event-driven**: EventGrid/EventBridge

---

## 🚀 Next Steps

### Immediate (Do Now)
1. ✅ **Gepa.Net.Client is built** - Ready to publish to NuGet
2. Update your API to use the client library instead of inline code

### Short-term (This Week)
3. Create **project template** (`dotnet new gepa-api`)
4. Write **blog post** about the pattern
5. Add **Azure Bicep** templates

### Medium-term (This Month)
6. Create **Helm chart** for Kubernetes
7. Submit **Python adapter** to GEPA repo
8. Write **detailed documentation**

### Long-term (Future)
9. Build **Visual Studio extension**
10. Create **video tutorial**
11. Present at conference/meetup

---

## 📝 Documentation as a Skill

The documentation you have is itself reusable:

- ✅ `QUICK_START.md` - Template for any wrapper project
- ✅ `IMPLEMENTATION_SUMMARY.md` - Architecture decision record
- ✅ `aws/README.md` - Deployment guide template
- ✅ `sample-requests.http` - API testing template

These can be adapted for other "Wrap Python ML in .NET" scenarios:
- HuggingFace Transformers
- LangChain
- CrewAI
- AutoGen

---

## 🎯 Publishing the NuGet Package

Ready to publish? Here's how:

### 1. Update Package Metadata

Edit `src/Gepa.Net.Client/Gepa.Net.Client.csproj`:
```xml
<PropertyGroup>
	<Version>1.0.0</Version>
	<Authors>Your Name</Authors>
	<Description>A .NET client for GEPA prompt optimization</Description>
	<PackageTags>gepa;ai;prompt-optimization;llm</PackageTags>
	<RepositoryUrl>https://github.com/jcapellman/gepa.net</RepositoryUrl>
</PropertyGroup>
```

### 2. Build Package

```bash
cd src/Gepa.Net.Client
dotnet pack -c Release
```

Creates: `bin/Release/Gepa.Net.Client.1.0.0.nupkg`

### 3. Test Locally

```bash
# Add local source
dotnet nuget add source ./bin/Release -n Local

# Test in another project
dotnet add package Gepa.Net.Client --source Local
```

### 4. Publish to NuGet.org

```bash
# Get API key from https://www.nuget.org/account/apikeys
dotnet nuget push bin/Release/Gepa.Net.Client.1.0.0.nupkg \
	--api-key YOUR_API_KEY \
	--source https://api.nuget.org/v3/index.json
```

### 5. Verify

Visit: https://www.nuget.org/packages/Gepa.Net.Client

---

## 📚 Learning Resources

To extend these skills:

1. **NuGet Package Development**
   - https://learn.microsoft.com/en-us/nuget/create-packages/

2. **Project Templates**
   - https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates

3. **VS Extensions**
   - https://learn.microsoft.com/en-us/visualstudio/extensibility/

4. **Helm Charts**
   - https://helm.sh/docs/topics/charts/

5. **Azure Bicep**
   - https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/

---

## ✅ Summary

**Created Today:**
- ✅ **Gepa.Net.Client** - Reusable NuGet library (builds successfully)

**Can Be Extracted:**
- Project template (4-8 hours)
- Azure Bicep (8-16 hours)
- Helm chart (8-12 hours)
- Python adapter (6-12 hours)
- VS extension (20-40 hours)

**Most Valuable:**
1. The architectural pattern itself
2. Gepa.Net.Client library
3. Documentation templates
4. Project scaffold template

Would you like me to help you:
1. Publish the NuGet package?
2. Create the project template?
3. Add Azure Bicep templates?
4. Something else?
