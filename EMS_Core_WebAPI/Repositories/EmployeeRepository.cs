using Microsoft.Data.SqlClient;
using System.Data;
using EMS_Core_WebAPI.Helper;
using EMS_Core_WebAPI.Models;

namespace EMS_Core_WebAPI.Repositories
{
    public class EmployeeRepository
    {
        private readonly DBConnection _db;

        public EmployeeRepository(DBConnection db)
        {
            _db = db;
        }

        public Task<List<Department>> GetDepartments(CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT DepartmentID, DepartmentName FROM [Department]";
            return _db.ExecuteReaderAsync(
                sql,
                reader => new Department
                {
                    DepartmentID = reader.GetInt32(0),
                    DepartmentName = reader.IsDBNull(1) ? null : reader.GetString(1)
                },
                parameters: null,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken
            );
        }
        public async Task<List<Employee>> GetEmployeeListAsync(CancellationToken cancellationToken = default)
        {
            const string spName = "SP_GetAllEmployees";
            // Use the DBConnection overload that returns a list of dictionaries (dynamic columns)
            var rows = await _db.ExecuteReaderAsync(
                spName, 
                parameters: null,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
                ).ConfigureAwait(false);

            var result = new List<Employee>(rows.Count);
            foreach ( var row in rows)
            {
                var employee = new Employee
                {
                    EmployeeID = row.TryGetValue("EmployeeID", out var idObj) && idObj is not null && idObj != DBNull.Value
                    ? Convert.ToInt32(idObj) 
                    : 0,
                    FirstName = row.TryGetValue("FirstName", out var fn) && fn is not null && fn != DBNull.Value
                    ? fn.ToString() : null,
                    LastName = row.TryGetValue("LastName", out var ln) && ln is not null && ln != DBNull.Value
                    ? ln.ToString() : null,
                    Email = row.TryGetValue("Email", out var em) && em is not null && em != DBNull.Value
                    ? em.ToString() : null,
                    Company = row.TryGetValue("Company", out var co) && co is not null && co != DBNull.Value
                    ? co.ToString() : null,
                    Department = row.TryGetValue("DepartmentName", out var dn) && dn is not null && dn != DBNull.Value
                    ? dn.ToString() : null,
                    ExperienceInMonths = row.TryGetValue("ExperienceInMonths", out var exp) && exp is not null && exp != DBNull.Value
                    ? Convert.ToInt32(exp) : 0,
                };
                result.Add(employee);
            }
            return result;
        }

        public async Task<EmployeeCompleteDetails> GetEmployeeDetailsAsync(int EmployeeID, CancellationToken cancellationToken = default)
        {
            const string spName = "GetEmployeeProfile";
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@EmployeeID", SqlDbType.Int) { Value = EmployeeID},
            };
            // Use the DBConnection overload that returns a list of dictionaries (dynamic columns)
            var rows = await _db.ExecuteReaderAsync(
                spName,
                parameters: parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
                ).ConfigureAwait(false);

            if (rows.Count == 0)
                return new EmployeeCompleteDetails(); // or return null if preferred

            var row = rows[0]; // first record

