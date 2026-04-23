# Database

Quy tắc thao tác với database — soft delete, audit, transaction, và primary key.

---

## DB-01 — Luôn dùng `SoftDeleteAsync`, không xóa cứng

```csharp
// ✅ Đúng: logical delete
await _db.SoftDeleteAsync(entity, param.CallerUsername);

// ❌ Sai: physical delete
_context.Remove(entity);
await _context.SaveChangesAsync();
```

---

## DB-02 — Không set audit fields thủ công trong service

`UnitOfWork` và `DbActionExecutor` tự động set `Log_CreatedBy`, `Log_UpdatedDate`, v.v. trước khi commit.

```csharp
// ✅ Đúng: để infrastructure tự xử lý
await _uow.Add(entity);
await _uow.SaveAsync(param.CallerUsername);

// ❌ Sai: set tay trong service
entity.Log_CreatedBy = param.CallerUsername;
entity.Log_CreatedDate = DateTimeOffset.UtcNow;
await _uow.Add(entity);
await _uow.SaveAsync(param.CallerUsername);
```

---

## DB-03 — Multi-entity operations phải dùng `IUnitOfWork` + `SaveAsync()`

```csharp
// ✅ Đúng: atomic transaction (Add requires createdBy as 2nd argument)
_uow.Add(order, param.CallerUsername);
_uow.Add(orderItem, param.CallerUsername);
_uow.Add(payment, param.CallerUsername);
await _uow.SaveAsync();

// ❌ Sai: 3 lần commit riêng biệt — không atomic
await _db.AddAsync(order);
await _db.AddAsync(orderItem);   // nếu lỗi ở đây, order đã được commit
await _db.AddAsync(payment);
```

---

## DB-04 — Single-entity operations dùng `IDbActionExecutor` trực tiếp

```csharp
// ✅ Đúng
var result = await _db.AddAsync(entity);
if (!result.IsSuccess) { sMessage += "Lỗi thêm dữ liệu."; return; }

// ❌ Sai: thao tác DbContext trực tiếp
_context.Add(entity);
await _context.SaveChangesAsync();
```

---

## DB-05 — Raw SQL phải có comment giải thích lý do

```csharp
// ✅ Đúng: raw SQL với lý do rõ ràng
// EF Core generates N individual UPDATE statements for batch; raw SQL is O(1)
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE Products SET Stock = Stock - @qty WHERE Id = @id",
    new SqlParameter("@qty", quantity),
    new SqlParameter("@id", productId));

// ❌ Sai: raw SQL không có comment
await _context.Database.ExecuteSqlRawAsync(
    "DELETE FROM Logs WHERE CreatedDate < @cutoff",
    new SqlParameter("@cutoff", cutoff));
```

---

## DB-06 — Luôn filter `IsDeleted == false` trong `ApplyFilter`

```csharp
// ✅ Đúng
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    query = query.Where(u => !u.IsDeleted);
    if (!string.IsNullOrEmpty(param.Keyword))
        query = query.Where(u => u.Name.Contains(param.Keyword));
    return query;
}

// ❌ Sai: quên filter IsDeleted — trả về cả record đã xóa
protected override IQueryable<User> ApplyFilter(IQueryable<User> query, UserParam param)
{
    return query.Where(u => u.Name.Contains(param.Keyword));
}
```

---

## DB-07 — Primary key là UUIDv7 tự sinh từ `BaseEntity`, không dùng int identity

```csharp
// ✅ Đúng: không gán Id (BaseEntity tự sinh UUIDv7)
var entity = new User { Name = param.Name, Email = param.Email };
await _db.AddAsync(entity);

// ❌ Sai: gán Id thủ công
var entity = new User { Id = Guid.NewGuid(), Name = param.Name }; // UUIDv4, không phải v7
var entity = new User { Id = someIntId }; // int identity không hỗ trợ
```
