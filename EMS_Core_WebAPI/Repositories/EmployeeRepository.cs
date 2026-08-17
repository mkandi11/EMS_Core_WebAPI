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
        public Task<List<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT Id, FirstName, LastName, Email, Position FROM Employees";
            return _db.ExecuteReaderAsync(sql,
                reader => new Employee
                {
                    EmployeeID = reader.GetInt32(0),
                    FirstName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    LastName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                },
                parameters: null,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken);
        }

        public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = "SELECT Id, FirstName, LastName, Email, Position FROM Employees WHERE Id = @Id";
            var param = new SqlParameter("@Id", SqlDbType.Int) { Value = id };
            var list = await _db.ExecuteReaderAsync(sql,
                reader => new Employee
                {
                    EmployeeID = reader.GetInt32(0),
                    FirstName = reader.IsDBNull(1) ? null : reader.GetString(1),
                    LastName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                },
                parameters: new[] { param },
                commandType: CommandType.Text,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return list.FirstOrDefault();
        }

        public async Task<int> AddAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            // Example using inline SQL; switch to CommandType.StoredProcedure and pass SP name + params if you prefer SP.
            const string sql = @"
INSERT INTO Employees (FirstName, LastName, Email, Position)
OUTPUT INSERTED.Id
VALUES (@FirstName, @LastName, @Email, @Position);";

            var parameters = new[]
            {
                new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = (object?)employee.FirstName ?? DBNull.Value },
                new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = (object?)employee.LastName ?? DBNull.Value },
                new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = (object?)employee.Email ?? DBNull.Value },
            };

            var scalar = await _db.ExecuteScalarAsync(sql, parameters, commandType: CommandType.Text, cancellationToken: cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(scalar);
        }

        public async Task<bool> UpdateAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            const string sql = @"
UPDATE Employees
SET FirstName = @FirstName,
    LastName = @LastName,
    Email = @Email,
    Position = @Position
WHERE Id = @Id;";

            var parameters = new[]
            {
                new SqlParameter("@Id", SqlDbType.Int) { Value = employee.EmployeeID },
                new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = (object?)employee.FirstName ?? DBNull.Value },
                new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = (object?)employee.LastName ?? DBNull.Value },
                new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = (object?)employee.Email ?? DBNull.Value },
            };

            var rows = await _db.ExecuteNonQueryAsync(sql, parameters, commandType: CommandType.Text, cancellationToken: cancellationToken).ConfigureAwait(false);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            const string sql = "DELETE FROM Employees WHERE Id = @Id";
            var parameter = new SqlParameter("@Id", SqlDbType.Int) { Value = id };
            var rows = await _db.ExecuteNonQueryAsync(sql, new[] { parameter }, commandType: CommandType.Text, cancellationToken: cancellationToken).ConfigureAwait(false);
            return rows > 0;
        }

        // New method: calls stored procedure AddFullEmployeeProfile and returns new EmployeeID
        public async Task<int> AddEmployeeAsync(AddEmployeeRequest request, CancellationToken cancellationToken = default)
        {
            const string spName = "AddFullEmployeeProfile";

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FirstName", SqlDbType.NVarChar, 100) { Value = (object?)request.FirstName ?? DBNull.Value },
                new SqlParameter("@LastName", SqlDbType.NVarChar, 100) { Value = (object?)request.LastName ?? DBNull.Value },
                new SqlParameter("@Email", SqlDbType.NVarChar, 150) { Value = (object?)request.Email ?? DBNull.Value },
                new SqlParameter("@Company", SqlDbType.NVarChar, 100) { Value = (object?)request.Company ?? DBNull.Value },
                new SqlParameter("@DepartmentID", SqlDbType.Int) { Value = request.DepartmentID },
                new SqlParameter("@ExperienceInMonths", SqlDbType.Int) { Value = (object?)request.ExperienceInMonths ?? DBNull.Value },

                new SqlParameter("@Basic", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = request.Salary?.Basic },
                new SqlParameter("@HRA", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = request.Salary?.HRA },
                new SqlParameter("@Misc", SqlDbType.Decimal) { Precision = 10, Scale = 2, Value = (object?)request.Salary?.Misc ?? DBNull.Value },

                new SqlParameter("@Skills", SqlDbType.NVarChar, -1) { Value = (object?)request.Skills ?? DBNull.Value },

                new SqlParameter("@Age", SqlDbType.Int) { Value = (object?)request.Age ?? DBNull.Value },
                new SqlParameter("@Gender", SqlDbType.NVarChar, 20) { Value = (object?)request.Gender ?? DBNull.Value },
                new SqlParameter("@City", SqlDbType.NVarChar, 100) { Value = (object?)request.Address?.City ?? DBNull.Value },
                new SqlParameter("@Country", SqlDbType.NVarChar, 100) { Value = (object?)request.Address?.Country ?? DBNull.Value },
            };

            var scalar = await _db.ExecuteScalarAsync(spName, parameters, commandType: CommandType.StoredProcedure, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (scalar == null || scalar == DBNull.Value)
            {
                throw new InvalidOperationException("Stored procedure did not return the new EmployeeID.");
            }

            return Convert.ToInt32(scalar);
        }
    }
}