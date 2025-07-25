using System;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using EasyModbus;

/// <summary>
/// Script simple pour tester et maintenir la connexion Modbus
/// Basé sur l'analyse du code ModbusController et Acquirer
/// </summary>
public class ModbusConnectionTest
{
    private ModbusClient modbusClient;
    private System.Timers.Timer pollingTimer;
    private SemaphoreSlim connectionSemaphore = new SemaphoreSlim(1, 1);

    // Configuration par défaut basée sur ModbusController
    public string IPAddress { get; set; } = "192.168.0.1";
    public int Port { get; set; } = 502;
    public bool IsConnected { get; private set; } = false;
    public bool IsEnabled { get; set; } = false;

    // Intervalle de polling (comme dans ModbusController)
    private readonly int POLLING_INTERVAL_MS = 250;

    public ModbusConnectionTest()
    {
        Console.WriteLine("=== Test de connexion Modbus ===");
        Console.WriteLine($"IP: {IPAddress}, Port: {Port}");

        // Initialisation du client Modbus
        modbusClient = new ModbusClient(IPAddress, Port);
        modbusClient.ConnectedChanged += OnConnectionChanged;

        // Initialisation du timer de polling
        pollingTimer = new System.Timers.Timer();
        pollingTimer.Interval = POLLING_INTERVAL_MS;
        pollingTimer.Elapsed += OnPollingTimer_Elapsed;
        pollingTimer.AutoReset = true;
    }

