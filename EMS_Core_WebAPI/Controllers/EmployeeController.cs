using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EMS_Core_WebAPI.Repositories;
using EMS_Core_WebAPI.Models;

namespace EMS_Core_WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ILogger<EmployeeController> _logger;
        private readonly EmployeeRepository _employeeRepository;
        public EmployeeController(ILogger<EmployeeController> logger, EmployeeRepository employeeRepository) 
        {
            _logger = logger;
            _employeeRepository = employeeRepository;
        }

        [HttpGet(Name = "GetDepartments")]
        public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken = default)
        {
            var departments = await _employeeRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
            return Ok(departments);
        }

        [HttpGet(Name = "GetEmployeeList")]
        public async Task<IActionResult> GetEmployeeList()
        {
            try
            {
                var employeeList = await _employeeRepository.GetEmployeeListAsync();
                return Ok(employeeList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost(Name = "AddEmployee")]
        public async Task<ActionResult> AddEmployee([FromBody] AddEmployeeRequest request, CancellationToken cancellationToken = default)
        {
            if(request == null)
            {
                return BadRequest("Request body is required.");
            }
            if(!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            try
            {
                var newId = await _employeeRepository.AddEmployeeAsync(request, cancellationToken).ConfigureAwait(false);
                // Return 200 with New ID
                return Ok(new {EmployeeID = newId});
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                return BadRequest(ex.Message);
            }
        }
    }
}
