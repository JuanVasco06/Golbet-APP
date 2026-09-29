// GolBet.Repositories/Implementations/GenericRepository.cs
using GolBet.Entities.Common;
using GolBet.Repositories.Data;
using GolBet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using GolBet.Repositories.Exceptions;

namespace GolBet.Repositories.Implementations;

public class GenericRepository<T> : IGenericRepository<T> where T : AuditableEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();   // resolves the DbSet for T at runtime
    }

    // ---- Queries ----

    public virtual async Task<IEnumerable<T>> GetAllAsync(bool includeInactive = false)
    {
        IQueryable<T> query = _dbSet.AsNoTracking();

        if (!includeInactive)
            query = query.Where(e => e.IsActive);

        return await query.ToListAsync();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
        => await _dbSet.FindAsync(id);

    // ---- Commands ----

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await SaveAsync();
        return entity;
    }

    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await SaveAsync();
    }

    public async Task DeactivateAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity is null || !entity.IsActive) throw new KeyNotFoundException();

        entity.IsActive = false;             // logical delete
        await _context.SaveChangesAsync();   // tracked as Modified -> gets ModifiedDate
    }

    private async Task SaveAsync()
    {
        try { await _context.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            throw new DuplicateRecordException(ex);
        }
    }
}
