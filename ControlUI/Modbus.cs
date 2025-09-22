using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Reflection;
using EasyModbus;

namespace ModbusTCP_Simplified
{
    /// <summary>
    /// Classe simplifiée pour la connexion Modbus TCP
    /// Avec polling régulier via DispatcherTimer
    /// </summary>
    public class Modbus
    {
        private DispatcherTimer timer;
        private ModbusClient modbusClient;
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public string IPAddress { get; set; }
        public int Port { get; set; }
        public bool IsConnected { get; private set; }
        public bool Enabled { get; private set; }

        public int GemmaMode { get; private set; }
        public int Word0 { get; private set; }
        public bool RequestAnalyseVisionA { get; private set; }
        public bool RequestAnalyseVisionB { get; private set; }
        public bool RequestControlVoidA { get; private set; }
        public bool RequestControlVoidB { get; private set; }
        public string TxtWord0 { get; private set; }
        public int Word90 { get; private set; }
        public string TxtWord90 { get; private set; }
        public int Word91 { get; private set; }
        public string TxtWord91 { get; private set; }
        public int Dest_P_A { get; private set; }
        public int Dest_X_A { get; private set; }
        public int Dest_Y_A { get; private set; }
        public int Dest_Z_A { get; private set; }
        public int Dest_P_B { get; private set; }
        public int Dest_X_B { get; private set; }
        public int Dest_Y_B { get; private set; }
        public int Dest_Z_B { get; private set; }
        public int StepCyclePrincipal { get; private set; }
        public int FioleNumber { get; private set; }
        public bool DoneFlag { get; set; }

