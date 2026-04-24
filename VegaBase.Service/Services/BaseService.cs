using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VegaBase.Core.Common;
using VegaBase.Core.Entities;
using VegaBase.Service.Infrastructure.DbActions;
using VegaBase.Service.Models;
using VegaBase.Service.Permissions;

namespace VegaBase.Service.Services;

public abstract class BaseService<TEntity, TModel, TParam> : IBaseService<TModel, TParam>
    where TEntity : BaseEntity, new()
    where TModel  : new()
    where TParam  : BaseParamModel
{
    protected abstract string ScreenCode { get; }

    protected readonly IDbActionExecutor _executor;
    private readonly IPermissionCache _permissionCache;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger _logger;

    protected BaseService(
        IDbActionExecutor executor,
        IPermissionCache permissionCache,
        IHttpContextAccessor httpContextAccessor,
        ILogger logger)
    {
        _executor            = executor;
        _permissionCache     = permissionCache;
        _httpContextAccessor = httpContextAccessor;
        _logger              = logger;
    }

    protected bool HandleResult<T>(DbResult<T> result, ServiceMessage sMessage)
    {
        if (result.IsSuccess) return true;
        sMessage += result.Error?.Message ?? "Lỗi không xác định khi thao tác cơ sở dữ liệu";
        return false;
    }

    protected string CallerUsername =>
        _httpContextAccessor.HttpContext?
            .User?.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

    protected string CallerRole =>
        _httpContextAccessor.HttpContext?
            .User?.FindFirst("roleCode")?.Value ?? string.Empty;

    protected List<Guid> CallerRoleIds =>
        _httpContextAccessor.HttpContext?
            .User?.FindAll("roleId")
            .Select(c => Guid.TryParse(c.Value, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList() ?? new List<Guid>();

    protected void CheckPermission(PermissionCheckParam param, ServiceMessage sMessage)
    {
        if (string.Equals(param.CallerRole, "admin", StringComparison.OrdinalIgnoreCase)) return;

        if (!_permissionCache.HasPermission(param.CallerRoleIds, ScreenCode, param.Action))
            sMessage += $"Bạn không có quyền thực hiện thao tác này " +
                        $"(Màn hình: {ScreenCode}, Hành động: {param.Action})";
    }

    protected PermissionCheckParam PermParam(BaseParamModel source, string action) => new()
    {
        CallerUsername = source.CallerUsername,
        CallerRole     = source.CallerRole,
        CallerRoleIds  = source.CallerRoleIds,
        Action         = action
    };

    public async Task<List<TModel>> GetList(TParam param, ServiceMessage sMessage)
    {
        var models = await GetListCore(param, sMessage);
        if (!sMessage.HasError)
            await RefineListData(models, param, sMessage);
        return models;
    }

    protected virtual async Task<List<TModel>> GetListCore(TParam param, ServiceMessage sMessage)
    {
        CheckPermission(PermParam(param, "View"), sMessage);
        if (sMessage.HasError) return [];

        var result = await _executor.QueryAsync<TEntity>(q =>
        {
            var filtered = ApplyFilter(q.Where(e => !e.IsDeleted), param);
            param.TotalCount = filtered.Count();
            return filtered
                .Skip((param.Page - 1) * param.PageSize)
                .Take(param.PageSize);
        });

        if (!HandleResult(result, sMessage)) return [];
        return result.Data!.Select(ConvertToModel).ToList();
    }

    public virtual async Task<TModel?> GetItem(TParam param, ServiceMessage sMessage)
    {
        CheckPermission(PermParam(param, "View"), sMessage);
        if (sMessage.HasError) return default;

        if (param.Id is null) { sMessage += "Id là bắt buộc"; return default; }
        var result = await _executor.GetByIdAsync<TEntity>(param.Id.Value);
        if (!HandleResult(result, sMessage)) return default;
        if (result.Data == null) { sMessage += "Không tìm thấy dữ liệu"; return default; }

        return ConvertToModel(result.Data);
    }

    public virtual async Task<List<TModel>?> Add(TParam param, ServiceMessage sMessage)
    {
        CheckPermission(PermParam(param, "Create"), sMessage);
        if (sMessage.HasError) return null;

        await CheckAddCondition(param, sMessage);
        if (sMessage.HasError) return null;

        var data = GetAddData(param);
        if (data is null) { sMessage += "Dữ liệu thêm mới là bắt buộc"; return null; }

        var entity = ConvertToEntity(data);
        var result = await _executor.AddAsync(entity, CallerUsername);
        if (!HandleResult(result, sMessage)) return null;

        SafeOnChanged(nameof(Add));
        return [ConvertToModel(result.Data!)];
    }

    public virtual async Task<List<TModel>?> UpdateField(TParam param, ServiceMessage sMessage)
    {
        CheckPermission(PermParam(param, "Edit"), sMessage);
        if (sMessage.HasError) return null;

        if (param.Id is null) { sMessage += "Id là bắt buộc"; return null; }
        var findResult = await _executor.GetByIdAsync<TEntity>(param.Id.Value, tracked: true);
        if (!HandleResult(findResult, sMessage)) return null;
        if (findResult.Data == null) { sMessage += "Không tìm thấy dữ liệu"; return null; }

        var entity = findResult.Data;
        await CheckUpdateCondition(param, sMessage);
        if (sMessage.HasError) return null;

        var changed = ApplyUpdate(entity, param);
        if (!changed)
        {
            SafeOnChanged(nameof(UpdateField));
            return [ConvertToModel(entity)];
        }

        var updateResult = await _executor.UpdateAsync(entity, CallerUsername);
        if (!HandleResult(updateResult, sMessage)) return null;

        SafeOnChanged(nameof(UpdateField));
        return [ConvertToModel(entity)];
    }

    public virtual async Task<List<TModel>?> Delete(TParam param, ServiceMessage sMessage)
    {
        CheckPermission(PermParam(param, "Delete"), sMessage);
        if (sMessage.HasError) return null;

        if (param.Id is null) { sMessage += "Id là bắt buộc"; return null; }
        var findResult = await _executor.GetByIdAsync<TEntity>(param.Id.Value, tracked: true);
        if (!HandleResult(findResult, sMessage)) return null;
        if (findResult.Data == null) { sMessage += "Không tìm thấy dữ liệu"; return null; }

        if (findResult.Data.IsDeleted) return [];

        var deleteResult = await _executor.SoftDeleteAsync(findResult.Data, CallerUsername);
        if (!HandleResult(deleteResult, sMessage)) return null;

        SafeOnChanged(nameof(Delete));
        return [];
    }

    // ── Hooks ────────────────────────────────────────────────────

    /// <summary>
    /// Called after every successful Add, UpdateField, or Delete.
    /// Override to invalidate caches or trigger side-effects.
    /// Exceptions are caught and logged — the original operation is not rolled back.
    /// </summary>
    protected virtual void OnChanged() { }

    private void SafeOnChanged(string operation)
    {
        try { OnChanged(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnChanged failed after successful {Operation}", operation);
        }
    }

    protected virtual IQueryable<TEntity> ApplyFilter(IQueryable<TEntity> query, TParam param) => query;

    protected virtual Task RefineListData(List<TModel> models, TParam param, ServiceMessage sMessage)
        => Task.CompletedTask;

    protected virtual Task CheckAddCondition(TParam param, ServiceMessage sMessage)
        => Task.CompletedTask;

    protected virtual Task CheckUpdateCondition(TParam param, ServiceMessage sMessage)
        => Task.CompletedTask;

    /// <summary>
    /// Apply partial-update fields to the tracked entity.
    /// Returns <c>true</c> if at least one field was assigned (so the caller can skip the DB round-trip otherwise).
    /// Default implementation delegates to <see cref="AutoApplyUpdate"/>.
    /// </summary>
    protected virtual bool ApplyUpdate(TEntity entity, TParam param)
        => AutoApplyUpdate(entity, param);

    protected bool AutoApplyUpdate(TEntity entity, TParam param)
    {
        var dataProp = param.GetType().GetProperty("Data");
        var data     = dataProp?.GetValue(param);
        if (data == null)
        {
            _logger.LogWarning("[AutoApplyUpdate] param.Data is NULL — skipping.");
            return false;
        }

        var entityType  = entity.GetType();
        var sourceProps = data.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Id", "IsDeleted", "Log_CreatedDate", "Log_CreatedBy", "Log_UpdatedDate", "Log_UpdatedBy" };

        var changed = false;
        foreach (var src in sourceProps)
        {
            if (skip.Contains(src.Name)) continue;
            if (!param.HasField(src.Name)) continue;

            var dst = entityType.GetProperty(src.Name, BindingFlags.Public | BindingFlags.Instance);
            if (dst == null || !dst.CanWrite) continue;

            var dstType = Nullable.GetUnderlyingType(dst.PropertyType) ?? dst.PropertyType;
            var srcType = Nullable.GetUnderlyingType(src.PropertyType) ?? src.PropertyType;
            if (!dst.PropertyType.IsAssignableFrom(src.PropertyType) && dstType != srcType)
            {
                _logger.LogDebug("[AutoApplyUpdate] Skipping {Prop}: type mismatch {Src} -> {Dst}",
                    src.Name, src.PropertyType.Name, dst.PropertyType.Name);
                continue;
            }

            dst.SetValue(entity, src.GetValue(data));
            changed = true;
        }
        return changed;
    }

    // ── Abstract ─────────────────────────────────────────────────

    protected abstract TModel GetAddData(TParam param);

    // ── Conversion ───────────────────────────────────────────────

    public virtual TModel ConvertToModel(TEntity entity)
        => AutoMap<TModel>(entity);

    public virtual TEntity ConvertToEntity(TModel model)
        => AutoMap<TEntity>(model ?? throw new ArgumentNullException(nameof(model)), skipAudit: true);

    protected static TDest AutoMap<TDest>(object source, bool skipAudit = false)
        where TDest : new()
    {
        var dest      = new TDest();
        var destType  = typeof(TDest);
        var srcProps  = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        var skip = skipAudit
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "Id", "IsDeleted", "Log_CreatedDate", "Log_CreatedBy", "Log_UpdatedDate", "Log_UpdatedBy" }
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { "IsDeleted" };

        foreach (var src in srcProps)
        {
            if (skip.Contains(src.Name)) continue;

            var dst = destType.GetProperty(src.Name, BindingFlags.Public | BindingFlags.Instance);
            if (dst == null || !dst.CanWrite) continue;

            var value = src.GetValue(source);
            if (value == null)
            {
                if (!dst.PropertyType.IsValueType ||
                    Nullable.GetUnderlyingType(dst.PropertyType) != null)
                    dst.SetValue(dest, null);
            }
            else if (dst.PropertyType.IsAssignableFrom(src.PropertyType))
            {
                dst.SetValue(dest, value);
            }
        }

        return dest;
    }
}
