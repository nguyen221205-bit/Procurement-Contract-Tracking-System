using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ProcurementSystem.Core.DTOs;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;

namespace ProcurementSystem.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly ConcurrentDictionary<string, object> _repositories = new();
        private IDbContextTransaction? _transaction;

        public UnitOfWork(AppDbContext context)
        {
            _context = context;
        }

        public IGenericRepository<T> Repository<T>() where T : class
        {
            var type = typeof(T).Name;
            var instance = _repositories.GetOrAdd(type, _ => new GenericRepository<T>(_context));
            return (IGenericRepository<T>)instance;
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
        {
            _transaction = await _context.Database.BeginTransactionAsync(isolationLevel);
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<ApiResponse<T>> ExecuteTransactionalAsync<T>(
            Func<Task<ApiResponse<T>>> operation,
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
        {
            await BeginTransactionAsync(isolationLevel);
            try
            {
                var response = await operation();
                if (response.Success)
                {
                    await CommitTransactionAsync();
                }
                else
                {
                    await RollbackTransactionAsync();
                }

                return response;
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