    /// <summary>
    /// Événement déclenché lors du changement d'état de connexion
    /// </summary>
    private void OnConnectionChanged(object sender)
    {
        IsConnected = modbusClient.Connected;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] État connexion: {(IsConnected ? "CONNECTÉ" : "DÉCONNECTÉ")}");
    }

    /// <summary>
    /// Démarre la connexion et le polling (équivalent à StartAsync)
    /// </summary>
    public async Task StartAsync()
    {
        Console.WriteLine("Démarrage de la connexion...");
        IsEnabled = true;

        // Premier poll pour établir la connexion
        await PollAsync();

        if (IsConnected)
        {
            Console.WriteLine("Connexion établie avec succès !");
            Console.WriteLine("Démarrage du polling automatique...");
            pollingTimer.Start();
        }
        else
        {
            Console.WriteLine("Échec de la connexion initiale.");
            IsEnabled = false;
        }
    }

    /// <summary>
    /// Arrête la connexion et le polling
    /// </summary>
    public void Stop()
    {
        Console.WriteLine("Arrêt de la connexion...");
        IsEnabled = false;
        pollingTimer?.Stop();

        // Attendre que les opérations en cours se terminent
        connectionSemaphore.Wait(2000);

        try
        {
            modbusClient?.Disconnect();
            Console.WriteLine("Connexion fermée.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la fermeture: {ex.Message}");
        }
    }

    /// <summary>
    /// Établit la connexion (équivalent à ConnectAsync)
    /// </summary>
    public async Task ConnectAsync()
    {
        // Utilisation du sémaphore pour éviter les connexions concurrentes
        if (connectionSemaphore.Wait(0))
        {
            await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Tentative de connexion...");
                    modbusClient.Connect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Erreur connexion: {ex.Message}");
                }
                finally
                {
                    connectionSemaphore.Release();
                }
            });
        }
    }

    /// <summary>
    /// Poll les données Modbus (équivalent à PollAsync)
    /// </summary>
    public async Task PollAsync()
    {
        if (!IsEnabled) return;

        // Tenter de se connecter si pas encore connecté
        if (!IsConnected)
        {
            await ConnectAsync();
        }

        if (IsConnected)
        {
            try
            {
                // Lecture des registres (basée sur ModbusController - registres 0 à 79)
                int[] registers = modbusClient.ReadHoldingRegisters(0, 80);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Poll réussi - {registers.Length} registres lus");

                // Affichage de quelques registres importants pour vérification
                if (registers.Length >= 10)
                {
                    Console.WriteLine($"  Registres [0-4]: {registers[0]}, {registers[1]}, {registers[2]}, {registers[3]}, {registers[4]}");
                    Console.WriteLine($"  Registres [5-9]: {registers[5]}, {registers[6]}, {registers[7]}, {registers[8]}, {registers[9]}");

                    // Affichage en binaire pour les registres de statut (utile pour les bits)
                    Console.WriteLine($"  Registre 0 (binaire): {Convert.ToString(registers[0], 2).PadLeft(16, '0')}");
                    Console.WriteLine($"  Registre 1 (binaire): {Convert.ToString(registers[1], 2).PadLeft(16, '0')}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Erreur polling: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Lecture spécifique d'un ou plusieurs registres
    /// </summary>
    public async Task<int[]> ReadRegistersAsync(int startAddress, int numberOfRegisters)
    {
        if (!IsConnected)
        {
            Console.WriteLine("Pas de connexion - impossible de lire les registres");
            return null;
        }

        try
        {
            int[] registers = modbusClient.ReadHoldingRegisters(startAddress, numberOfRegisters);
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Lecture registres {startAddress}-{startAddress + numberOfRegisters - 1} réussie");

            // Affichage détaillé
            for (int i = 0; i < registers.Length; i++)
            {
                int registerAddress = startAddress + i;
                Console.WriteLine($"  Registre {registerAddress}: {registers[i]} (0x{registers[i]:X4}) (binaire: {Convert.ToString(registers[i], 2).PadLeft(16, '0')})");
            }

            return registers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Erreur lecture registres {startAddress}-{startAddress + numberOfRegisters - 1}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Lecture d'un seul registre
    /// </summary>
    public async Task<int?> ReadSingleRegisterAsync(int address)
    {
        var result = await ReadRegistersAsync(address, 1);
        return result?[0];
    }

    /// <summary>
    /// Timer de polling automatique
    /// </summary>
    private async void OnPollingTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        await PollAsync();
    }

    /// <summary>
    /// Test de connexion simple
    /// </summary>
    public static async Task Main(string[] args)
    {
        var test = new ModbusConnectionTest();

        Console.WriteLine("Appuyez sur une touche pour démarrer le test...");
        Console.ReadKey();

        try
        {
            await test.StartAsync();

            Console.WriteLine("\nTest en cours... Commandes disponibles:");
            Console.WriteLine("  'q' : Quitter");
            Console.WriteLine("  's' : Afficher le statut");
            Console.WriteLine("  'r' : Lire des registres spécifiques");
            Console.WriteLine("  'o' : Lire un seul registre");
            Console.WriteLine("  'p' : Forcer un poll manuel");

            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    break;
                }

                // Affichage du statut
                if (key.KeyChar == 's' || key.KeyChar == 'S')
                {
                    Console.WriteLine($"Statut: {(test.IsConnected ? "CONNECTÉ" : "DÉCONNECTÉ")} - Polling: {(test.IsEnabled ? "ACTIF" : "INACTIF")}");
                }

                // Lecture de registres spécifiques
                if (key.KeyChar == 'r' || key.KeyChar == 'R')
                {
                    Console.Write("Adresse de début (0-79): ");
                    if (int.TryParse(Console.ReadLine(), out int startAddr) && startAddr >= 0 && startAddr < 80)
                    {
                        Console.Write("Nombre de registres à lire (1-10): ");
                        if (int.TryParse(Console.ReadLine(), out int count) && count >= 1 && count <= 10 && (startAddr + count) <= 80)
                        {
                            await test.ReadRegistersAsync(startAddr, count);
                        }
                        else
                        {
                            Console.WriteLine("Nombre invalide (1-10 max)");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Adresse invalide (0-79)");
                    }
                }

                // Lecture d'un seul registre
                if (key.KeyChar == 'o' || key.KeyChar == 'O')
                {
                    Console.Write("Adresse du registre (0-79): ");
                    if (int.TryParse(Console.ReadLine(), out int addr) && addr >= 0 && addr < 80)
                    {
                        var value = await test.ReadSingleRegisterAsync(addr);
                        if (value.HasValue)
                        {
                            Console.WriteLine($"Registre {addr} = {value.Value}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Adresse invalide (0-79)");
                    }
                }

                // Poll manuel
                if (key.KeyChar == 'p' || key.KeyChar == 'P')
                {
                    Console.WriteLine("Poll manuel déclenché...");
                    await test.PollAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur: {ex.Message}");
        }
        finally
        {
            test.Stop();
            Console.WriteLine("Test terminé. Appuyez sur une touche pour fermer...");
            Console.ReadKey();
        }
    }
}
