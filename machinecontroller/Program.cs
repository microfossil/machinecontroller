using System;
using System.Threading.Tasks;

namespace ModbusTCP_Simplified
{
    /// <summary>
    /// Programme principal ultra-simplifié pour tester la connexion Modbus TCP
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Simple Modbus TCP Connection Test ===\n");

            var machine = new Machine();

            try
            {
                // Test de connexion
                Console.WriteLine("Testing connection...");
                bool connected = await machine.ConnectAsync();

                if (connected)
                {

                    // Test de maintien de connexion
                    Console.WriteLine("Maintaining connection for 5 seconds...");
                    for (int i = 0; i < 1; i++)
                    {
                        await machine.Modbus.PollAsync();
                        await Task.Delay(10000);
                    }

                    machine.Disconnect();
                }
                else
                {
                    Console.WriteLine("[KO] Connection failed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
