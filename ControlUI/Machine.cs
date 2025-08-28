using System;
using System.Threading.Tasks;

namespace ModbusTCP_Simplified
{
    /// <summary>
    /// Classe ultra-simplifiée pour gérer la connexion à une machine via Modbus TCP
    /// </summary>
    public class Machine
    {
        public string Status { get; private set; }
        public bool IsTestMode { get; set; }
        public Modbus Modbus { get; set; }

        public Machine()
        {
            Modbus = new Modbus();
            Status = "Disconnected";
        }

        /// <summary>
        /// Démarre la connexion avec la machine
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            Console.WriteLine("----- Machine.ConnectAsync called -----");

            await Modbus.StartAsync();

            if (Modbus.IsConnected)
            {
                Status = "Connected";
                Console.WriteLine("----- Machine.ConnectAsync END call -----\n");
                return true;
            }

            Status = "Connection failed";
            Console.WriteLine("----- Machine.ConnectAsync END call -----\n");
            return false;
        }

        /// <summary>
        /// Arrête la connexion
        /// </summary>
        public void Disconnect()
        {
            Modbus.Stop();
            Status = "Disconnected";
            Console.WriteLine("Machine disconnected");
        }

        /// <summary>
        /// Vérifie l'état de la connexion
        /// </summary>
        public bool IsConnected()
        {
            return Modbus.IsConnected;
        }
    }
}
