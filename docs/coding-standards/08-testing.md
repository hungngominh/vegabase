# Testing

Quy tắc viết test cho VegaBase — scope, structure, và naming.

## Phân loại test

| Loại | Scope | Tool |
|---|---|---|
| Unit test | Business logic hooks (`CheckAddCondition`, `ApplyFilter`) | xUnit |
| Integration test | `DbActionExecutor`, `UnitOfWork` với real DB | xUnit + EF Core InMemory hoặc Testcontainers |

---

## TS-01 — Unit test tập trung vào business logic hooks

```csharp
// ✅ Đúng: test CheckAddCondition qua subclass (protected method cần test subclass để expose)
// Tạo TestableUserService trong test project:
internal class TestableUserService : UserService
{
    public Task TestCheckAddCondition(UserParam param, ServiceMessage sMessage)
        => CheckAddCondition(param, sMessage);
}

[Fact]
public async Task CheckAddCondition_DuplicateEmail_AddsErrorToMessage()
{
    // Arrange
    var mockExecutor = new Mock<IDbActionExecutor>();
    mockExecutor.Setup(d => d.QueryAsync<User>(It.IsAny<Func<IQueryable<User>, IQueryable<User>>>()))
                .ReturnsAsync(DbResult<List<User>>.Success(existingUsers, TimeSpan.Zero));
    var service = new TestableUserService(mockExecutor.Object, ...);
    var param = new UserParam { Email = "existing@example.com" };
    var sMessage = new ServiceMessage();

    // Act
    await service.TestCheckAddCondition(param, sMessage);

    // Assert
    Assert.True(sMessage.HasError);
}

// ❌ Sai: test GetList end-to-end trong unit test (integration concern)
[Fact]
public async Task GetList_ReturnsAllUsers()
{
    var result = await _service.GetList(new UserParam(), new ServiceMessage());
    Assert.NotEmpty(result); // cần DB thật để test này có ý nghĩa
}
```

---

## TS-02 — Integration test phải dùng real database, không mock `IDbActionExecutor`

```csharp
// ✅ Đúng: integration test với EF Core InMemory hoặc Testcontainers
public class UserDbTests : IAsyncLifetime
{
    private AppDbContext _context;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        _context = new AppDbContext(options);
        await _context.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task AddAsync_NewUser_PersistsToDatabase()
    {
        var executor = new DbActionExecutor(_context, NullLogger<DbActionExecutor>.Instance);
        var user = new User { Name = "Test", Email = "test@example.com" };

        var result = await executor.AddAsync(user);

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Data.Id);
    }
}

// ❌ Sai: mock DbActionExecutor trong integration test
var mockDb = new Mock<IDbActionExecutor>();
mockDb.Setup(d => d.AddAsync(It.IsAny<User>()))
      .ReturnsAsync(DbResult<User>.Ok(new User()));
// → test này không kiểm tra được DB behavior thực sự
```

---

## TS-03 — Test method phải theo naming `MethodName_Scenario_ExpectedResult`

```csharp
// ✅ Đúng
[Fact] public async Task Add_DuplicateEmail_ReturnsError() { }
[Fact] public async Task GetList_WithKeyword_FiltersResults() { }
[Fact] public async Task Delete_NonExistentId_AddsErrorToMessage() { }
[Fact] public async Task UpdateField_EmailField_UpdatesOnlyEmail() { }

// ❌ Sai
[Fact] public async Task TestAdd() { }
[Fact] public async Task Test1() { }
[Fact] public async Task AddTest_Success() { }
[Fact] public async Task ShouldAddUser() { }
```

---

## TS-04 — Mỗi test có một assertion chính (AAA pattern)

```csharp
// ✅ Đúng: một assertion rõ ràng
[Fact]
public async Task Add_ValidParam_ReturnsSavedUser()
{
    // Arrange
    var param = new UserParam { Name = "Alice", Email = "alice@example.com" };
    var sMessage = new ServiceMessage();

    // Act
    var result = await _service.Add(param, sMessage);

    // Assert
    Assert.False(sMessage.HasError);
}

// ❌ Sai: nhiều assertion không liên quan trong một test
[Fact]
public async Task Add_ValidParam_EverythingWorks()
{
    var result = await _service.Add(param, sMessage);
    Assert.False(sMessage.HasError);
    Assert.NotNull(result);
    Assert.Equal(param.Email, result.Email);
    Assert.NotEqual(Guid.Empty, result.Id);
    Assert.True(result.Log_CreatedDate > DateTimeOffset.MinValue);
    // quá nhiều — nếu fail không biết cái nào gây ra vấn đề
}
```

---

## TS-05 — Test project phải nằm trong `VegaBase.[Layer].Tests/` tương ứng

```
// ✅ Đúng: cấu trúc tương ứng với project được test
VegaBase.Service.Tests/
    Services/
        UserServiceTests.cs
        ProductServiceTests.cs
    Infrastructure/
        DbActionExecutorTests.cs

VegaBase.API.Tests/
    Controllers/
        UserControllerTests.cs

// ❌ Sai: tất cả trong một file hoặc không có cấu trúc
Tests/
    AllTests.cs
```

---

## Lưu ý: Test project chưa tồn tại trong VegaBase

Khi tạo test project, thêm vào solution:

```bash
dotnet new xunit -n VegaBase.Service.Tests
dotnet sln add VegaBase.Service.Tests/VegaBase.Service.Tests.csproj
```

Thêm reference đến project cần test:

```xml
<!-- VegaBase.Service.Tests.csproj -->
<ItemGroup>
  <ProjectReference Include="..\VegaBase.Service\VegaBase.Service.csproj" />
</ItemGroup>
```
