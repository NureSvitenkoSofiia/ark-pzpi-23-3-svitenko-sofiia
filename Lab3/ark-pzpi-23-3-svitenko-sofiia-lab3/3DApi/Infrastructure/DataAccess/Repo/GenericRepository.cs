using System.Linq.Expressions;
using _3DApi.Models;
using Microsoft.EntityFrameworkCore;

namespace _3DApi.Infrastructure.DataAccess.Repo;

public class GenericRepository<T> : IGenericRepository<T> where T : Base
{
    public readonly MainDbContext _context;

    public GenericRepository(MainDbContext context)
    {
        _context = context;
    }

    protected DbSet<T> Table => _context.Set<T>();

    public virtual async Task<Result<IEnumerable<T>>> GetListByConditionAsync(
        Expression<Func<T, bool>>? condition = null,
        OrderByOptions<T>? orderBy = null,
        IEnumerable<Func<IQueryable<T>, IQueryable<T>>>? includes = null,
        bool? isNoTracking = null,
        bool? isSplitQuery = null
    )
    {
        try
        {
            var query = Table.AsQueryable();

            query = isNoTracking is not null && isNoTracking != false ? query.AsNoTracking() : query;
            query = isSplitQuery is not null && isSplitQuery != false ? query.AsSplitQuery() : query;

            if (condition is not null)
            {
                query = query.Where(condition);
            }

            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => include(current));
            }

            if (orderBy is not null)
            {
                query = orderBy.IsDescending
                    ? query.OrderByDescending(orderBy.Expression)
                    : query.OrderBy(orderBy.Expression);
            }

            var result = await query.ToListAsync();
            return Result<IEnumerable<T>>.Success(result);
        }
        catch (DbUpdateException ex)
        {
            return Result<IEnumerable<T>>.Failure(RepositoryErrorMapper<T>.Map(ex));
        }
    }

    public virtual async Task<Result<T>> GetSingleByConditionAsync(
        Expression<Func<T, bool>>? condition = null,
        IEnumerable<Func<IQueryable<T>, IQueryable<T>>>? includes = null,
        bool? isNoTracking = null,
        bool? isSplitQuery = null
    )
    {
        try
        {
            var query = Table.AsQueryable();
            
            query = isNoTracking is not null && isNoTracking != false ? query.AsNoTracking() : query;
            query = isSplitQuery is not null && isSplitQuery != false ? query.AsSplitQuery() : query;
            
            if (condition is not null)
            {
                query = query.Where(condition);
            }
            
            if (includes != null)
            {
                query = includes.Aggregate(query, (current, include) => include(current));
            }

            var result = await query.FirstOrDefaultAsync();

            return result == null ? Result<T>.Failure(RepositoryErrors<T>.NotFoundError) : Result<T>.Success(result);
        }
        catch (DbUpdateException ex)
        {
            return Result<T>.Failure(RepositoryErrorMapper<T>.Map(ex));
        }
    }

    public virtual async Task<Result<int>> AddAsync(T item)
    {
        try
        {
            Table.Add(item);
            await GenericSaveAsync();
            return Result<int>.Success(item.Id);
        }
        catch (DbUpdateException ex)
        {
            return Result<int>.Failure(RepositoryErrorMapper<T>.Map(ex));
        }
    }

    public virtual async Task<Result> UpdateAsync(T item)
    {
        try
        {
            Table.Update(item);
            await GenericSaveAsync();
            return Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(RepositoryErrorMapper<T>.Map(ex));
        }
    }

    public async Task<Result> DeleteAsync(Expression<Func<T, bool>> condition)
    {
        try
        {
            var deletedCount = await Table.Where(condition).ExecuteDeleteAsync();

            return deletedCount == 0 ? Result.Failure(RepositoryErrors<T>.NotFoundError) : Result.Success();
        }
        catch (DbUpdateException ex)
        {
            return Result.Failure(RepositoryErrorMapper<T>.Map(ex));
        }
    }
    
    public async Task GenericSaveAsync()
    {
        var changedEntries = _context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in changedEntries)
        {
            var entity = (Base)entry.Entity;
            var now = DateTime.UtcNow;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedOn = now;
            }

            entity.LastModifiedOn = now;
        }

        await _context.SaveChangesAsync();
    }
}