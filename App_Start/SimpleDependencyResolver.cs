using System;
using System.Collections.Generic;
using System.Web.Http.Dependencies;

// Existing namespaces
using SmkcApi.Controllers;
using SmkcApi.Repositories;
using SmkcApi.Services;

// Add these usings if your Oracle classes live in Repositories namespace
// using SmkcApi.Repositories.Oracle; // (if you put Oracle* classes in a sub-namespace)

namespace SmkcApi
{
    /// <summary>
    /// Simple dependency resolver for demonstration purposes.
    /// In production, use a proper DI container like Unity, Autofac, or Ninject.
    /// </summary>
    public class SimpleDependencyResolver : IDependencyResolver
    {
        private readonly Dictionary<Type, Func<object>> _services = new Dictionary<Type, Func<object>>();

        public SimpleDependencyResolver()
        {
            RegisterDependencies();
        }

        /// <summary>
        /// Central place to register all dependencies.
        /// </summary>
        private void RegisterDependencies()
        {
            //
            // ORACLE: core plumbing
            //
            // NOTE: Ensure you added Oracle.ManagedDataAccess NuGet and created:
            //   - IOracleConnectionFactory / OracleConnectionFactory
            //   - IOracleDiagnosticsRepository / OracleDiagnosticsRepository
            // Connection string name "OracleDb" must exist in Web.config <connectionStrings>.
            _services[typeof(IOracleConnectionFactory)] = () => new OracleConnectionFactory("OracleDb");
            _services[typeof(SmkcApi.Repositories.IOracleConnectionFactory)] = () => new SmkcApi.Repositories.OracleConnectionFactory("OracleDb");
            _services[typeof(SmkcApi.Repositories.IWaterRepository)] = () =>
                new SmkcApi.Repositories.WaterRepository(
                    GetService(typeof(SmkcApi.Repositories.IOracleConnectionFactory)) as SmkcApi.Repositories.IOracleConnectionFactory
                );
            _services[typeof(SmkcApi.Services.IWaterService)] = () =>
                new SmkcApi.Services.WaterService(
                    GetService(typeof(SmkcApi.Repositories.IWaterRepository)) as SmkcApi.Repositories.IWaterRepository
                );

            // Controller
            _services[typeof(SmkcApi.Controllers.WaterController)] = () =>
                new SmkcApi.Controllers.WaterController(
                    GetService(typeof(SmkcApi.Services.IWaterService)) as SmkcApi.Services.IWaterService
                );
            _services[typeof(IAccountRepository)] = () => new AccountRepository();
            _services[typeof(ICustomerRepository)] = () => new CustomerRepository();
            _services[typeof(ITransactionRepository)] = () => new TransactionRepository();

            //
            // Services
            //
            _services[typeof(IAccountService)] = () => new AccountService(
                GetService(typeof(IAccountRepository)) as IAccountRepository,
                GetService(typeof(ICustomerRepository)) as ICustomerRepository
            );

            _services[typeof(ICustomerService)] = () => new CustomerService(
                GetService(typeof(ICustomerRepository)) as ICustomerRepository
            );

            _services[typeof(ITransactionService)] = () => new TransactionService(
                GetService(typeof(ITransactionRepository)) as ITransactionRepository,
                GetService(typeof(IAccountRepository)) as IAccountRepository
            );

            //
            // Controllers
            //
            _services[typeof(AccountController)] = () => new AccountController(
                GetService(typeof(IAccountService)) as IAccountService
            );

            _services[typeof(CustomerController)] = () => new CustomerController(
                GetService(typeof(ICustomerService)) as ICustomerService
            );

            _services[typeof(TransactionController)] = () => new TransactionController(
                GetService(typeof(ITransactionService)) as ITransactionService
            );

            // (Optional) Diagnostics controller if you add one:
            // _services[typeof(DiagnosticsController)] = () => new DiagnosticsController(
            //     GetService(typeof(IOracleDiagnosticsRepository)) as IOracleDiagnosticsRepository
            // );
        }

        public IDependencyScope BeginScope()
        {
            return new SimpleDependencyScope(this);
        }

        public object GetService(Type serviceType)
        {
            if (_services.ContainsKey(serviceType))
            {
                return _services[serviceType]();
            }
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            var service = GetService(serviceType);
            return service != null ? new[] { service } : new object[0];
        }

        public void Dispose()
        {
            _services.Clear();
        }
    }

    /// <summary>
    /// Simple dependency scope implementation
    /// </summary>
    public class SimpleDependencyScope : IDependencyScope
    {
        private readonly SimpleDependencyResolver _resolver;

        public SimpleDependencyScope(SimpleDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public object GetService(Type serviceType)
        {
            return _resolver.GetService(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return _resolver.GetServices(serviceType);
        }

        public void Dispose()
        {
            // Nothing to dispose in this simple implementation
        }
    }
}
