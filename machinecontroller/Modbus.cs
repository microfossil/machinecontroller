using System;
using System.Threading;
using System.Threading.Tasks;
using EasyModbus;

namespace ModbusTCP_Simplified
{
    /// <summary>
    /// Classe ultra-simplifiée pour la connexion Modbus TCP
    /// Contient uniquement l'essentiel pour établir une connexion
    /// </summary>
    public class Modbus
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public bool IsConnected { get; private set; }
        public bool Enabled { get; set; }

        private ModbusClient modbusClient;
        private SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public Modbus()
        {
            IPAddress = "192.168.0.1";
            Port = 502;
            modbusClient = new ModbusClient(IPAddress, Port);
            modbusClient.ConnectedChanged += (sender) =>
            {
                IsConnected = modbusClient.Connected;
                Console.WriteLine($"[MODBUS] Connection status changed: {(IsConnected ? "CONNECTED" : "DISCONNECTED")}");
            };
            Enabled = false;
            modbusClient.UnitIdentifier = 1;
        }

        /// <summary>
        /// Démarre la connexion Modbus
        /// </summary>
        public async Task StartAsync()
        {
            Console.WriteLine("----- Modbus.StartAsync called -----");
            Enabled = true;
            await ConnectAsync();
            Console.WriteLine("----- Modbus.StartAsync END call -----\n");
        }

