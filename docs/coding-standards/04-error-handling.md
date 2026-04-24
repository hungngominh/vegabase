# Error Handling

Quy tắc xử lý lỗi trong VegaBase — phân biệt rõ business error vs infrastructure error.

## Phân loại lỗi

| Loại | Xử lý bằng | Layer |
|---|---|---|
| Business validation | `ServiceMessage` | Service |
| DB operation result | `DbResult<T>` | Service / Infrastructure |
| Unexpected / infrastructure | `throw Exception` | Middleware bắt |
| HTTP response lỗi | `ApiResponse<T>.Fail()` | Controller |

---

## EH-01 — Dùng `ServiceMessage` cho business validation errors, không throw exception

```csharp
// ✅ Đúng
protected override async Task CheckAddCondition(UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email))
        sMessage += "Email không được để trống.";
}

// ❌ Sai
protected override async Task CheckAddCondition(UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email))
        throw new ArgumentException("Email không được để trống.");
}
```

---

## EH-02 — Không return sớm sau lỗi đầu tiên

> **Lưu ý về ServiceMessage:** Toán tử `+=` chỉ lưu lỗi **đầu tiên** — nếu `sMessage.Value` đã có nội dung, các lỗi sau bị bỏ qua. Để trả về nhiều lỗi, nối thủ công: `sMessage.Value += (sMessage.HasError ? " | " : "") + "Lỗi tiếp theo.";`

```csharp
// ✅ Đúng: không return sớm để chạy hết tất cả validation
protected override async Task CheckAddCondition(UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email))
        sMessage += "Email không được để trống.";
    if (string.IsNullOrEmpty(param.Name))
        sMessage += "Tên không được để trống."; // chỉ lưu nếu Email chưa có lỗi
    if (param.Age < 18)
        sMessage += "Phải đủ 18 tuổi.";
    // Để trả về nhiều lỗi cùng lúc, dùng:
    // sMessage.Value += (sMessage.HasError ? " | " : "") + "Lỗi mới.";
}

// ❌ Sai: return sau lỗi đầu tiên, các validation sau không được chạy
protected override async Task CheckAddCondition(UserParam param, ServiceMessage sMessage)
{
    if (string.IsNullOrEmpty(param.Email)) { sMessage += "Email trống."; return; }
    if (string.IsNullOrEmpty(param.Name)) { sMessage += "Tên trống."; return; }
}
```

---

## EH-03 — Luôn kiểm tra `DbResult.IsSuccess` trước khi dùng `DbResult.Data`

```csharp
// ✅ Đúng
var result = await _executor.AddAsync(entity, CallerUsername);
if (!result.IsSuccess)
{
    sMessage += result.Error?.ToString() ?? "Lỗi khi thêm dữ liệu.";
    return;
}
var saved = result.Data;

// ❌ Sai: dùng Data mà không kiểm tra IsSuccess
var result = await _executor.AddAsync(entity, CallerUsername);
var saved = result.Data; // NullReferenceException nếu IsSuccess = false
```

---

## EH-04 — Không dùng exception để điều khiển business flow

```csharp
// ✅ Đúng: flow nghiệp vụ qua ServiceMessage
if (!hasPermission)
{
    sMessage += "Bạn không có quyền thực hiện thao tác này.";
    return;
}

// ❌ Sai: exception cho flow nghiệp vụ
if (!hasPermission)
    throw new UnauthorizedAccessException("Bạn không có quyền.");
```

---

## EH-05 — Không swallow exceptions (catch rỗng)

```csharp
// ✅ Đúng: log và re-throw hoặc xử lý có ý nghĩa
try
{
    await DoSomethingAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Lỗi khi thực hiện DoSomething");
    throw;
}

// ❌ Sai: catch rỗng
try
{
    await DoSomethingAsync();
}
catch (Exception) { }  // lỗi bị nuốt, không ai biết
```

---

## EH-06 — Không trả stack trace hoặc inner exception ra client

```csharp
// ✅ Đúng: message chung chung
return BadRequest(ApiResponse<object>.Fail("Đã xảy ra lỗi. Vui lòng thử lại."));

// ❌ Sai: lộ nội bộ
return BadRequest(ApiResponse<object>.Fail(ex.StackTrace));
return BadRequest(ApiResponse<object>.Fail(ex.InnerException?.Message));
```

---

## EH-07 — Không log cùng một exception hai lần trong middleware

Khi `ExceptionHandlingMiddleware` bắt exception, nó log ở `LogError` trước khi kiểm tra `Response.HasStarted`. Nếu response đã gửi, chỉ log thêm `LogWarning` **không kèm exception** (exception đã có trong log trước đó). Log exception hai lần tạo noise và làm alert system kích hoạt nhân đôi.

```csharp
// ✅ Đúng: exception log 1 lần ở LogError; nếu response đã gửi thì LogWarning không kèm ex
_logger.LogError(ex, "Unhandled exception: {Message} [TraceId={TraceId}]", safeMessage, traceId);
if (context.Response.HasStarted)
{
    _logger.LogWarning("Response already started — cannot write 500 [TraceId={TraceId}]", traceId);
    throw;
}

// ❌ Sai: log exception 2 lần → double alert
_logger.LogError(ex, "Unhandled exception...");
if (context.Response.HasStarted)
    _logger.LogError(ex, "Exception after response started..."); // ex đã log rồi
```
