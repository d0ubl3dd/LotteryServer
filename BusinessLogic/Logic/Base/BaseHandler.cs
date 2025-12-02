using System;
using System.ServiceModel;
using System.Threading.Tasks;
using log4net;
using Contracts.Faults;
using BusinessLogic.Utilities;

namespace BusinessLogic.Logic.Base
{
    public abstract class BaseHandler
    {
        protected readonly ILog _logger;

        protected BaseHandler(Type loggerType)
        {
            _logger = LogManager.GetLogger(loggerType);
        }

        protected async Task<T> ExecuteFaultSafeAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default;
            }
        }

        protected async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        protected void ExecuteFaultSafe(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
            }
        }

        protected T ExecuteFaultSafe<T>(Func<T> action, string operationName)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                HandleException(ex, operationName);
                return default;
            }
        }

        private void HandleException(Exception ex, string operationName)
        {
            if (ex is FaultException<ServiceFault>)
                throw ex;

            var result = Utilities.ExceptionMapper.GetFaultAndLogAction(ex);
            var fault = result.Fault;
            var logAction = result.Logger;

            logAction($"[{operationName}] {fault.ErrorCode}: {fault.Message} | Detalle: {ex.Message}");

            throw new FaultException<ServiceFault>(fault, new FaultReason(fault.Message));
        }
    }
}