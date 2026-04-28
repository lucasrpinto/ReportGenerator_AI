using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using Relatorios.Application.Abstractions.Persistence;
using Relatorios.Domain.Entities;
using Relatorios.Infrastructure.Options;

namespace Relatorios.Infrastructure.Repositories;

public sealed class PostgresUserRepository : IUserRepository
{
    private readonly ReportHistoryDatabaseOptions _options;

    public PostgresUserRepository(IOptions<ReportHistoryDatabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<User?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
    SELECT
        id,
        full_name,
        email,
        password_hash,
        role,
        is_active,
        created_at,
        updated_at,
        last_login_at
    FROM app_users
    WHERE id = @id
    LIMIT 1;
    """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid)
        {
            Value = id
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new User
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            Role = reader.GetString(reader.GetOrdinal("role")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("updated_at")),
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("last_login_at"))
        };
    }

    public async Task UpdatePasswordAsync(
        Guid userId,
        string passwordHash,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
    UPDATE app_users
    SET 
        password_hash = @password_hash,
        updated_at = @updated_at
    WHERE id = @id;
    """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid)
        {
            Value = userId
        });

        command.Parameters.Add(new NpgsqlParameter("@password_hash", NpgsqlDbType.Text)
        {
            Value = passwordHash
        });

        command.Parameters.Add(new NpgsqlParameter("@updated_at", NpgsqlDbType.Timestamp)
        {
            Value = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        SELECT
            id,
            full_name,
            email,
            password_hash,
            role,
            is_active,
            created_at,
            updated_at,
            last_login_at
        FROM app_users
        WHERE lower(email) = lower(@email)
        LIMIT 1;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@email", NpgsqlDbType.Varchar)
        {
            Value = email
        });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new User
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
            Role = reader.GetString(reader.GetOrdinal("role")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("updated_at")),
            LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("last_login_at"))
        };
    }

    public async Task<bool> ExistsByEmailAsync(
        string email,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        SELECT EXISTS (
            SELECT 1
            FROM app_users
            WHERE lower(email) = lower(@email)
        );
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@email", NpgsqlDbType.Varchar)
        {
            Value = email
        });

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is bool exists && exists;
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        INSERT INTO app_users
        (
            id,
            full_name,
            email,
            password_hash,
            role,
            is_active,
            created_at
        )
        VALUES
        (
            @id,
            @full_name,
            @email,
            @password_hash,
            @role,
            @is_active,
            @created_at
        );
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid) { Value = user.Id });
        command.Parameters.Add(new NpgsqlParameter("@full_name", NpgsqlDbType.Varchar) { Value = user.FullName });
        command.Parameters.Add(new NpgsqlParameter("@email", NpgsqlDbType.Varchar) { Value = user.Email });
        command.Parameters.Add(new NpgsqlParameter("@password_hash", NpgsqlDbType.Text) { Value = user.PasswordHash });
        command.Parameters.Add(new NpgsqlParameter("@role", NpgsqlDbType.Varchar) { Value = user.Role });
        command.Parameters.Add(new NpgsqlParameter("@is_active", NpgsqlDbType.Boolean) { Value = user.IsActive });
        command.Parameters.Add(new NpgsqlParameter("@created_at", NpgsqlDbType.Timestamp)
        {
            Value = DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Unspecified)
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateLastLoginAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string sql = """
        UPDATE app_users
        SET last_login_at = @last_login_at
        WHERE id = @id;
        """;

        await using var command = new NpgsqlCommand(sql, connection);

        command.Parameters.Add(new NpgsqlParameter("@id", NpgsqlDbType.Uuid) { Value = userId });
        command.Parameters.Add(new NpgsqlParameter("@last_login_at", NpgsqlDbType.Timestamp)
        {
            Value = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified)
        });

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}