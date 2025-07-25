using System;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

class NetworkTest
{
    static async Task Main()
    {
        string serverIp = "192.168.0.1";
        int serverPort = 502;

        Console.WriteLine("=== Diagnostic de connectivité réseau ===\n");

        // Test 1: Ping
        Console.WriteLine($"1. Test de ping vers {serverIp}:");
        await TestPingAsync(serverIp);

        // Test 2: Votre fonction
        Console.WriteLine($"\n2. Test de votre fonction IsServerReachableAsync:");
        bool isReachable = await IsServerReachableAsync(serverIp, serverPort);
        Console.WriteLine($"Résultat: {(isReachable ? "OK ACCESSIBLE" : "KO NON ACCESSIBLE")}");

        // Test 3: Test détaillé
        Console.WriteLine($"\n3. Test détaillé du port {serverPort}:");
        await TestTcpPortDetailedAsync(serverIp, serverPort);

        // Test 4: Test avec différents timeouts
        Console.WriteLine($"\n4. Test avec différents timeouts:");
        int[] timeouts = { 1000, 2000, 5000, 10000 };
        foreach (int timeout in timeouts)
        {
            bool result = await IsServerReachableAsync(serverIp, serverPort, timeout);
            Console.WriteLine($"Timeout {timeout}ms: {(result ? "OK" : "KO")}");
        }

        Console.WriteLine("\nAppuyez sur une touche pour fermer...");
        Console.ReadKey();
    }

    // Votre fonction testée
    public static async Task<bool> IsServerReachableAsync(string serverIp, int serverPort, int timeoutMs = 2000)
    {
        try
        {
            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync(serverIp, serverPort);
                var timeoutTask = Task.Delay(timeoutMs);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                return completedTask == connectTask && client.Connected;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Exception: {ex.Message}");
            return false;
        }
    }

    static async Task TestPingAsync(string ip)
    {
        try
        {
            Ping ping = new Ping();
            PingReply reply = await ping.SendPingAsync(ip, 3000);

            if (reply.Status == IPStatus.Success)
            {
                Console.WriteLine($"   ✅ Ping réussi - Temps: {reply.RoundtripTime}ms");
            }
            else
            {
                Console.WriteLine($"   ❌ Ping échoué - Statut: {reply.Status}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Erreur ping: {ex.Message}");
        }
    }

    static async Task TestTcpPortDetailedAsync(string ip, int port)
    {
        try
        {
            using (var tcpClient = new TcpClient())
            {
                tcpClient.ReceiveTimeout = 5000;
                tcpClient.SendTimeout = 5000;

                Console.WriteLine($"   Tentative de connexion à {ip}:{port}...");

                var connectTask = tcpClient.ConnectAsync(ip, port);
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask == connectTask)
                {
                    if (tcpClient.Connected)
                    {
                        Console.WriteLine($"   ✅ Port {port}: OUVERT et ACCESSIBLE");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Port {port}: Connexion échouée");
                    }
                }
                else
                {
                    Console.WriteLine($"   ❌ Port {port}: TIMEOUT (5 secondes)");
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
}
