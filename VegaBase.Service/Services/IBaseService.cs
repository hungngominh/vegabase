using VegaBase.Core.Common;
using VegaBase.Service.Models;

namespace VegaBase.Service.Services;

public interface IBaseService<TModel, TParam> where TParam : BaseParamModel
{
    Task<List<TModel>>  GetList(TParam param, ServiceMessage sMessage, CancellationToken ct = default);
    Task<TModel?>       GetItem(TParam param, ServiceMessage sMessage, CancellationToken ct = default);
    Task<List<TModel>?> Add(TParam param, ServiceMessage sMessage, CancellationToken ct = default);
    Task<List<TModel>?> UpdateField(TParam param, ServiceMessage sMessage, CancellationToken ct = default);
    Task<List<TModel>?> Delete(TParam param, ServiceMessage sMessage, CancellationToken ct = default);
}
