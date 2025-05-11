using System.Data;
using CW9.Model.DTOs;
using CW9.Services;
using Microsoft.Data.SqlClient;

namespace WarehouseService;
public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString;

    public WarehouseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public async Task<int> AddProductToWarehouseAsync(WarehouseRequest request)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var cmd = conn.CreateCommand();

        if (!await DoesProductExistAsync(cmd, request.ProductId))
            throw new Exception($"Product with ID {request.ProductId} does not exist.");

        if (!await DoesWarehouseExistAsync(cmd, request.WarehouseId))
            throw new Exception($"Warehouse with ID {request.WarehouseId} does not exist.");

        int idOrder = await FindMatchingOrderAsync(cmd, request);

        if (await IsOrderAlreadyFulfilledAsync(cmd, idOrder))
            throw new Exception($"Order with ID {idOrder} has already been fulfilled.");

        await MarkOrderAsFulfilledAsync(cmd, idOrder);

        decimal totalPrice = await CalculateTotalPriceAsync(cmd, request);

        int newId = await InsertProductWarehouseAsync(cmd, request, idOrder, totalPrice);

        return newId;
    }

    public async Task<int> AddProductToWarehouseUsingProcedureAsync(WarehouseRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "AddProductToWarehouse";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.Parameters.AddWithValue("@IdProduct", request.ProductId);
        cmd.Parameters.AddWithValue("@IdWarehouse", request.WarehouseId);
        cmd.Parameters.AddWithValue("@Amount", request.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
     

    private async Task<bool> DoesProductExistAsync(SqlCommand cmd, int productId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
        cmd.Parameters.AddWithValue("@IdProduct", productId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<bool> DoesWarehouseExistAsync(SqlCommand cmd, int warehouseId)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
        cmd.Parameters.AddWithValue("@IdWarehouse", warehouseId);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    private async Task<int> FindMatchingOrderAsync(SqlCommand cmd, WarehouseRequest request)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = @"
            SELECT TOP 1 IdOrder FROM [Order]
            WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt";

        cmd.Parameters.AddWithValue("@IdProduct", request.ProductId);
        cmd.Parameters.AddWithValue("@Amount", request.Amount);
        cmd.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new Exception("No matching order found.");

        return reader.GetInt32(reader.GetOrdinal("IdOrder"));
    }

    private async Task<bool> IsOrderAlreadyFulfilledAsync(SqlCommand cmd, int idOrder)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);

        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
    private async Task MarkOrderAsFulfilledAsync(SqlCommand cmd, int idOrder)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "UPDATE [Order] SET FulfilledAt = @Now WHERE IdOrder = @IdOrder";
        cmd.Parameters.AddWithValue("@Now", DateTime.Now);
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<decimal> CalculateTotalPriceAsync(SqlCommand cmd, WarehouseRequest request)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = "SELECT Price FROM Product WHERE IdProduct = @IdProduct";
        cmd.Parameters.AddWithValue("@IdProduct", request.ProductId);

        var result = await cmd.ExecuteScalarAsync();
        if (result == null)
            throw new Exception("Unable to retrieve product price.");

        decimal unitPrice = (decimal)result;
        return unitPrice * request.Amount;
    }

    private async Task<int> InsertProductWarehouseAsync(SqlCommand cmd, WarehouseRequest request, int idOrder, decimal totalPrice)
    {
        cmd.Parameters.Clear();
        cmd.CommandText = @"
            INSERT INTO Product_Warehouse
                (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
            OUTPUT INSERTED.IdProductWarehouse
            VALUES
                (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, @CreatedAt)";

        cmd.Parameters.AddWithValue("@IdWarehouse", request.WarehouseId);
        cmd.Parameters.AddWithValue("@IdProduct", request.ProductId);
        cmd.Parameters.AddWithValue("@IdOrder", idOrder);
        cmd.Parameters.AddWithValue("@Amount", request.Amount);
        cmd.Parameters.AddWithValue("@Price", totalPrice);
        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
}