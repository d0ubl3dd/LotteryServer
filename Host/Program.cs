using System;
using System.ServiceModel;
using BusinessLogic;

namespace Host
{
    class Program
    {
        static void Main(string[] args)
        {
            using (ServiceHost host = new ServiceHost(typeof(GameService)))
            {
                host.Open();

                Console.WriteLine("Servicio de Lotería iniciado. Presiona Enter para salir.");
                Console.WriteLine($"Escuchando en: {host.Description.Endpoints[0].Address}");

                Console.ReadLine();

                host.Close();
            }
        }
    }
}