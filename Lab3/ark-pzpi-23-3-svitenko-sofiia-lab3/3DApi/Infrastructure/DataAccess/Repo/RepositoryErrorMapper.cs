using _3DApi.Infrastructure.Errors;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace _3DApi.Infrastructure.DataAccess.Repo;

public static class RepositoryErrorMapper<T>
{
    public static Error Map(DbUpdateException ex)
    {
        if (ex is DbUpdateConcurrencyException)
        {
            return RepositoryErrors<T>.UpdateError;
        }
        
        if (ex.InnerException is PostgresException pgEx)
        {
            switch (pgEx.SqlState)
            {
                case "23505": 
                    return RepositoryErrors<T>.AddError;
                case "23503": 
                    if (pgEx.Message.Contains("DELETE", StringComparison.OrdinalIgnoreCase))
                    {
                        return RepositoryErrors<T>.DeleteError;
                    }
                    return RepositoryErrors<T>.UpdateError;
                default:
                    return RepositoryErrors<T>.UpdateError;
            }
        }

        return RepositoryErrors<T>.UpdateError;
    }
}