            var employeeDetails = new EmployeeCompleteDetails
            {
                EmployeeID = row.TryGetValue("EmployeeID", out var idObj) && idObj != DBNull.Value
                    ? Convert.ToInt32(idObj)
                    : 0,

                FirstName = row.TryGetValue("FirstName", out object? fn) && fn != DBNull.Value
                    ? fn.ToString()
                    : null,

                LastName = row.TryGetValue("LastName", out var ln) && ln != DBNull.Value
                    ? ln.ToString()
                    : null,

                Email = row.TryGetValue("Email", out var em) && em != DBNull.Value
                    ? em.ToString()
                    : null,

                Company = row.TryGetValue("Company", out var co) && co != DBNull.Value
                    ? co.ToString()
                    : null,

                DepartmentID = row.TryGetValue("DepartmentID", out var Did) && Did != DBNull.Value
                    ? Convert.ToInt32(Did)
                    : 0,

                DepartmentName = row.TryGetValue("DepartmentName", out var dn) && dn != DBNull.Value
                    ? dn.ToString()
                    : null,

                ExperienceInMonths = row.TryGetValue("ExperienceInMonths", out var exp) && exp != DBNull.Value
                    ? Convert.ToInt32(exp)
                    : 0,

                Salary = new Salary
                {
                    Basic = row.TryGetValue("Basic", out var basic) && basic != DBNull.Value
                        ? Convert.ToDecimal(basic)
                        : (decimal?)null,
                    HRA = row.TryGetValue("HRA", out var hra) && hra != DBNull.Value
                        ? Convert.ToDecimal(hra)
                        : (decimal?)null,
                    Misc = row.TryGetValue("Misc", out var misc) && misc != DBNull.Value
                        ? Convert.ToDecimal(misc)
                        : (decimal?)null
                },

                Skills = row.TryGetValue("Skills", out var skillsObj) && skillsObj != DBNull.Value
                    ? skillsObj.ToString()?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>(),

                Age = row.TryGetValue("Age", out var ageObj) && ageObj != DBNull.Value
                    ? Convert.ToInt32(ageObj)
                    : null,

                Gender = row.TryGetValue("Gender", out var genderObj) && genderObj != DBNull.Value
                    ? genderObj.ToString()
                    : null,

                Address = new Address
                {
                    City = row.TryGetValue("City", out var cityObj) && cityObj != DBNull.Value
                    ? cityObj.ToString()
                    : null,

                    Country = row.TryGetValue("Country", out var countryObj) && countryObj != DBNull.Value
                    ? countryObj.ToString()
                    : null
                }
            };
            return employeeDetails;
        }

        // Calls stored procedure AddFullEmployeeProfile and returns new EmployeeID
        public async Task<int> AddEmployeeAsync(EmployeeCompleteDetails request, CancellationToken cancellationToken = default)
        {
            const string spName = "AddFullEmployeeProfile";
            // request.Skills is a string[], convert it into a comma‑separated string before assigning it to the @Skills parameter
            var skillsCsv = request.Skills != null && request.Skills.Length > 0
                ? string.Join(",", request.Skills)
                : null;

            try
            {
                var parameters = new List<SqlParameter>
        {
            new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = (object?)request.FirstName ?? DBNull.Value },
            new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = (object?)request.LastName ?? DBNull.Value },
            new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = (object?)request.Email ?? DBNull.Value },
            new SqlParameter("@Company", SqlDbType.NVarChar, 100) { Value = (object?)request.Company ?? DBNull.Value },
            new SqlParameter("@DepartmentID", SqlDbType.Int) { Value = request.DepartmentID },
            new SqlParameter("@ExperienceInMonths", SqlDbType.Int) { Value = (object?)request.ExperienceInMonths ?? DBNull.Value },

