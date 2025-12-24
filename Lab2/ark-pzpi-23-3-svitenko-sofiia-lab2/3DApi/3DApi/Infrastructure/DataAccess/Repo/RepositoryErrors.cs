using _3DApi.Infrastructure.Errors;

namespace _3DApi.Infrastructure.DataAccess.Repo;

public static class RepositoryErrors<T>
{
    private static readonly string EntityName = typeof(T).Name;

    public static readonly Error NotFoundError =
        Error.NotFound("common.REPOSITORY_NOT_FOUND_ERROR", $"{EntityName} not found");

    public static readonly Error UpdateError =
        Error.Conflict("common.REPOSITORY_UPDATE_ERROR", $"{EntityName} couldn't be updated");

    public static readonly Error AddError =
        Error.Conflict("common.REPOSITORY_ADD_ERROR", $"{EntityName} couldn't be added");

    public static readonly Error DeleteError =
        Error.NotFound("common.REPOSITORY_DELETE_ERROR", $"{EntityName} couldn't be deleted");
}