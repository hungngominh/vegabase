# Entity & DbContext

Quy tắc định nghĩa entity và cấu hình AppDbContext cho consumer dùng VegaBase.

> **Prerequisite:** Đọc [06-database.md](../06-database.md) cho quy tắc soft-delete và audit fields.

---

## NS-09 — Namespace entity theo project

Entity thuộc `{App}.Core.Entities`. Import `using VegaBase.Core.Entities;` chỉ xuất hiện trong `{App}.Core`.

```csharp
// CORRECT — LuxCar.Core/Entities/Vehicle.cs
using VegaBase.Core.Entities;
namespace LuxCar.Core.Entities;

public class Vehicle : BaseEntity { ... }
```

```csharp
// WRONG — entity trong Service project
namespace LuxCar.Service.Entities;
public class Vehicle : BaseEntity { ... }
```

---

## DB-08 — Inherit BaseEntity bắt buộc

Mọi entity phải kế thừa `BaseEntity`:

```csharp
using VegaBase.Core.Entities;
namespace {App}.Core.Entities;

public class Vehicle : BaseEntity
{
    public string Name         { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    // ... domain fields only
}
```

`BaseEntity` cung cấp sẵn:
- `Id` — `Guid`, tự set bằng `Guid.CreateVersion7()` khi tạo mới
- `IsDeleted` — `bool`, dùng cho soft delete
- `Log_CreatedDate`, `Log_CreatedBy` — audit created
- `Log_UpdatedDate`, `Log_UpdatedBy` — audit updated

`BaseEntity` cũng cung cấp sẵn:
- `RowVersion` — `byte[]` với `[Timestamp]`, dùng cho optimistic concurrency

**Không tự khai báo lại các field trên** → EF migration conflict, duplicate column.

> **PostgreSQL consumers:** `[Timestamp]` không map tự động sang `xmin` trên PostgreSQL. Phải cấu hình trong `OnModelCreating`:
> ```csharp
> modelBuilder.Entity<Vehicle>()
>     .Property(e => e.RowVersion)
>     .IsRowVersion();
> ```
> SQL Server: không cần thêm gì, `[Timestamp]` tự map sang `rowversion`.
> Consumers phải chạy migration để thêm cột `RowVersion` vào tất cả bảng.

---

## DB-09 — HasQueryFilter trên mọi entity

Mỗi `DbSet` mới phải có global query filter trong `OnModelCreating`:

```csharp
// {App}.Core/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply to EVERY entity
    modelBuilder.Entity<Vehicle>()      .HasQueryFilter(e => !e.IsDeleted);
    modelBuilder.Entity<VehicleSpec>()  .HasQueryFilter(e => !e.IsDeleted);
    modelBuilder.Entity<VehicleImage>() .HasQueryFilter(e => !e.IsDeleted);
    // ... every DbSet
}
```

Thiếu filter → query bình thường trả về cả bản ghi đã xóa mềm. Ảnh hưởng mọi operation: list, get, duplicate check đều sai.

Để query không có filter (ví dụ: seed, admin check): dùng `.IgnoreQueryFilters()`.

---

## DB-10 — Unique index phải kèm HasFilter

Mọi unique index trên entity phải dùng partial filter:

```csharp
// PostgreSQL syntax (Npgsql)
modelBuilder.Entity<User>(b =>
{
    b.HasIndex(u => u.Username)
     .IsUnique()
     .HasFilter("\"IsDeleted\" = false");
});

// SQL Server syntax
modelBuilder.Entity<User>(b =>
{
    b.HasIndex(u => u.Username)
     .IsUnique()
     .HasFilter("[IsDeleted] = 0");
});
```

**Tại sao:** Soft-deleted record vẫn chiếm slot trong index thường. Khi tạo lại record cùng username → lỗi `23505 duplicate key` (PostgreSQL) dù record cũ đã `IsDeleted=true`.

Với composite unique index:
```csharp
b.HasIndex(e => new { e.VehicleId, e.TagType, e.TagCode })
 .IsUnique()
 .HasFilter("\"IsDeleted\" = false");
```

---

## DB-11 — Guid PK không auto-generate

Trong `OnModelCreating` phải tắt auto-generation cho mọi Guid PK:

```csharp
// {App}.Core/Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Npgsql fix: PostgreSQL cannot use IDENTITY for uuid columns
    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
        var idProp = entityType.FindProperty("Id");
        if (idProp != null && idProp.ClrType == typeof(Guid))
            idProp.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
    }

    // ... rest of OnModelCreating
}
```

**Lý do:** Npgsql mặc định áp `ValueGeneratedOnAdd()` cho mọi PK. PostgreSQL chỉ hỗ trợ `IDENTITY` cho `smallint/int/bigint`, không cho `uuid` → migration fail. Convention loop này tự động áp cho tất cả entity hiện tại và tương lai.

ID được generate phía app: `BaseEntity` tự gọi `Guid.CreateVersion7()` trong constructor.

---

## DB-12 — Override SaveChanges: soft-delete

`AppDbContext` phải override cả hai `SaveChanges` để convert hard-delete thành soft-delete:

```csharp
// {App}.Core/Data/AppDbContext.cs
public override int SaveChanges()
{
    ConvertDeleteToSoftDelete();
    return base.SaveChanges();
}

public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    ConvertDeleteToSoftDelete();
    return base.SaveChangesAsync(cancellationToken);
}

private void ConvertDeleteToSoftDelete()
{
    var deleted = ChangeTracker
        .Entries<VegaBase.Core.Entities.BaseEntity>()
        .Where(e => e.State == EntityState.Deleted);

    foreach (var entry in deleted)
    {
        entry.State            = EntityState.Modified;
        entry.Entity.IsDeleted = true;
    }
}
```

Đây là phòng thủ cuối: tránh `.Remove()` vô tình hard-delete. Services cũng set `IsDeleted` thủ công trước, nhưng lớp này đảm bảo không ai bypass.

---

## DB-13 — Decimal phải khai báo precision

Mọi `decimal` property phải khai báo precision trong `OnModelCreating`:

```csharp
modelBuilder.Entity<VehicleRentalConfig>(b =>
{
    b.Property(e => e.Deposit).HasPrecision(18, 2);
});

modelBuilder.Entity<VehicleServiceTypePrice>(b =>
{
    b.Property(e => e.PricePerDay).HasPrecision(18, 2);
    b.Property(e => e.OriginalPrice).HasPrecision(18, 2);
});
```

Thiếu → EF tạo cột `numeric` không có precision trên PostgreSQL → EF warning `No store type was specified for the decimal property` + tiềm ẩn mất dữ liệu thập phân khi precision thay đổi ở migration sau.
