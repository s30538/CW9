using CW9.Model.DTOs;

namespace CW9.Services;

public interface IWarehouseService
{
    Task<int> AddProductToWarehouseAsync(WarehouseRequest request);
    Task<int> AddProductToWarehouseUsingProcedureAsync(WarehouseRequest request);
}