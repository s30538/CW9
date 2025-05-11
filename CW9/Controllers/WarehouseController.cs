using CW9.Model.DTOs;
using CW9.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CW9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehouseController : ControllerBase
    {
        private readonly IWarehouseService _service;

        public WarehouseController(IWarehouseService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct([FromBody] WarehouseRequest request)
        {
            try
            {
                var newId = await _service.AddProductToWarehouseAsync(request);
                return Ok(newId);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddProductWithSP([FromBody] WarehouseRequest request)
        {
            try
            {
                var id = await _service.AddProductToWarehouseUsingProcedureAsync(request);
                return Ok(id);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
