using EasyModbus;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

public class ModbusController
{
    private ModbusClient modbusClient;

    public bool IsConnected => modbusClient?.Connected ?? false;

    public ModbusController(string ip, int port)
    {
        modbusClient = new ModbusClient(ip, port);
        ServerIp = ip;
        ServerPort = port;
    }

    public string ServerIp { get; }
    public int ServerPort { get; }

    public async Task<bool> IsServerReachableAsync(int timeoutMs = 2000)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync(ServerIp, ServerPort);
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                return completedTask == connectTask && client.Connected;
            }
        }
        catch
        {
            return false;
        }
    }
    public async Task TestTcpPortDetailedAsync(string ip, int port)
    {
        try
        {
            using (var tcpClient = new TcpClient())
            {
                tcpClient.ReceiveTimeout = 5000;
                tcpClient.SendTimeout = 5000;

                Console.WriteLine($"\nTentative de connexion à {ip}:{port}...");

                var connectTask = tcpClient.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask)
                {
                    if (tcpClient.Connected)
                    {
                        Console.WriteLine($"   [OK] Port {port}: OUVERT et ACCESSIBLE");
                    }
                    else
                    {
                        Console.WriteLine($"   [KO] Port {port}: Connexion échouée");
                    }
                }
                else
                {
                    Console.WriteLine($"   [KO] Port {port}: TIMEOUT (5 secondes)");
                }
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"   ❌ Port {port}: SocketException - {ex.SocketErrorCode}");
            if (ex.SocketErrorCode == SocketError.TimedOut)
                Console.WriteLine("      → Le serveur ne répond pas (timeout)");
            else if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                Console.WriteLine("      → Connexion refusée (pas de serveur sur ce port)");
            else if (ex.SocketErrorCode == SocketError.HostUnreachable)
                Console.WriteLine("      → Hôte inaccessible (problème réseau)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Port {port}: Exception - {ex.Message}");
        }
    }

    public async Task ConnectAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                modbusClient.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Échec: {ex.Message}");
                throw;
            }
        });
     }

    public void Disconnect()
    {
        modbusClient.Disconnect();
    }

    public void WriteRegister(int address, ushort value)
    {
        modbusClient.WriteSingleRegister(address, value);
    }

    public int[] ReadRegisters(int startAddress, int count)
    {
        return modbusClient.ReadHoldingRegisters(startAddress, count);
    }

    public ushort ReadSingleRegister(int address)
    {
        var result = modbusClient.ReadHoldingRegisters(address, 1);
        return (ushort)result[0];
    }
}