using BusinessLogic;
using log4net;
using System;
using System.IO;
using System.ServiceModel;

namespace Host
{
    static class Program
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            string logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            log4net.Config.XmlConfigurator.Configure();
            _logger.Info("=== INICIANDO SERVIDOR DE LOTERÍA ===");

            try
            {
                using (ServiceHost host = new ServiceHost(typeof(LotteryService)))
                {
                    host.Open();

                    Console.WriteLine("Servicio de Lotería iniciado. Presiona Enter para salir.");
                    if (host.Description.Endpoints.Count > 0)
                    {
                        Console.WriteLine($"Escuchando en: {host.Description.Endpoints[0].Address}");
                        _logger.InfoFormat("Servidor escuchando en: {0}", host.Description.Endpoints[0].Address);
                    }

                    Console.ReadLine();
                    host.Close();
                }
            }
            catch (Exception exception)
            {
                _logger.Fatal("El servidor no pudo iniciar.", exception);
                Console.WriteLine($"Error fatal: {exception.Message}");
                Console.ReadLine();
            }

            _logger.Info("=== SERVIDOR DETENIDO ===");
        }
    }
}