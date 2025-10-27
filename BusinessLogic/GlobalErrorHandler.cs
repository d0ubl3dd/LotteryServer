using Contracts.Faults;
using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace BusinessLogic
{
    public class GlobalErrorHandler : IErrorHandler
    {
        public bool HandleError(Exception error)
        {
            Console.WriteLine($"[ERROR DE SERVICIO] Operación fallida: {error.Message}");

            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            var serviceFault = new ServiceFault
            {
                Message = error.Message,
                ErrorCode = "OPER_FALLIDA"
            };

            var faultException = new FaultException<ServiceFault>(serviceFault, new FaultReason(error.Message));

            MessageFault messageFault = faultException.CreateMessageFault();
            fault = Message.CreateMessage(version, messageFault, faultException.Action);
        }
    }

    public class GlobalErrorHandlerBehavior : IServiceBehavior
    {
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcherBase channelDispatcherBase in serviceHostBase.ChannelDispatchers)
            {
                var channelDispatcher = (ChannelDispatcher)channelDispatcherBase;
                channelDispatcher.ErrorHandlers.Add(new GlobalErrorHandler());
            }
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
    }
}