            new SqlParameter("@Basic", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = request.Salary?.Basic ?? (object)DBNull.Value },
            new SqlParameter("@HRA", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = request.Salary?.HRA ?? (object)DBNull.Value },
            new SqlParameter("@Misc", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)request.Salary?.Misc ?? DBNull.Value },

            new SqlParameter("@Skills", SqlDbType.NVarChar, -1) { Value = (object?)skillsCsv ?? DBNull.Value },

            new SqlParameter("@Age", SqlDbType.Int) { Value = (object?)request.Age ?? DBNull.Value },
            new SqlParameter("@Gender", SqlDbType.NVarChar, 20) { Value = (object?)request.Gender ?? DBNull.Value },
            new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = (object?)request.Address?.City ?? DBNull.Value },
            new SqlParameter("@Country", SqlDbType.NVarChar, 100) { Value = (object?)request.Address?.Country ?? DBNull.Value },
        };

                var scalar = await _db.ExecuteScalarAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (scalar == null || scalar == DBNull.Value)
                {
                    throw new InvalidOperationException("Stored procedure did not return the new EmployeeID.");
                }

                return Convert.ToInt32(scalar);
            }
            catch (SqlException ex)
            {
                // Log SQL-specific errors (unique constraint, FK violation, etc.)
                throw new Exception($"SQL error occurred while executing {spName}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                throw new Exception($"Unexpected error occurred while adding employee profile: {ex.Message}", ex);
            }
        }

        // Calls stored procedure UpdateFullEmployeeProfile and returns true if update succeeded
        public async Task<bool> UpdateEmployeeAsync(EmployeeCompleteDetails request, CancellationToken cancellationToken = default)
        {
            const string spName = "UpdateFullEmployeeProfile";
            var skillsCsv = request.Skills != null && request.Skills.Length > 0
                ? string.Join(",", request.Skills)
                : null;

            try
            {
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@EmployeeID", SqlDbType.Int) { Value = request.EmployeeID },
                    new SqlParameter("@FirstName", SqlDbType.VarChar, 100) { Value = (object?)request.FirstName ?? DBNull.Value },
                    new SqlParameter("@LastName", SqlDbType.VarChar, 100) { Value = (object?)request.LastName ?? DBNull.Value },
                    new SqlParameter("@Email", SqlDbType.VarChar, 150) { Value = (object?)request.Email ?? DBNull.Value },
                    new SqlParameter("@Company", SqlDbType.VarChar, 100) { Value = (object?)request.Company ?? DBNull.Value },
                    new SqlParameter("@DepartmentID", SqlDbType.Int) { Value = request.DepartmentID },
                    new SqlParameter("@ExperienceInMonths", SqlDbType.Int) { Value = (object?)request.ExperienceInMonths ?? DBNull.Value },

                    // IsActive not present on EmployeeCompleteDetails; default to true (1)
                    new SqlParameter("@IsActive", SqlDbType.Bit) { Value = true },

                    new SqlParameter("@Basic", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = request.Salary?.Basic ?? (object)DBNull.Value },
                    new SqlParameter("@HRA", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = request.Salary?.HRA ?? (object)DBNull.Value },
                    new SqlParameter("@Misc", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)request.Salary?.Misc ?? DBNull.Value },

                    new SqlParameter("@Skills", SqlDbType.NVarChar, -1) { Value = (object?)skillsCsv ?? DBNull.Value },

                    new SqlParameter("@Age", SqlDbType.Int) { Value = (object?)request.Age ?? DBNull.Value },
                    new SqlParameter("@Gender", SqlDbType.NVarChar, 20) { Value = (object?)request.Gender ?? DBNull.Value },
                    new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = (object?)request.Address?.City ?? DBNull.Value },
                    new SqlParameter("@Country", SqlDbType.NVarChar, 100) { Value = (object?)request.Address?.Country ?? DBNull.Value },
                };

                var scalar = await _db.ExecuteScalarAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (scalar == null || scalar == DBNull.Value)
                {
                    return false;
                }

                var resultStr = scalar.ToString() ?? string.Empty;
                if (resultStr.Contains("updated successfully", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // If the proc returned an error object (ErrorNumber, ErrorMessage), throw with details
                throw new Exception($"Stored procedure returned unexpected result: {resultStr}");
            }
            catch (SqlException ex)
            {
                throw new Exception($"SQL error occurred while executing {spName}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error occurred while updating employee profile: {ex.Message}", ex);
            }
        }

        // Add this method inside the EmployeeRepository class
        public async Task<bool> DeleteEmployeeAsync(int employeeId, CancellationToken cancellationToken = default)
        {
            const string spName = "DeleteEmployee";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@EmployeeID", SqlDbType.Int) { Value = employeeId }
            };

            try
            {
                var scalar = await _db.ExecuteScalarAsync(
                    spName,
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (scalar == null || scalar == DBNull.Value)
                {
                    return false;
                }

                var resultStr = scalar.ToString() ?? string.Empty;
                if (resultStr.Contains("deleted successfully", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // If the proc returned an error (e.g. ErrorNumber / ErrorMessage), include details
                throw new Exception($"Stored procedure returned unexpected result: {resultStr}");
            }
            catch (SqlException ex)
            {
                throw new Exception($"SQL error occurred while executing {spName}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unexpected error occurred while deleting employee: {ex.Message}", ex);
            }
        }
    }
}