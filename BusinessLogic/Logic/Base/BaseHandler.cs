using System;
using System.ServiceModel;
using System.Threading.Tasks;
using log4net;
using Contracts.Faults;

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
            catch (Exception exception)
            {
                HandleException(exception, operationName);
                return default;
            }
        }

        protected async Task ExecuteFaultSafeAsync(Func<Task> action, string operationName)
        {
            try
            {
                await action();
            }
            catch (Exception exception)
            {
                HandleException(exception, operationName);
            }
        }

        protected void ExecuteFaultSafe(Action action, string operationName)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                HandleException(exception, operationName);
            }
        }

        protected T ExecuteFaultSafe<T>(Func<T> action, string operationName)
        {
            try
            {
                return action();
            }
            catch (Exception exception)
            {
                HandleException(exception, operationName);
                return default;
            }
        }

        private void HandleException(Exception exception, string operationName)
        {
            if (exception is FaultException<ServiceFault>)
            {
                throw exception;
            }

            var result = BusinessLogic.Utilities.ExceptionMapper.GetFaultAndLogAction(exception);

            var fault = result.Fault;
            var logAction = result.Logger;

            logAction(string.Format("[{0}] {1}: {2} | DebugDetail: {3}",
                operationName, fault.ErrorCode, fault.Message, exception.Message));

            throw new FaultException<ServiceFault>(fault, new FaultReason(fault.Message));
        }
    }
}