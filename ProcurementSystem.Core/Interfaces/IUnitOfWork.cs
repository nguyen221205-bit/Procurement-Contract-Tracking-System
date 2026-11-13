using ProcurementSystem.Core.DTOs;

namespace ProcurementSystem.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted);
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
        Task<ApiResponse<T>> ExecuteTransactionalAsync<T>(
            Func<Task<ApiResponse<T>>> operation,
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted);
    }
}
