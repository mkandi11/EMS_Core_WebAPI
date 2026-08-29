using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using EMS_Core_WebAPI.Repositories;
using EMS_Core_WebAPI.Models;
using Microsoft.AspNetCore.Authorization;

namespace EMS_Core_WebAPI.Controllers
{
    [Authorize]
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

        // GET api/Employee/GetDepartments
        [HttpGet("GetDepartments", Name = "GetDepartments")]
        public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken = default)
        {
            var departments = await _employeeRepository.GetDepartments(cancellationToken).ConfigureAwait(false);
            return Ok(departments);
        }

        // GET api/Employee/GetEmployeeList
        [HttpGet("GetEmployeeList", Name = "GetEmployeeList")]
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

        // GET api/Employee/GetEmployeeDetails
        [HttpGet("GetEmployeeDetails/{EmployeeID}", Name = "GetEmployeeDetails")]
        public async Task<IActionResult> GetEmployeeDetails(int EmployeeID)
        {
            try
            {
                var employeeDetails = await _employeeRepository.GetEmployeeDetailsAsync(EmployeeID);
                return Ok(employeeDetails);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST api/Employee/AddEmployee
        [HttpPost("AddEmployee", Name = "AddEmployee")]
        public async Task<IActionResult> AddEmployee([FromBody] EmployeeCompleteDetails request, CancellationToken cancellationToken = default)
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

        // PUT api/Employee/UpdateEmployee
        [HttpPut("UpdateEmployee", Name = "UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeCompleteDetails request, CancellationToken cancellationToken = default)
        {
            if(request == null)
            {
                return BadRequest("Request body is required");
            }
            if(!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            try
            {
                var isUpdated = _employeeRepository.UpdateEmployeeAsync(request, cancellationToken).ConfigureAwait(false);
                return Ok(request.EmployeeID);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error updating employee");
                return BadRequest(ex.Message);
            }
        }

        // DELETE api/Employee/DeleteEmployee
        [HttpDelete("DeleteEmployee/{EmployeeID}", Name = "DeleteEmployee")]
        public async Task<IActionResult> DeleteEmployee(int EmployeeID)
        {
            try
            {
                var isDeleted = await _employeeRepository.DeleteEmployeeAsync(EmployeeID);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