        public Modbus()
        {
            IPAddress = "192.168.0.1";
            Port = 502;

            modbusClient = new ModbusClient(IPAddress, Port);
            modbusClient.UnitIdentifier = 1;
            modbusClient.ConnectedChanged += (sender) =>
            {
                IsConnected = modbusClient.Connected;
                Console.WriteLine($"[MODBUS] Connection status changed: {(IsConnected ? "CONNECTED" : "DISCONNECTED")}");
            };

            Enabled = false;

            timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2000) // Poll every 2000 ms
            };
            timer.Tick += Timer_Tick;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            await PollAsync();
        }

        /// <summary>
        /// Démarre la connexion Modbus
        /// </summary>
        public async Task StartAsync()
        {
            Console.WriteLine("----- Modbus.StartAsync called -----");
            Enabled = true;
            await ConnectAsync();
            await PollAsync(); // Premier poll immédiat

            if (IsConnected)
                timer.Start();
            Console.WriteLine("----- Modbus.StartAsync END call -----\n");
        }

        /// <summary>
        /// Arrête la connexion Modbus
        /// </summary>
        public void Stop()
        {
            Console.WriteLine("Stopping Modbus connection...");
            Enabled = false;
            timer.Stop();
            modbusClient.Disconnect();
            IsConnected = false;
        }

        /// <summary>
        /// Établit la connexion avec le serveur Modbus
        /// </summary>
        public async Task ConnectAsync()
        {
            Console.WriteLine("----- Modbus.ConnectAsync called -----");
            if (_semaphoreSlim.Wait(0))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        Console.WriteLine($"Connecting to {IPAddress}:{Port}");
                        modbusClient.Connect();
                        IsConnected = modbusClient.Connected;
                        Console.WriteLine("Connected successfully!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Connection error: {ex.Message}");
                        IsConnected = false;
                    }
                    _semaphoreSlim.Release();
                });
            }
            else
            {
                Console.WriteLine("Connection attempt skipped - another thread is connected");
            }
            Console.WriteLine("----- Modbus.ConnectAsync END call -----\n");
        }


        // Poll function
        //------------------------------------------------------------------------------------------
        public async Task PollAsync()
        {
            if (!Enabled || !IsConnected) return;

            try
            {
                Word0 = await ReadHoldingRegisterAsync(0);
                RequestAnalyseVisionA = GetBit(Word0, 0);
                RequestAnalyseVisionB = GetBit(Word0, 1);
                RequestControlVoidA = GetBit(Word0, 2);
                RequestControlVoidB = GetBit(Word0, 3);
                TxtWord0 = GetTxtWord(0);

                GemmaMode = await ReadHoldingRegisterAsync(1);

                Word90 = await ReadHoldingRegisterAsync(90);
                TxtWord90 = GetTxtWord(90);

                Word91 = await ReadHoldingRegisterAsync(91);

                Dest_P_A = await ReadHoldingRegisterAsync(96); //Destination_A_Plateau 
                Dest_X_A = await ReadHoldingRegisterAsync(97); // Destination_A_X
                Dest_Y_A = await ReadHoldingRegisterAsync(98); // Destination_A_Y
                Dest_Z_A = await ReadHoldingRegisterAsync(99); // Destination_A_Z
                Dest_P_B = await ReadHoldingRegisterAsync(100); // Destination_B_Plateau
                Dest_X_B = await ReadHoldingRegisterAsync(101); // Destination_B_X
                Dest_Y_B = await ReadHoldingRegisterAsync(102); // Destination_B_Y
                Dest_Z_B = await ReadHoldingRegisterAsync(103); // Destination_B_Z

                // TxtWord91 = GetTxtWord(91);

                StepCyclePrincipal = await ReadHoldingRegisterAsync(10);

                FioleNumber = await ReadHoldingRegisterAsync(105);

                Console.WriteLine($"[Poll] GEMMA={GemmaMode:X2}, Auto/Man={Word90}, Vial={FioleNumber}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lecture Modbus : {ex.Message}");
                IsConnected = false;
            }
        }

        private string GetTxtWord(int wordNumber)
        {
            StringBuilder sb = new StringBuilder();
            var getBitNameFunc = GetBitNameFuncByReflection(wordNumber);

            for (int i = 0; i < 8; i++)
            {
                bool bitValue = GetBit(Word90, i);
                string bitName = getBitNameFunc(i);
                sb.AppendLine($"{bitName} - {bitValue}");
            }

            return sb.ToString();
        }
        //------------------------------------------------------------------------------------------

        // Higher level functions
        //------------------------------------------------------------------------------------------
        // Read and display GEMMA mode with description
        public async Task<int> GetGEMMAMode()
        {
            if (!IsConnected)
            {
                Console.WriteLine("\nCannot read - Modbus not connected");
                return -1;
            }

            try
            {
                int gemmaMode = await ReadHoldingRegisterAsync(1);
                Console.WriteLine($"\nGEMMA Mode: {gemmaMode} (decimal) -> {gemmaMode:X2} (hexa) || {GetGEMMADescription(gemmaMode)}");
                return gemmaMode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError reading GEMMA mode: {ex.Message}");
                return -1;
            }
        }

        /// Set Auto/Manual mode
        public async Task SetAutoModeAsync(bool autoMode)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }

            try
            {
                int currentValue = await ReadHoldingRegisterAsync(90);
                int newValue = autoMode ? SetBit(currentValue, 1) : ClearBit(currentValue, 1);

                await WriteSingleRegisterAsync(90, newValue);

                bool currentBit = GetBit(currentValue, 1);
                bool newBit = GetBit(newValue, 1);

                Console.WriteLine($"\nWORD90.1 (Cde_Auto.Mode_Auto) changed from {currentBit} to {newBit} ({(autoMode ? "AUTO" : "MANUAL")})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError setting Auto mode: {ex.Message}");
            }
        }

        public async Task InitAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 4); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);

                Console.WriteLine($"\nWORD90.4 (Cde_Auto.Init) demande initialisation sent");

                // GEMMA mode is constantly tracked and once it is not anymore in A5, we reset the Init bit
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int gemmaMode = GemmaMode; // GemmaMode updated by PollAsync()

                        if (gemmaMode != 0xA5) // As soon as GEMMA leaves A5 (0xA5 (hex) = 165 (decimal) & In C#, $"{165:X2}" yields "A5"
                        {
                            Console.WriteLine($"Init done (GEMMA={gemmaMode:X2}), reset du bit Init");

                            // reset bit 4 to 0
                            int resetValue = ClearBit(newValue, 4);
                            await WriteSingleRegisterAsync(90, resetValue);
                            DoneFlag = true; // Set the flag to true when done
                            break;
                        }

                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during Init: {ex.Message}");
            }
        }

        public async Task StartCycleAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 2); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);

                Console.WriteLine($"\nWORD90.2 (Cde_Auto.Start) demande départ cycle sent");

                // GEMMA mode is constantly tracked and once it is not anymore in A1, we reset the Start bit
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int gemmaMode = GemmaMode; // GemmaMode updated by PollAsync()

                        if (gemmaMode != 0xA1) // As soon as GEMMA leaves A1
                        {
                            Console.WriteLine($"Cycle terminé (GEMMA={gemmaMode:X2}), reset du bit Start");

                            // reset bit 2 to 0
                            int resetValue = ClearBit(newValue, 2);
                            await WriteSingleRegisterAsync(90, resetValue);
                            DoneFlag = true; // Set the flag to true when done
                            break;
                        }

                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during StartCycle: {ex.Message}");
            }
        }

        public async Task StopCycleAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 3); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);

                Console.WriteLine($"\nWORD90.3 (Cde_Auto.Stop) demande arrêt cycle sent");

                // GEMMA mode is constantly tracked and once it is not anymore in F1, we reset the Stop bit
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int gemmaMode = GemmaMode; // GemmaMode updated by PollAsync()

                        if (gemmaMode != 0xF1) // As soon as GEMMA leaves F1
                        {
                            Console.WriteLine($"Cycle arrêté (GEMMA={gemmaMode:X2}), reset du bit Stop");

                            // reset bit 3 to 0
                            int resetValue = ClearBit(newValue, 3);
                            await WriteSingleRegisterAsync(90, resetValue);
                            DoneFlag = true; // Set the flag to true when done
                            break;
                        }

                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during StopCycle: {ex.Message}");
            }
        }

        public async Task AcquitDefaultAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 0); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);

                Console.WriteLine($"\nWORD90.0 (Cde_Auto.Acquit) demande acquittement défaut sent");

                // GEMMA mode is constantly tracked and once it is not anymore in D2, we reset the Acquit bit
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int gemmaMode = GemmaMode; // GemmaMode updated by PollAsync()

                        if (gemmaMode != 0xD1) // As soon as GEMMA leaves D1
                        {
                            Console.WriteLine($"Acquittement effectué (GEMMA={gemmaMode:X2}), reset du bit Acquit");

                            // reset bit 0 to 0
                            int resetValue = ClearBit(newValue, 0);
                            await WriteSingleRegisterAsync(90, resetValue);
                            DoneFlag = true; // Set the flag to true when done
                            break;
                        }

                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during AcquitDefaut: {ex.Message}");
            }
        }

        public async Task StartCollectAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 6); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);

                Console.WriteLine($"\nWORD90.6 (Cde_Auto.StartCollect) start collect");

                // GEMMA mode is constantly tracked and once it is not anymore in D2, we reset the Acquit bit
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int stepCyclePrincipal = StepCyclePrincipal; // StepCyclePrincipal updated by PollAsync()

                        if (stepCyclePrincipal != 0) // As soon as Step is not 0 anymore
                        {
                            Console.WriteLine($"Acquittement effectué (GEMMA={stepCyclePrincipal:X2}), reset du bit Start collect");

                            // reset bit 6 to 0
                            int resetValue = ClearBit(newValue, 6);
                            await WriteSingleRegisterAsync(90, resetValue);
                            DoneFlag = true; // Set the flag to true when done
                            break;
                        }

                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during StartCollect: {ex.Message}");
            }
        }

        public async Task StopCollectAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 7); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);

                Console.WriteLine($"\nWORD90.7 (Cde_Auto.StopCollect) stop collect");

                // GEMMA mode is constantly tracked and once it is not anymore in D2, we reset the Acquit bit
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        int stepCyclePrincipal = StepCyclePrincipal; // StepCyclePrincipal updated by PollAsync()

                        if (stepCyclePrincipal != 35) // As soon as Step is not 35 anymore
                        {
                            Console.WriteLine($"Acquittement effectué (GEMMA={stepCyclePrincipal:X2}), reset du bit Stop collect");

                            // reset bit 7 to 0
                            int resetValue = ClearBit(newValue, 7);
                            await WriteSingleRegisterAsync(90, resetValue);
                            DoneFlag = true; // Set the flag to true when done
                            break;
                        }

                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during StopCollect: {ex.Message}");
            }
        }


        public async Task AnalyseVisionADoneAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word91, 0); // Word91 updated by PollAsync()
                await WriteSingleRegisterAsync(91, newValue);

                // Bit requestAnalyseVisionA is constantly tracked and once it is false, we reset 
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        if (!RequestAnalyseVisionA) // Once RequestAnalyseVisionA is false, reset the bit Analyse Done
                        {
                            int resetValue = ClearBit(newValue, 0);
                            await WriteSingleRegisterAsync(91, resetValue);
                            DoneFlag = true;
                            break;
                        }
                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });

                Console.WriteLine($"\nWORD91.0 (Cde_Auto.AnalyseVisionA) analyse vision A done sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during AnalyseVisionADone: {ex.Message}");
            }
        }

        public async Task AnalyseVisionBDoneAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word91, 1); // Word91 updated by PollAsync()
                await WriteSingleRegisterAsync(91, newValue);

                // Bit requestAnalyseVisionB is constantly tracked and once it is false, we reset
                await Task.Run(async () =>
                {
                    while (true)
                    {
                        if (!RequestAnalyseVisionB) // Once RequestAnalyseVisionB is false, reset the bit Analyse Done
                        {
                            int resetValue = ClearBit(newValue, 1);
                            await WriteSingleRegisterAsync(91, resetValue);
                            DoneFlag = true;
                            break;
                        }
                        await Task.Delay(200); // wait before reading again (avoids saturating CPU)
                    }
                });

                Console.WriteLine($"\nWORD91.0 (Cde_Auto.AnalyseVisionA) analyse vision A done sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during AnalyseVisionADone: {ex.Message}");
            }
        }

        public async Task SendCoordinatesAsync(int plateau_nb, int x_multiple_slide, int y_multiple_slide, int x_multiple_cavity, int y_multiple_cavity)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }

            uint x_coord_mm = 30 * (x_multiple_slide - 1) + 6 * (x_multiple_cavity - 1);
            uint y_coord_mm = 84 * (y_multiple_slide - 1) + 6 * (y_multiple_cavity - 1);
            uint z_coord_mm = 70;

            uint x_coord_cmm = x_coord_mm * 100; // Convert to 100th of milimeters
            uint y_coord_cmm = y_coord_mm * 100; // Convert to 100th of milimeters
            uint z_coord_cmm = z_coord_mm * 100; // Convert to 100th of milimeters

            try
            {
                await WriteSingleRegisterAsync(96, plateau_nb);  // Destination_A_Plateau
                await WriteSingleRegisterAsync(97, x_coord_cmm); // Destination_A_X
                await WriteSingleRegisterAsync(98, y_coord_cmm); // Destination_A_Y
                await WriteSingleRegisterAsync(99, z_coord_cmm); // Destination_A_Y

                await WriteSingleRegisterAsync(100, plateau_nb);  // Destination_B_Plateau
                await WriteSingleRegisterAsync(101, x_coord_cmm); // Destination_B_X
                await WriteSingleRegisterAsync(102, y_coord_cmm); // Destination_B_Y
                await WriteSingleRegisterAsync(103, z_coord_cmm); // Destination_B_Z

                Console.WriteLine($"\nCoordinates sent to GEMMA: Plateau={plateau_nb}, X={x_coord_mm} mm, Y={y_coord_mm} mm");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending coordinates: {ex.Message}");
            }

            Console.WriteLine($"Computed coordinates: X={x_coord_mm}, Y={y_coord_mm}");
        }

        public async Task HardResetAsync()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                int newValue = SetBit(Word90, 15); // Word90 updated by PollAsync()
                await WriteSingleRegisterAsync(90, newValue);
                DoneFlag = true; // Set the flag to true when done

                Console.WriteLine($"\nWORD90.15 (Cde_Auto.HardReset) hard reset sent");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during HardReset: {ex.Message}");
            }
        }

        public async Task SetVialNbAsync(int vial_number)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }
            try
            {
                await WriteSingleRegisterAsync(105, vial_number);
                Console.WriteLine($"\nWORD105 (Vial Number) set to {vial_number}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError setting Auto mode: {ex.Message}");
            }
        }
        //------------------------------------------------------------------------------------------


        /// Read / Write methods
        //------------------------------------------------------------------------------------------
        public async Task WriteBitAsync(int wordNumber, int bitIndex, bool value, string role = "Unknown")
        {
            if (!IsConnected)
            {
                Console.WriteLine("\nCannot write - Modbus not connected");
                return;
            }

            try
            {
                int currentValue = await ReadHoldingRegisterAsync(wordNumber);
                int newValue = value ? SetBit(currentValue, bitIndex) : ClearBit(currentValue, bitIndex);

                await WriteSingleRegisterAsync(wordNumber, newValue);

                Console.WriteLine($"\nWORD{wordNumber}.{bitIndex} ({role}) changed {GetBit(currentValue, bitIndex)} -> {GetBit(newValue, bitIndex)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError writing to WORD{wordNumber}.{bitIndex}: {ex.Message}");
            }
        }

        public async Task WriteWordAsync(int wordNumber, int value, string role = "Unknown")
        {
            if (!IsConnected)
            {
                Console.WriteLine("\nCannot write - Modbus not connected");
                return;
            }

            try
            {
                int currentvalue = await ReadHoldingRegisterAsync(wordNumber);
                await WriteSingleRegisterAsync(wordNumber, value);
                Console.WriteLine($"\nWORD{wordNumber} ({role}) changed {currentvalue} (decimal) / 0x{currentvalue:X2} (hex) -> {value} (decimal) / 0x{value:X2} (hex)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError writing to WORD{wordNumber}: {ex.Message}");
            }
        }

        public async Task<int> ReadHoldingRegisterAsync(int address)
        {
            if (!IsConnected)
            {
                Console.WriteLine($"[ReadHoldingRegisterAsync] Not connected (address {address})");
                return -1;
            }

            try
            {
                return await Task.Run(() => modbusClient.ReadHoldingRegisters(address, 1)[0]);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ReadHoldingRegisterAsync] Error at address {address}: {ex.Message}");
                IsConnected = false;
                return -1;
            }
        }


        public Task WriteSingleRegisterAsync(int address, int value)
        {
            return Task.Run(() => modbusClient.WriteSingleRegister(address, value));
        }
        //------------------------------------------------------------------------------------------

        // Get bits values
        //------------------------------------------------------------------------------------------
        private async Task DisplayWordBits(int wordNumber, int startBit = 0, int count = 16)
        {
            Console.WriteLine($"\n- Holding Registers word{wordNumber}:");
            int wordValue = await ReadHoldingRegisterAsync(wordNumber);
            var getBitNameFunc = GetBitNameFuncByReflection(wordNumber);
            for (int i = 0; i < count; i++)
            {
                int bitIndex = startBit + i;
                bool bitValue = GetBit(wordValue, bitIndex);
                string bitName = getBitNameFunc(bitIndex);
                Console.WriteLine($"    Bit {bitIndex}: {bitValue} - {bitName}");
            }
        }
        //------------------------------------------------------------------------------------------

        // Bits operation functions
        //------------------------------------------------------------------------------------------
        // Set a specific bit to 1 (TRUE)
        private int SetBit(int value, int position)
        {
            return value | (1 << position);
        }
        /// Clear a specific bit to 0 (FALSE)
        private int ClearBit(int value, int position)
        {
            return value & ~(1 << position);
        }
        /// Toggle a specific bit (0→1 or 1→0)
        private int ToggleBit(int value, int position)
        {
            return value ^ (1 << position);
        }
        /// Check if a specific bit is set
        private bool GetBit(int value, int position)
        {
            return (value & (1 << position)) != 0;
        }
        //------------------------------------------------------------------------------------------

        // Destination plateau
        //------------------------------------------------------------------------------------------
        // Set destination plateau for particle A
        public async Task SetDestinationA_Plateau(int destination)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }

            if (destination < 0 || destination > 3)
            {
                Console.WriteLine("Invalid destination value. Must be 0-3 (0=Rebut; 1=Plateau Gauche; 2=Plateau Droite; 3=Analyseur)");
                return;
            }

            try
            {
                modbusClient.WriteSingleRegister(96, destination);
                Console.WriteLine($"WORD96 - Destination_A_Plateau set to: {destination} ({GetDestinationName(destination)})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Destination_A_Plateau: {ex.Message}");
            }
        }

        /// Set X coordinate for particle A deposition (in thousandths)
        public async Task SetDestinationA_X(int x)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }

            try
            {
                modbusClient.WriteSingleRegister(97, x);
                Console.WriteLine($"WORD97 - Destination_A_X set to: {x} (thousandths)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Destination_A_X: {ex.Message}");
            }
        }

        /// Set Y coordinate for particle A deposition (in thousandths)
        public async Task SetDestinationA_Y(int y)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }

            try
            {
                modbusClient.WriteSingleRegister(98, y);
                Console.WriteLine($"WORD98 - Destination_A_Y set to: {y} (thousandths)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Destination_A_Y: {ex.Message}");
            }
        }

        // Set Z coordinate for particle A deposition (in thousandths)
        public async Task SetDestinationA_Z(int z)
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot write - Modbus not connected");
                return;
            }

            try
            {
                modbusClient.WriteSingleRegister(99, z);
                Console.WriteLine($"WORD99 - Destination_A_Z set to: {z} (thousandths)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing Destination_A_Z: {ex.Message}");
            }
        }

        // Set complete destination parameters for particle A
        public async Task SetDestinationA_Complete(int plateau, int x, int y, int z)
        {
            Console.WriteLine($"\n----- SETTING PARTICLE A DESTINATION -----");
            await SetDestinationA_Plateau(plateau);
            if (plateau == 1 || plateau == 2) // Only set coordinates for plateaus
            {
                await SetDestinationA_X(x);
                await SetDestinationA_Y(y);
                await SetDestinationA_Z(z);
            }
            Console.WriteLine($"Particle A destination set: {GetDestinationName(plateau)}");
            if (plateau == 1 || plateau == 2)
            {
                Console.WriteLine($"Coordinates: X={x}, Y={y}, Z={z} (thousandths)");
            }
            Console.WriteLine("---------------------------------------------\n");
        }

        public async Task ReadDestinationParameters()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Cannot read - Modbus not connected");
                return;
            }

            try
            {
                // Read all destination parameters at once
                int[] parameters = modbusClient.ReadHoldingRegisters(96, 8);

                Console.WriteLine("\n=== DESTINATION PARAMETERS ===");
                Console.WriteLine("PARTICLE A:");
                Console.WriteLine($"  Plateau: {parameters[0]} ({GetDestinationName(parameters[0])})");
                Console.WriteLine($"  X: {parameters[1]} (thousandths)");
                Console.WriteLine($"  Y: {parameters[2]} (thousandths)");
                Console.WriteLine($"  Z: {parameters[3]} (thousandths)");

                Console.WriteLine("PARTICLE B:");
                Console.WriteLine($"  Plateau: {parameters[4]} ({GetDestinationName(parameters[4])})");
                Console.WriteLine($"  X: {parameters[5]} (thousandths)");
                Console.WriteLine($"  Y: {parameters[6]} (thousandths)");
                Console.WriteLine($"  Z: {parameters[7]} (thousandths)");
                Console.WriteLine("==============================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading destination parameters: {ex.Message}");
            }
        }
        //------------------------------------------------------------------------------------------

        // Helper function to name the bits/words/GEMMA modes according to your register table
        //------------------------------------------------------------------------------------------
        // Use reflection to get the appropriate GetBitNameWord{wordNumber} method
        private Func<int, string> GetBitNameFuncByReflection(int wordNumber)
        {
            var method = GetType().GetMethod($"GetBitNameWord{wordNumber}", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
                return (bit) => (string)method.Invoke(this, new object[] { bit });
            return (bit) => $"Bit {bit} - Non défini pour WORD{wordNumber}";
        }


        /// Get GEMMA description - hex-based encoding
        public string GetGEMMADescription(int mode)
        {
            return mode switch
            {
                // Stop states (A series) - hex encoded
                161 => "A1 - Arrêt dans état initial",                    // 0xA1
                162 => "A2 - Arrêt demandé en fin de cycle",             // 0xA2
                163 => "A3 - Arrêt demandé dans état déterminé",         // 0xA3
                164 => "A4 - Arrêt obtenu",                              // 0xA4
                165 => "A5 - Préparation pour remise en route après défaillance", // 0xA5
                166 => "A6 - Mise en position pour production",          // 0xA6
                167 => "A7 - Mise en énergie",                           // 0xA7

                // Operating states (F series) - hex encoded
                241 => "F1 - Production normale",                        // 0xF1
                242 => "F2 - Marche de préparation",                     // 0xF2
                243 => "F3 - Marche de clôture",                         // 0xF3
                244 => "F4 - Marche de vérification dans le désordre",   // 0xF4
                245 => "F5 - Marche de vérification dans l'ordre",       // 0xF5
                246 => "F6 - Marche de test",                            // 0xF6

                // Fault states (D series) - hex encoded
                209 => "D1 - Arrêt d'urgence",                           // 0xD1
                210 => "D2 - Diagnostic/traitement défaillance",         // 0xD2
                211 => "D3 - Production tout de même",                   // 0xD3

                _ => $"Mode non défini ({mode} = 0x{mode:X2})"
            };
        }

        // Get destination name based on destination code
        private string GetDestinationName(int destination)
        {
            return destination switch
            {
                0 => "Rebut",
                1 => "Plateau Gauche",
                2 => "Plateau Droite",
                3 => "Analyseur",
                _ => $"Destination inconnue ({destination})"
            };
        }

        // Allow to dynamically get GetBitNameWord{wordNumber} functions

        private string GetBitNameWord0(int bit)
        {
            return bit switch
            {
                0 => "Demande_Analyse_Vision_A",
                1 => "Demande_Analyse_Vision_B",
                2 => "Demande_Controle_vide_A",
                3 => "Demande_Controle_vide_B",
                _ => "Réserve"
            };
        }
        private string GetBitNameWord3(int bit)
        {
            return bit switch
            {
                0 => "Arrêt_urgence",                    // WORD3.0 not defined in your table
                1 => "I_DemAcces",                         // WORD3.1 - Bouton poussoir demande d'accès (I0.1)
                2 => "I_bp_dcy",                           // WORD3.2 - Bouton poussoir départ cycle (I0.2)
                3 => "I_Commut",                           // WORD3.3 - Commutateur pour Vibration manuelle à 100% (I0.3)
                4 => "I_Pressostat",                       // WORD3.4 - Présence d'air (I0.4)
                5 => "I_RelaiNonRearme",                   // WORD3.5 - Relais de sécurité non ré-armé (I0.5)
                6 => "I_porte_gauche_ouverte",             // WORD3.6 - Porte gauche ouverte (I0.6)
                7 => "I_porte_droite_ouverte",             // WORD3.7 - Porte droite ouverte (I0.7)
                8 => "Présence_24V",
                _ => "Réserve"
            };
        }
        private string GetBitNameWord4(int bit)
        {
            return bit switch
            {
                0 => "I_PRES_Sortie_BOL",           // WORD4.0 - Présence particule en sortie Bol vibrant (I100.0)
                1 => "I_PRES_Sortie_Rail_1",       // WORD4.1 - Présence particule en sortie vibrateur linéaire 1 (I100.1)
                2 => "I_PRES_Sortie_Rail_2",       // WORD4.2 - Présence particule en sortie vibrateur linéaire 2 (I100.2)
                3 => "I_IND_Presence_Fiole",       // WORD4.3 - Présence d'une fiole (I100.3)
                4 => "I_NET_Basculeur_Sortie",     // WORD4.4 - Basculeur sorti dans le bol vibrant (I100.4)
                5 => "I_NET_Basculeur_rentre",     // WORD4.5 - Basculeur rentré hors du bol (I100.5)
                6 => "I_NET_Balayage_0",           // WORD4.6 - Balayage en position 0° (I100.6)
                7 => "I_NET_Balayage_180",         // WORD4.7 - Balayage en position 180° (I100.7)
                8 => "I_reserve_101_0",            // WORD4.8 - Réserve (I101.0)
                9 => "I_reserve_101_1",            // WORD4.9 - Réserve (I101.1)
                10 => "I_reserve_101_2",           // WORD4.10 - Réserve (I101.2)
                11 => "I_reserve_101_3",           // WORD4.11 - Réserve (I101.3)
                12 => "I_reserve_101_4",           // WORD4.12 - Réserve (I101.4)
                13 => "I_reserve_101_5",           // WORD4.13 - Réserve (I101.5)
                14 => "I_reserve_101_6",           // WORD4.14 - Réserve (I101.6)
                15 => "I_reserve_101_7",           // WORD4.15 - Réserve (I101.7)
                _ => "Réserve"
            };
        }

        private string GetBitNameWord5(int bit)
        {
            return bit switch
            {
                0 => "I_DEV_verin_haut",           // WORD5.0 - Déverseur en Haut (I105.0)
                1 => "I_DEV_verin_bas",            // WORD5.1 - Déverseur en Bas (I105.1)
                2 => "I_DEV_Verin_Pince_Rentrer",  // WORD5.2 - Déverseur rentré (I105.2)
                3 => "I_DEV_Verin_Pince_Avancer",  // WORD5.3 - Déverseur sorti (I105.3)
                4 => "I_DEV_Pince_0",              // WORD5.4 - Déverseur en position normale (0°) (I105.4)
                5 => "I_DEV_Pince_180",            // WORD5.5 - Déverseur en position retournée (~180°) (I105.5)
                6 => "I_reserve_105_6",            // WORD5.6 - Réserve (I105.6)
                7 => "I_reserve_105_7",            // WORD5.7 - Réserve (I105.7)
                8 => "I_MAN_PRES_Fiole",           // WORD5.8 - Présence d'une fiole (I106.0)
                9 => "I_ANA_Pousseur_Tiroir_Rentre", // WORD5.9 - Le vérin est au repos et le tiroir en état de repos (I106.1)
                10 => "I_ANA_pousseur_Tiroir_Sorti", // WORD5.10 - Le vérin est sorti et le tiroir est sorti (I106.2)
                11 => "I_ANA_Tiroir_rentre",       // WORD5.11 - Le tiroir est en sécurité (I106.3)
                12 => "I_NET_entonnoir_rentre",    // WORD5.12 - Le vérin est au repos et le tiroir en état de repos (I106.4)
                13 => "I_NET_entonnoir_degager",   // WORD5.13 - Le vérin est sorti et le tiroir est sorti (I106.5)
                14 => "I_reserve_106_6",           // WORD5.14 - Réserve (I106.6)
                15 => "I_reserve_106_7",           // WORD5.15 - Réserve (I106.7)
                _ => "Réserve"
            };
        }

        private string GetBitNameWord6(int bit)
        {
            return bit switch
            {
                0 => "I_DEV_Pince_opened",         // WORD6.0 - Pince déverseur ouverte
                1 => "I_DEV_Pince_checked",        // WORD6.1 - Pince déverseur fermée complétement
                2 => "I_DEV_Pince_closed",         // WORD6.2 - Pince déverseur fermée sur fiole
                3 => "I_MAN_Pince_opened",         // WORD6.3 - Pince manipulateur ouverte
                4 => "I_MAN_Pince_checked",        // WORD6.4 - Pince manipulateur fermée complétement
                5 => "I_MAN_Pince_closed",         // WORD6.5 - Pince manipulateur fermée sur fiole
                _ => "Réserve"
            };
        }

        private string GetBitNameWord7(int bit)
        {
            return bit switch
            {
                0 => "Indexeur_homed",             // WORD7.0 - Variateur Indexeur référencé
                1 => "Indexeur_powerEnabled",      // WORD7.1 - Variateur Indexeur sous puissance
                2 => "Indexeur_error",             // WORD7.2 - Variateur Indexeur en défaut
                3 => "Indexeur_Busy",              // WORD7.3 - Variateur Indexeur occupé
                4 => "Indexeur_Moving",            // WORD7.4 - Variateur Indexeur en mouvement
                5 => "Indexeur_inPosition",        // WORD7.5 - Variateur Indexeur en position
                6 => "Réserve",                    // WORD7.6 - Réserve
                7 => "Réserve",                    // WORD7.7 - Réserve
                8 => "Analyseur_homed",            // WORD7.8 - Variateur Analyseur référencé
                9 => "Analyseur_powerEnabled",     // WORD7.9 - Variateur Analyseur sous puissance
                10 => "Analyseur_error",           // WORD7.10 - Variateur Analyseur en défaut
                11 => "Analyseur_Busy",            // WORD7.11 - Variateur Analyseur occupé
                12 => "Analyseur_Moving",          // WORD7.12 - Variateur Analyseur en mouvement
                13 => "Analyseur_inPosition",      // WORD7.13 - Variateur Analyseur en position
                14 => "Réserve",                   // WORD7.14 - Réserve
                15 => "Réserve",                   // WORD7.15 - Réserve
                _ => "Réserve"
            };
        }

        private string GetBitNameWord8(int bit)
        {
            return bit switch
            {
                0 => "Manip_XY_homed",             // WORD8.0 - Variateur Manipulateur XY référencé
                1 => "Manip_XY_powerEnabled",      // WORD8.1 - Variateur Manipulateur XY sous puissance
                2 => "Manip_XY_error",             // WORD8.2 - Variateur Manipulateur XY en défaut
                3 => "Manip_XY_Busy",              // WORD8.3 - Variateur Manipulateur XY occupé
                4 => "Manip_XY_Moving",            // WORD8.4 - Variateur Manipulateur XY en mouvement
                5 => "Manip_XY_inPosition",        // WORD8.5 - Variateur Manipulateur XY en position
                6 => "Réserve",                    // WORD8.6 - Réserve
                7 => "Réserve",                    // WORD8.7 - Réserve
                8 => "Manip_Z_homed",              // WORD8.8 - Variateur Manipulateur Z référencé
                9 => "Manip_Z_powerEnabled",       // WORD8.9 - Variateur Manipulateur Z sous puissance
                10 => "Manip_Z_error",             // WORD8.10 - Variateur Manipulateur Z en défaut
                11 => "Manip_Z_Busy",              // WORD8.11 - Variateur Manipulateur Z occupé
                12 => "Manip_Z_Moving",            // WORD8.12 - Variateur Manipulateur Z en mouvement
                13 => "Manip_Z_inPosition",        // WORD8.13 - Variateur Manipulateur Z en position
                14 => "Réserve",                   // WORD8.14 - Réserve
                15 => "Réserve",                   // WORD8.15 - Réserve
                _ => "Réserve"
            };
        }

        private string GetBitNameWord9(int bit)
        {
            return bit switch
            {
                0 => "Vision_Z_homed",             // WORD9.0 - Variateur vision Z référencé
                1 => "Vision_Z_powerEnabled",      // WORD9.1 - Variateur vision Z sous puissance
                2 => "Vision_Z_error",             // WORD9.2 - Variateur vision Z en défaut
                3 => "Vision_Z_Busy",              // WORD9.3 - Variateur vision Z occupé
                4 => "Vision_Z_Moving",            // WORD9.4 - Variateur vision Z en mouvement
                5 => "Vision_Z_inPosition",        // WORD9.5 - Variateur vision Z en position
                6 => "Vision_Z_TaskDone",          // WORD9.6 - Variateur vision Z Mouvement exécuté
                _ => "Réserve"
            };
        }

        private string GetBitNameWord77(int bit)
        {
            return bit switch
            {
                0 => "Fiole1_Presence_Rack",
                1 => "Fiole1_ProcessCompleted",
                2 => "Fiole1_CollectCompleted",
                3 => "Fiole1_ProcessAborted",
                4 => "Fiole2_Presence_Rack",
                5 => "Fiole2_ProcessCompleted",
                6 => "Fiole2_CollectCompleted",
                7 => "Fiole2_ProcessAborted",
                8 => "Fiole3_Presence_Rack",
                9 => "Fiole3_ProcessCompleted",
                10 => "Fiole3_CollectCompleted",
                11 => "Fiole3_ProcessAborted",
                12 => "Fiole4_Presence_Rack",
                13 => "Fiole4_ProcessCompleted",
                14 => "Fiole4_CollectCompleted",
                15 => "Fiole4_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord78(int bit)
        {
            return bit switch
            {
                0 => "Fiole5_Presence_Rack",
                1 => "Fiole5_ProcessCompleted",
                2 => "Fiole5_CollectCompleted",
                3 => "Fiole5_ProcessAborted",
                4 => "Fiole6_Presence_Rack",
                5 => "Fiole6_ProcessCompleted",
                6 => "Fiole6_CollectCompleted",
                7 => "Fiole6_ProcessAborted",
                8 => "Fiole7_Presence_Rack",
                9 => "Fiole7_ProcessCompleted",
                10 => "Fiole7_CollectCompleted",
                11 => "Fiole7_ProcessAborted",
                12 => "Fiole8_Presence_Rack",
                13 => "Fiole8_ProcessCompleted",
                14 => "Fiole8_CollectCompleted",
                15 => "Fiole8_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord79(int bit)
        {
            return bit switch
            {
                0 => "Fiole9_Presence_Rack",
                1 => "Fiole9_ProcessCompleted",
                2 => "Fiole9_CollectCompleted",
                3 => "Fiole9_ProcessAborted",
                4 => "Fiole10_Presence_Rack",
                5 => "Fiole10_ProcessCompleted",
                6 => "Fiole10_CollectCompleted",
                7 => "Fiole10_ProcessAborted",
                8 => "Fiole11_Presence_Rack",
                9 => "Fiole11_ProcessCompleted",
                10 => "Fiole11_CollectCompleted",
                11 => "Fiole11_ProcessAborted",
                12 => "Fiole12_Presence_Rack",
                13 => "Fiole12_ProcessCompleted",
                14 => "Fiole12_CollectCompleted",
                15 => "Fiole12_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord80(int bit)
        {
            return bit switch
            {
                0 => "Fiole13_Presence_Rack",
                1 => "Fiole13_ProcessCompleted",
                2 => "Fiole13_CollectCompleted",
                3 => "Fiole13_ProcessAborted",
                4 => "Fiole14_Presence_Rack",
                5 => "Fiole14_ProcessCompleted",
                6 => "Fiole14_CollectCompleted",
                7 => "Fiole14_ProcessAborted",
                8 => "Fiole15_Presence_Rack",
                9 => "Fiole15_ProcessCompleted",
                10 => "Fiole15_CollectCompleted",
                11 => "Fiole15_ProcessAborted",
                12 => "Fiole16_Presence_Rack",
                13 => "Fiole16_ProcessCompleted",
                14 => "Fiole16_CollectCompleted",
                15 => "Fiole16_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord81(int bit)
        {
            return bit switch
            {
                0 => "Fiole17_Presence_Rack",
                1 => "Fiole17_ProcessCompleted",
                2 => "Fiole17_CollectCompleted",
                3 => "Fiole17_ProcessAborted",
                4 => "Fiole18_Presence_Rack",
                5 => "Fiole18_ProcessCompleted",
                6 => "Fiole18_CollectCompleted",
                7 => "Fiole18_ProcessAborted",
                8 => "Fiole19_Presence_Rack",
                9 => "Fiole19_ProcessCompleted",
                10 => "Fiole19_CollectCompleted",
                11 => "Fiole19_ProcessAborted",
                12 => "Fiole20_Presence_Rack",
                13 => "Fiole20_ProcessCompleted",
                14 => "Fiole20_CollectCompleted",
                15 => "Fiole20_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord82(int bit)
        {
            return bit switch
            {
                0 => "Fiole21_Presence_Rack",
                1 => "Fiole21_ProcessCompleted",
                2 => "Fiole21_CollectCompleted",
                3 => "Fiole21_ProcessAborted",
                4 => "Fiole22_Presence_Rack",
                5 => "Fiole22_ProcessCompleted",
                6 => "Fiole22_CollectCompleted",
                7 => "Fiole22_ProcessAborted",
                8 => "Fiole23_Presence_Rack",
                9 => "Fiole23_ProcessCompleted",
                10 => "Fiole23_CollectCompleted",
                11 => "Fiole23_ProcessAborted",
                12 => "Fiole24_Presence_Rack",
                13 => "Fiole24_ProcessCompleted",
                14 => "Fiole24_CollectCompleted",
                15 => "Fiole24_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord83(int bit)
        {
            return bit switch
            {
                0 => "Fiole25_Presence_Rack",
                1 => "Fiole25_ProcessCompleted",
                2 => "Fiole25_CollectCompleted",
                3 => "Fiole25_ProcessAborted",
                4 => "Fiole26_Presence_Rack",
                5 => "Fiole26_ProcessCompleted",
                6 => "Fiole26_CollectCompleted",
                7 => "Fiole26_ProcessAborted",
                8 => "Fiole27_Presence_Rack",
                9 => "Fiole27_ProcessCompleted",
                10 => "Fiole27_CollectCompleted",
                11 => "Fiole27_ProcessAborted",
                12 => "Fiole28_Presence_Rack",
                13 => "Fiole28_ProcessCompleted",
                14 => "Fiole28_CollectCompleted",
                15 => "Fiole28_ProcessAborted",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord84(int bit)
        {
            return bit switch
            {
                0 => "Fiole29_Presence_Rack",
                1 => "Fiole29_ProcessCompleted",
                2 => "Fiole29_CollectCompleted",
                3 => "Fiole29_ProcessAborted",
                4 => "Fiole30_Presence_Rack",
                5 => "Fiole30_ProcessCompleted",
                6 => "Fiole30_CollectCompleted",
                7 => "Fiole30_ProcessAborted",
                8 => "FioleRebus_Presence",
                9 => "FioleRebus_A_verifier",
                10 => "FioleAnalyseur_Presence",
                11 => "FioleAnalyseur_A_Verifier",
                12 => "FioleManipulateur_Presence",
                13 => "Réserve",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord85(int bit)
        {
            return bit switch
            {
                0 => "ParticuleAnalyseur_Vibrateur_A_Presence",
                1 => "ParticuleAnalyseur_Vibrateur_B_Presence",
                2 => "ParticuleAnalyseur_Vibrateur_A_Verifier",
                3 => "ParticuleAnalyseur_Vibrateur_B_Verifier",
                4 => "ParticuleAnalyseur_Camera_A_Presence",
                5 => "ParticuleAnalyseur_Camera_B_Presence",
                6 => "ParticuleAnalyseur_Camera_A_Verifier",
                7 => "ParticuleAnalyseur_Camera_B_Verifier",
                8 => "ParticuleAnalyseur_Manip_A_Presence",
                9 => "ParticuleAnalyseur_Manip_B_Presence",
                10 => "ParticuleAnalyseur_Manip_A_A_Verifier",
                11 => "ParticuleAnalyseur_Manip_B_A_Verifier",
                12 => "ParticuleManipulateur_Presence",
                13 => "Réserve",
                _ => "Réserve"
            };
        }

        // private string GetBitNameWord90(int bit)
        // {
        //     return bit switch
        //     {
        //         0 => "Cde_Auto.Acquit - Demande d'acquittement des défauts",
        //         1 => "Cde_Auto.Mode_Auto - Passage en mode Auto (Auto=TRUE/MANU=FALSE)",
        //         2 => "Cde_Auto.Start - Demande départ cycle : Passage en Mode F1",
        //         3 => "Cde_Auto.Stop - Demande arrêt cycle : Passage de F1 en A2 (puis A1 quand cycle terminé)",
        //         4 => "Cde_Auto.Init - Demande initialisation : Passage de D2 ou F4 en A6 (puis A2 quand initialisation terminée)",
        //         5 => "Cde_Auto.Avec_controle_vide - Mode vérification slot vide par caméra après prise/dépose (mode actif=TRUE)",
        //         6 => "Cde_Auto.Collect_Start - Démarrage du cycle principale de la fiole N (Param_N_Fiole)",
        //         7 => "Cde_Auto.Collect_Stop - Demande d'arrêt de collecte de la fiole N (déclenche la vidange et la fin du cycle principale)",
        //         8 => "Cde_Auto.Vidange_Stop - Demande d'arrêt de la vidange (arrête la vidange même si le convoyage n'est pas vide)",
        //         _ => "Réserve"
        //     };
        // }

        private string GetBitNameWord90(int bit)
        {
            return bit switch
            {
                0 => "Cde_Auto.Acquit",
                1 => "Cde_Auto.Mode_Auto",
                2 => "Cde_Auto.Start",
                3 => "Cde_Auto.Stop",
                4 => "Cde_Auto.Init",
                5 => "Cde_Auto.Avec_controle_vide",
                6 => "Cde_Auto.Collect_Start",
                7 => "Cde_Auto.Collect_Stop",
                8 => "Cde_Auto.Vidange_Stop",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord91(int bit)
        {
            return bit switch
            {
                0 => "Cde_Auto.Vision_Analyse_A_Done - Retour d'information fin d'analyse vision du slot A",
                1 => "Cde_Auto.Vision_Analyse_B_Done - Retour d'information fin d'analyse vision du slot B",
                2 => "Cde_Auto.Vision_Controle_vide_A_Done - Retour d'information fin de contrôle vide du slot A",
                3 => "Cde_Auto.Vision_Controle_vide_B_Done - Retour d'information fin de contrôle vide du slot B",
                4 => "Cde_Auto.Vision_presence_A - Présence particule dans le slot A (TRUE=Présence/FALSE=Absence)",
                5 => "Cde_Auto.Vision_presence_B - Présence particule dans le slot B (TRUE=Présence/FALSE=Absence)",
                6 => "Cde_Auto.Axe_Vision_Z_Execute - Demande déplacement axe vision à la position",
                7 => "Réserve",
                8 => "Cde_Auto.Vision_A_Eclairage1 - Demande Eclairage vision A (Section 1)",
                9 => "Cde_Auto.Vision_A_Eclairage2 - Demande Eclairage vision A (Section 2)",
                10 => "Cde_Auto.Vision_A_Eclairage3 - Demande Eclairage vision A (Section 3)",
                11 => "Cde_Auto.Vision_A_Eclairage4 - Demande Eclairage vision A (Section 4)",
                12 => "Cde_Auto.Vision_B_Eclairage1 - Demande Eclairage vision B (Section 1)",
                13 => "Cde_Auto.Vision_B_Eclairage2 - Demande Eclairage vision B (Section 2)",
                14 => "Cde_Auto.Vision_B_Eclairage3 - Demande Eclairage vision B (Section 3)",
                15 => "Cde_Auto.Vision_B_Eclairage4 - Demande Eclairage vision B (Section 4)",
                _ => "Réserve"
            };
        }

        // Alarm words
        private string GetBitNameWord70(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1100 Arrêt d'urgence Enclenché",
                1 => "Alarme_1101 Pneumatique faible",
                2 => "Alarme_1102 Machine non réarmée",
                3 => "Alarme_1103 Défaut 24V",
                4 => "Alarme_1104 Défaut variateur Indexeur",
                5 => "Alarme_1105 Défaut variateur Analyseur",
                6 => "Alarme_1106 Défaut variateur Manip_XY",
                7 => "Alarme_1107 Défaut variateur Manip_Z",
                8 => "Alarme_1108 Défaut variateur Vision_Z",
                9 => "Alarme_1109 Indexeur non référencé",
                10 => "Alarme_1110 Analyseur non référencé",
                11 => "Alarme_1111 Manip_XY non référencé",
                12 => "Alarme_1112 Manip_Z non référencé",
                13 => "Alarme_1113 Vision_Z non référencé",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord71(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1116 Timeout Fermeture Pince déverseur",
                1 => "Alarme_1117 Timeout Ouverture Pince déverseur",
                2 => "Alarme_1118 Timeout Contrôle Pince déverseur",
                3 => "Alarme_1119 Timeout Fermeture Pince manipulateur",
                4 => "Alarme_1120 Timeout Ouverture Pince manipulateur",
                5 => "Alarme_1121 Timeout Contrôle Pince manipulateur",
                6 => "Alarme_1122 Timeout Descendre vérin déverseur",
                7 => "Alarme_1123 Timeout Monter vérin déverseur",
                8 => "Alarme_1124 Incohérence capteurs vérin Descendre déverseur",
                9 => "Alarme_1125 Timeout vérin avancer déverseur",
                10 => "Alarme_1126 Timeout vérin reculer déverseur",
                11 => "Alarme_1127 Incohérence capteurs vérin avancer/reculer déverseur",
                12 => "Alarme_1128 Timeout vérin rotation 180 déverseur",
                13 => "Alarme_1129 Timeout vérin rotation 0 déverseur",
                14 => "Alarme_1130 Incohérence capteurs vérin rotation déverseur",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord72(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1132 Timeout vérin sortir basculeur nettoyage",
                1 => "Alarme_1133 Timeout vérin rentrer basculeur nettoyage",
                2 => "Alarme_1134 Incohérence capteurs vérin sortir/rentrer basculeur nettoyage",
                3 => "Alarme_1135 Timeout vérin rotation 180 balayage nettoyage",
                4 => "Alarme_1136 Timeout vérin rotation 0 balayage nettoyage",
                5 => "Alarme_1137 Incohérence capteurs vérin rotation balayage nettoyage",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord73(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1148 Grafcet Main en défaut",
                1 => "Alarme_1149 Grafcet Initialisation en défaut",
                2 => "Alarme_1150 Grafcet Manip_Depose_Analyseur en défaut",
                3 => "Alarme_1151 Grafcet Manip_Depose_Rack en défaut",
                4 => "Alarme_1152 Grafcet Manip_Depose_Rebut en défaut",
                5 => "Alarme_1153 Grafcet Manip_Depose_particule_A en défaut",
                6 => "Alarme_1154 Grafcet Manip_Depose_particule_B en défaut",
                7 => "Alarme_1155 Grafcet Manip_Controle_Fiole_Analyseur en défaut",
                8 => "Alarme_1156 Grafcet Manip_Controle_Fiole_Rack en défaut",
                9 => "Alarme_1157 Grafcet Manip_Controle_Fiole_Rebut en défaut",
                10 => "Alarme_1158 Grafcet Manip_Prise_Fiole_Analyseur en défaut",
                11 => "Alarme_1159 Grafcet Manip_Prise_Fiole_Rack en défaut",
                12 => "Alarme_1160 Grafcet Manip_Prise_Fiole_Rebut en défaut",
                13 => "Alarme_1161 Grafcet Manip_Prise_particule_A en défaut",
                14 => "Alarme_1162 Grafcet Manip_Prise_particule_B en défaut",
                15 => "Alarme_1163 Grafcet Deverseur_Prise en défaut",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord74(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1164 Grafcet Deverseur_Vider en défaut",
                1 => "Alarme_1165 Grafcet Deverseur_Depose en défaut",
                2 => "Alarme_1166 Grafcet Analyseur en défaut",
                3 => "Alarme_1167 Grafcet Analyseur_Vision_A en défaut",
                4 => "Alarme_1168 Grafcet Analyseur_Vision_B en défaut",
                5 => "Alarme_1169 Grafcet reception_particule en défaut",
                6 => "Alarme_1170 Grafcet Nettoyage_Auto en défaut",
                7 => "Alarme_1171 Grafcet Aspiration_entonoir en défaut",
                8 => "Alarme_1172 Grafcet Vidange en défaut",
                _ => "Réserve"
            };
        }
    }
}
