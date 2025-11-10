using Microsoft.EntityFrameworkCore.Storage;
using TaxFlow.Core.Entities;
using TaxFlow.Core.Interfaces;
using TaxFlow.Infrastructure.Data;

namespace TaxFlow.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly TaxFlowDbContext _context;
    private IDbContextTransaction? _transaction;

    private IInvoiceRepository? _invoices;
    private IReceiptRepository? _receipts;
    private IRepository<Customer>? _customers;
    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private IAuditLogRepository? _auditLogs;

    public UnitOfWork(TaxFlowDbContext context)
    {
        _context = context;
    }

    public IInvoiceRepository Invoices
    {
        get
        {
            _invoices ??= new InvoiceRepository(_context);
            return _invoices;
        }
    }

    public IReceiptRepository Receipts
    {
        get
        {
            _receipts ??= new ReceiptRepository(_context);
            return _receipts;
        }
    }

    public IRepository<Customer> Customers
    {
        get
        {
            _customers ??= new Repository<Customer>(_context);
            return _customers;
        }
    }

    public IUserRepository Users
    {
        get
        {
            _users ??= new UserRepository(_context);
            return _users;
        }
    }

    public IRoleRepository Roles
    {
        get
        {
            _roles ??= new RoleRepository(_context);
            return _roles;
        }
    }

    public IPermissionRepository Permissions
    {
        get
        {
            _permissions ??= new PermissionRepository(_context);
            return _permissions;
        }
    }

    public IAuditLogRepository AuditLogs
    {
        get
        {
            _auditLogs ??= new AuditLogRepository(_context);
            return _auditLogs;
        }
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
