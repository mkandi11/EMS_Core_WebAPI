using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.Extensions.Configuration;

namespace EMS_Core_WebAPI.Helper
{
    public class DBConnection
    {
        private readonly string _connectionString;

        public DBConnection(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("DefaultConnection not found in configuration.");
        }

        /// <summary>
        /// Opens and returns an open SqlConnection. Caller should dispose the returned connection.
        /// </summary>
        public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken = default)
        {
            var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
            return conn;
        }

        /// <summary>
        /// Ensures connection is closed and disposed.
        /// </summary>
        public static void Close(SqlConnection? connection)
        {
            if (connection == null) return;
            try
            {
                if (connection.State != ConnectionState.Closed)
                {
                    connection.Close();
                }
            }
            finally
            {
                connection.Dispose();
            }
        }

        /// <summary>
        /// Executes a command that returns a sequence of items and maps each row into a dictionary
        /// where the key is the column name and the value is the column value (or null).
        /// This is useful when the returned columns are dynamic or unknown in advance (e.g., stored procedures).
        /// Default CommandType is StoredProcedure for convenience when calling SPs frequently.
        /// </summary>
        public async Task<List<Dictionary<string, object?>>> ExecuteReaderAsync(
            string commandText,
            IEnumerable<SqlParameter>? parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default)
        {
            var results = new List<Dictionary<string, object?>>();

            await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new SqlCommand(commandText, conn) { CommandType = commandType };

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var name = reader.GetName(i);
                    var value = await reader.IsDBNullAsync(i, cancellationToken).ConfigureAwait(false)
                        ? null
                        : reader.GetValue(i);
                    row[name] = value;
                }

                results.Add(row);
            }

            return results;
        }

        /// <summary>
        /// Executes a command that returns a sequence of items. The mapper transforms each SqlDataReader row into T.
        /// Kept for cases where you want a typed projection.
        /// </summary>
        public async Task<List<T>> ExecuteReaderAsync<T>(
            string commandText,
            Func<SqlDataReader, T> map,
            IEnumerable<SqlParameter>? parameters = null,
            CommandType commandType = CommandType.Text,
            CancellationToken cancellationToken = default)
        {
            var results = new List<T>();

            await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new SqlCommand(commandText, conn) { CommandType = commandType };

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                results.Add(map(reader));
            }

            return results;
        }

        /// <summary>
        /// Executes a scalar command and returns the result (e.g., inserted id).
        /// </summary>
        public async Task<object?> ExecuteScalarAsync(
            string commandText,
            IEnumerable<SqlParameter>? parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new SqlCommand(commandText, conn) { CommandType = commandType };

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }

            return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a non-query command (INSERT/UPDATE/DELETE or stored procedure) and returns affected rows.
        /// </summary>
        public async Task<int> ExecuteNonQueryAsync(
            string commandText,
            IEnumerable<SqlParameter>? parameters = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default)
        {
            await using var conn = await OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var cmd = new SqlCommand(commandText, conn) { CommandType = commandType };

            if (parameters != null)
            {
                cmd.Parameters.AddRange(parameters.ToArray());
            }

            return await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}