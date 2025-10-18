using System;
using System.ServiceModel;
using BusinessLogic;

namespace Host
{
    class Program
    {
        static void Main(string[] args)
        {
            string passwordDePrueba = "password123";
            BusinessLogic.Logic.PasswordHasher.CreatePasswordHash(passwordDePrueba, out byte[] passwordHash, out byte[] passwordSalt);

            Console.WriteLine("--- DATOS PARA EL USUARIO DE PRUEBA ---");
            Console.WriteLine();
            Console.WriteLine($"Contraseña: {passwordDePrueba}");
            Console.WriteLine();
            Console.WriteLine("Copia y pega estos valores en tu script de SQL Server:");
            Console.WriteLine();
            Console.WriteLine("Password Salt (hex):");
            Console.WriteLine("0x" + BitConverter.ToString(passwordSalt).Replace("-", ""));
            Console.WriteLine();
            Console.WriteLine("Password Hash (hex):");
            Console.WriteLine("0x" + BitConverter.ToString(passwordHash).Replace("-", ""));
            Console.WriteLine();
            Console.WriteLine("------------------------------------------");

            using (ServiceHost host = new ServiceHost(typeof(LotteryService)))
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