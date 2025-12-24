using System.Linq.Expressions;

namespace _3DApi.Infrastructure.DataAccess.Repo;

public class OrderByOptions<T>
{
    public Expression<Func<T, object>> Expression { get; set; }
    
    public bool IsDescending { get; set; }
}