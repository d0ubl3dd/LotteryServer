using Contracts.Faults;
using log4net;
using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace BusinessLogic
{
    public class GlobalErrorHandler : IErrorHandler
    {
        private static readonly ILog _logger = LogManager.GetLogger("WCFServiceLogger");

        public bool HandleError(Exception error)
        {
            if (!(error is FaultException<ServiceFault>))
            {
                _logger.Error("Error CRÍTICO no controlado en infraestructura WCF", error);
            }

            return true;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            if (error is FaultException<ServiceFault> serviceFaultException)
            {
                var messageFault = serviceFaultException.CreateMessageFault();
                fault = Message.CreateMessage(version, messageFault, serviceFaultException.Action);
            }
            else
            {
                var serviceFault = new ServiceFault
                {
                    Message = "Ocurrió un error inesperado en el servidor.",
                    ErrorCode = "SERVER_ERROR"
                };

                var faultException = new FaultException<ServiceFault>(
                    serviceFault,
                    new FaultReason("Error Interno del Servidor")
                );

                var messageFault = faultException.CreateMessageFault();
                fault = Message.CreateMessage(version, messageFault, faultException.Action);
            }
        }
    }

    public class GlobalErrorBehavior : IServiceBehavior
    {
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            var handler = new GlobalErrorHandler();

            foreach (ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers)
            {
                dispatcher.ErrorHandlers.Add(handler);
            }
        }

        public void AddBindingParameters(ServiceDescription sd, ServiceHostBase sh, Collection<ServiceEndpoint> ep, BindingParameterCollection bp)
        {
        }

        public void Validate(ServiceDescription sd, ServiceHostBase sh)
        {
        }
    }

    public class GlobalErrorBehaviorExtension : BehaviorExtensionElement
    {
        public override Type BehaviorType => typeof(GlobalErrorBehavior);

        protected override object CreateBehavior()
        {
            return new GlobalErrorBehavior();
        }
    }
}