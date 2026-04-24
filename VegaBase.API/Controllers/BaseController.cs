// VegaBase.API/Controllers/BaseController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using VegaBase.Core.Common;
using VegaBase.Service.Models;
using VegaBase.Service.Services;

namespace VegaBase.API.Controllers;

[Authorize]
[ApiController]
public abstract class BaseController<TService, TModel, TParam> : ControllerBase
    where TService : IBaseService<TModel, TParam>
    where TParam   : BaseParamModel, new()
{
    protected readonly TService _service;

    protected BaseController(TService service)
    {
        _service = service;
    }

    protected void FillCallerInfo(BaseParamModel param)
        => Infrastructure.CallerInfoHelper.Fill(param, User);

    [HttpGet]
    public virtual async Task<IActionResult> GetList([FromQuery] TParam param, CancellationToken ct)
    {
        FillCallerInfo(param);
        var sMessage = new ServiceMessage();
        var result   = await _service.GetList(param, sMessage, ct);

        if (sMessage.HasError)
            return BadRequest(ApiResponse<TModel>.Fail(sMessage.Value));

        var totalPages = param.PageSize > 0
            ? (param.TotalCount + param.PageSize - 1) / param.PageSize
            : 0;
        return Ok(ApiResponse<TModel>.Ok(result, param.TotalCount, param.Page, param.PageSize, totalPages));
    }

    [HttpGet("{id:guid}")]
    public virtual async Task<IActionResult> GetItem(Guid id, [FromQuery] TParam param, CancellationToken ct)
    {
        FillCallerInfo(param);
        param.Id = id;
        var sMessage = new ServiceMessage();
        var result   = await _service.GetItem(param, sMessage, ct);

        if (sMessage.HasError)
            return BadRequest(ApiResponse<TModel>.Fail(sMessage.Value));

        var list = result != null ? new List<TModel> { result } : new List<TModel>();
        return Ok(ApiResponse<TModel>.Ok(list, list.Count));
    }

    [HttpPost]
    public virtual async Task<IActionResult> Add([FromBody] TParam param, CancellationToken ct)
    {
        FillCallerInfo(param);
        var sMessage = new ServiceMessage();
        var result   = await _service.Add(param, sMessage, ct);

        if (sMessage.HasError)
            return BadRequest(ApiResponse<TModel>.Fail(sMessage.Value));

        return Ok(ApiResponse<TModel>.Ok(result));
    }

    [HttpPost("UpdateField")]
    public virtual async Task<IActionResult> UpdateField([FromBody] TParam param, CancellationToken ct)
    {
        FillCallerInfo(param);

        try
        {
            Request.Body.Position = 0;
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            foreach (var rootProp in doc.RootElement.EnumerateObject())
            {
                if (!rootProp.Name.Equals("data", StringComparison.OrdinalIgnoreCase)) continue;
                if (rootProp.Value.ValueKind != JsonValueKind.Object) break;
                foreach (var prop in rootProp.Value.EnumerateObject())
                    param.UpdatedFields.Add(prop.Name);
                break;
            }
        }
        catch (JsonException)
        {
            return BadRequest(ApiResponse<TModel>.Fail("Dữ liệu không hợp lệ"));
        }

        var sMessage = new ServiceMessage();
        var result   = await _service.UpdateField(param, sMessage, ct);

        if (sMessage.HasError)
            return BadRequest(ApiResponse<TModel>.Fail(sMessage.Value));

        return Ok(ApiResponse<TModel>.Ok(result));
    }

    [HttpPost("Delete")]
    public virtual async Task<IActionResult> Delete([FromBody] TParam param, CancellationToken ct)
    {
        FillCallerInfo(param);
        var sMessage = new ServiceMessage();
        var result   = await _service.Delete(param, sMessage, ct);

        if (sMessage.HasError)
            return BadRequest(ApiResponse<TModel>.Fail(sMessage.Value));

        return Ok(ApiResponse<TModel>.Ok(result));
    }
}
