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
                    Console.WriteLine("✓ Connected successfully!");
                    
                    // Test de maintien de connexion
                    Console.WriteLine("Read/write attempt...");
                    for (int i = 0; i < 1; i++)
                    {
                        await machine.Modbus.PollAsync();
                        await Task.Delay(100);
                    }
                    
                    machine.Disconnect();
                    Console.WriteLine("✓ Disconnected successfully!");
                }
                else
                {
                    Console.WriteLine("✗ Connection failed - testing simulation mode");
                    machine.IsTestMode = true;
                    
                    if (await machine.ConnectAsync())
                    {
                        Console.WriteLine("✓ Test mode successful!");
                        machine.Disconnect();
                    }
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