        /// <summary>
        /// Arrête la connexion Modbus
        /// </summary>
        public void Stop()
        {
            Console.WriteLine("Stopping Modbus connection...");
            Enabled = false;
            modbusClient.Disconnect();
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
                        Console.WriteLine("Connected successfully!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Connection error: {ex.Message}");
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

        /// <summary>
        /// Test de connexion simple
        /// </summary>
        public async Task PollAsync()
        {
            Console.WriteLine("----- Modbus.PollAsync called -----");
            if (!Enabled) return;
            if (!IsConnected) await ConnectAsync();
            if (IsConnected)
            {
                try
                {
                    Console.WriteLine(" - Holding Registers - ");
                    try
                    {
                        // GEMMA MODE BEFORE WRITE ATTEMPT
                        //-----------------------------------------------------
                        Console.WriteLine($"\n----- GEMMA MODE -----");
                        int[] word1 = modbusClient.ReadHoldingRegisters(1, 1);
                        int word1value = word1[0];
                        Console.WriteLine($"Holding Registers word1: {word1value:X2}");
                        //-----------------------------------------------------

                        // Write attempt
                        //-----------------------------------------------------
                        Console.WriteLine($"\n----- SWITCH AUTO/MANUAL MODE -----");
                        int[] word90 = modbusClient.ReadHoldingRegisters(90, 1);
                        int word90value = word90[0];

                        // Read byte 2 from word 90
                        bool bitValue = (word90value & (1 << 1)) != 0;

                        Console.WriteLine($"Word 90 AUTO_MODE (bit 1): {bitValue}");

                        // Toggle bit 1 while preserving all other bits
                        int newValue;
                        if (bitValue)
                        {
                            // Clear bit 1 (set to MANUAL)
                            newValue = word90value & ~(1 << 1);
                        }
                        else
                        {
                            // Set bit 1 (set to AUTO)
                            newValue = word90value | (1 << 1);
                        }

                        modbusClient.WriteSingleRegister(90, newValue);
                        bool bitValue_new = (newValue & (1 << 1)) != 0;
                        Console.WriteLine($"Word 90 AUTO_MODE (bit 1): {bitValue_new}");
                        //-----------------------------------------------------

                        // GEMMA MODE AFTER WRITE ATTEMPT
                        //-----------------------------------------------------
                        Console.WriteLine($"\n----- GEMMA MODE -----");
                        int[] word1_new = modbusClient.ReadHoldingRegisters(1, 1);
                        int word1value_new = word1_new[0];
                        Console.WriteLine($"Holding Registers word1: {word1value_new:X2}");
                        //-----------------------------------------------------
                    }
                    catch (Exception ex) { Console.WriteLine($"Holding failed: {ex.Message}"); }

                    Console.WriteLine("\nPoll OK");
                    Console.WriteLine("----- Modbus.PollAsync END call -----\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Poll error: {ex.Message}");
                    Console.WriteLine("----- Modbus.PollAsync END call -----\n");
                }
            }
        }

        // Helper function to name the bits according to your register table
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

        private string GetBitNameWord70(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1100 - Arrêt d'urgence Enclenché",
                1 => "Alarme_1101 - Pneumatique faible",
                2 => "Alarme_1102 - Machine non réarmée",
                3 => "Alarme_1103 - Défaut 24V",
                4 => "Alarme_1104 - Défaut variateur Indexeur",
                5 => "Alarme_1105 - Défaut variateur Analyseur",
                6 => "Alarme_1106 - Défaut variateur Manip_XY",
                7 => "Alarme_1107 - Défaut variateur Manip_Z",
                8 => "Alarme_1108 - Défaut variateur Vision_Z",
                9 => "Alarme_1109 - Indexeur non référencé",
                10 => "Alarme_1110 - Analyseur non référencé",
                11 => "Alarme_1111 - Manip_XY non référencé",
                12 => "Alarme_1112 - Manip_Z non référencé",
                13 => "Alarme_1113 - Vision_Z non référencé",
                14 => "Alarme_1114 - Réserve",
                15 => "Alarme_1115 - Réserve",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord71(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1116 - Timeout Fermeture Pince déverseur",
                1 => "Alarme_1117 - Timeout Ouverture Pince déverseur",
                2 => "Alarme_1118 - Timeout Contrôle Pince déverseur",
                3 => "Alarme_1119 - Timeout Fermeture Pince manipulateur",
                4 => "Alarme_1120 - Timeout Ouverture Pince manipulateur",
                5 => "Alarme_1121 - Timeout Contrôle Pince manipulateur",
                6 => "Alarme_1122 - Timeout Descendre vérin déverseur",
                7 => "Alarme_1123 - Timeout Monter vérin déverseur",
                8 => "Alarme_1124 - Incohérence capteurs vérin Descendre déverseur",
                9 => "Alarme_1125 - Timeout vérin avancer déverseur",
                10 => "Alarme_1126 - Timeout vérin reculer déverseur",
                11 => "Alarme_1127 - Incohérence capteurs vérin avancer/reculer déverseur",
                12 => "Alarme_1128 - Timeout vérin rotation 180 déverseur",
                13 => "Alarme_1129 - Timeout vérin rotation 0 déverseur",
                14 => "Alarme_1130 - Incohérence capteurs vérin rotation déverseur",
                15 => "Alarme_1131 - Réserve",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord72(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1132 - Timeout vérin sortir basculeur nettoyage",
                1 => "Alarme_1133 - Timeout vérin rentrer basculeur nettoyage",
                2 => "Alarme_1134 - Incohérence capteurs vérin sortir/rentrer basculeur nettoyage",
                3 => "Alarme_1135 - Timeout vérin rotation 180 balayage nettoyage",
                4 => "Alarme_1136 - Timeout vérin rotation 0 balayage nettoyage",
                5 => "Alarme_1137 - Incohérence capteurs vérin rotation balayage nettoyage",
                _ => "Alarme_Réserve"
            };
        }

        private string GetBitNameWord73(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1148 - Grafcet Main en défaut",
                1 => "Alarme_1149 - Grafcet Initialisation en défaut",
                2 => "Alarme_1150 - Grafcet Manip_Depose_Analyseur en défaut",
                3 => "Alarme_1151 - Grafcet Manip_Depose_Rack en défaut",
                4 => "Alarme_1152 - Grafcet Manip_Depose_Rebut en défaut",
                5 => "Alarme_1153 - Grafcet Manip_Depose_particule_A en défaut",
                6 => "Alarme_1154 - Grafcet Manip_Depose_particule_B en défaut",
                7 => "Alarme_1155 - Grafcet Manip_Controle_Fiole_Analyseur en défaut",
                8 => "Alarme_1156 - Grafcet Manip_Controle_Fiole_Rack en défaut",
                9 => "Alarme_1157 - Grafcet Manip_Controle_Fiole_Rebut en défaut",
                10 => "Alarme_1158 - Grafcet Manip_Prise_Fiole_Analyseur en défaut",
                11 => "Alarme_1159 - Grafcet Manip_Prise_Fiole_Rack en défaut",
                12 => "Alarme_1160 - Grafcet Manip_Prise_Fiole_Rebut en défaut",
                13 => "Alarme_1161 - Grafcet Manip_Prise_particule_A en défaut",
                14 => "Alarme_1162 - Grafcet Manip_Prise_particule_B en défaut",
                15 => "Alarme_1163 - Grafcet Deverseur_Prise en défaut",
                _ => "Réserve"
            };
        }

        private string GetBitNameWord74(int bit)
        {
            return bit switch
            {
                0 => "Alarme_1164 - Grafcet Deverseur_Vider en défaut",
                1 => "Alarme_1165 - Grafcet Deverseur_Depose en défaut",
                2 => "Alarme_1166 - Grafcet Analyseur en défaut",
                3 => "Alarme_1167 - Grafcet Analyseur_Vision_A en défaut",
                4 => "Alarme_1168 - Grafcet Analyseur_Vision_B en défaut",
                5 => "Alarme_1169 - Grafcet reception_particule en défaut",
                6 => "Alarme_1170 - Grafcet Nettoyage_Auto en défaut",
                7 => "Alarme_1171 - Grafcet Aspiration_entonoir en défaut",
                8 => "Alarme_1172 - Grafcet Vidange en défaut",
                9 => "Alarme_1173 - Réserve",
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
    }
}
