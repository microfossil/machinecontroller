using System;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Threading;
using ModbusTCP_Simplified;

namespace ControlUI
{
    public partial class MainWindow : Window
    {
        public Modbus Modbus { get; set; }
        private DispatcherTimer uiTimer;

        public MainWindow()
        {
            InitializeComponent();

            // lancer la connexion après chargement de la fenêtre
            Loaded += async (s, e) => await ConnectAsync();
        }

        public async Task ConnectAsync()
        {
            Modbus = new Modbus();
            await Modbus.StartAsync();

            if (Modbus.IsConnected)
            {
                TxtStatus.Text = "✅ Connection successful";

                // Timer UI pour rafraîchir les valeurs affichées
                uiTimer = new DispatcherTimer();
                uiTimer.Interval = TimeSpan.FromMilliseconds(500);
                uiTimer.Tick += (s2, e2) => UpdateUI();
                uiTimer.Start();
            }
            else
            {
                TxtStatus.Text = "❌ Connection failed";
            }
        }

        private void UpdateUI()
        {
            if (Modbus.IsConnected)
            {
                TxtGEMMAMode.Text = $"{Modbus.GemmaMode} (decimal)\n{Modbus.GemmaMode:X2} (hexa)\n({Modbus.GetGEMMADescription(Modbus.GemmaMode)})";
                TxtFiole.Text = $"Fiole n°{Modbus.FioleNumber}";

                LedRequestAnalyseVisionA.Fill = Modbus.RequestAnalyseVisionA ? Brushes.LimeGreen : Brushes.Red;
                LedRequestAnalyseVisionB.Fill = Modbus.RequestAnalyseVisionB ? Brushes.LimeGreen : Brushes.Red;
                LedRequestControlVoidA.Fill = Modbus.RequestControlVoidA ? Brushes.LimeGreen : Brushes.Red;
                LedRequestControlVoidB.Fill = Modbus.RequestControlVoidB ? Brushes.LimeGreen : Brushes.Red;

                TxtWord90.Text = Modbus.TxtWord90;
                TxtStepCyclePrincipal.Text = $"{Modbus.StepCyclePrincipal}";

                if (Modbus.DoneFlag)
                {
                    TxtStatus.Text = TxtStatus.Text + "\n[DONE]";
                    Modbus.DoneFlag = false; // reset the flag
                }

                // Activate Btn startCollect and StopCollect when GEMMA is in mode F1
                if (Modbus.GemmaMode == 0xF1)
                    {
                        BtnStartCollect.IsEnabled = true;
                        BtnStopCollect.IsEnabled = true;
                    }
                    else
                    {
                        BtnStartCollect.IsEnabled = false;
                        BtnStopCollect.IsEnabled = false;
                    }
            }
            else
            {
                TxtStatus.Text = "❌ Disconnected";
            }
        }

        // private void ReadGemmaMode_Click(object sender, RoutedEventArgs e)
        // {
        //     int mode = Modbus.GetGEMMAMode();
        //     TxtGEMMAMode.Text = $"{mode} (decimal)\n{mode:X2} (hexa)\n({Modbus.GetGEMMADescription(mode)})";
        // }

        private async void BtnAuto_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Mode_Auto\nmode auto";
            await Modbus.SetAutoModeAsync(true);
        }

        private async void BtnManual_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Mode_Auto\nmode manuel";
            await Modbus.SetAutoModeAsync(false);
        }

        private async void BtnInit_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Init\ndemande initialisation";
            await Modbus.InitAsync();
        }

        private async void BtnStartCycle_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Start\ndemande départ cycle";
            await Modbus.StartCycleAsync();
        }

        private async void BtnStopCycle_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Stop\ndemande arrêt cycle";
            await Modbus.StopCycleAsync();
        }

        private async void BtnAcquitDef_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Acquit\ndemande acquittement défaut";
            await Modbus.AcquitDefaultAsync();
        }

        private async void StartCollect_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.StartCollect\nstart collect";
            await Modbus.StartCollectAsync();
        }
        private async void StopCollect_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.StopCollect\nstop collect";
            await Modbus.StopCollectAsync();
        }

        private async void BtnHardReset_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.HardReset\nhard reset";
            await Modbus.HardResetAsync();
        }

        private async void ValidateFiole_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FioleInput.Text, out int NewFioleNumber))
            {
                // int CurrentFioleNumber = await Modbus.ReadHoldingRegisterAsync(105);
                TxtStatus.Text = $"Numéro de fiole demandé : {NewFioleNumber}";
                await Modbus.SetVialNbAsync(NewFioleNumber);
                // TxtFiole.Text = $"Old: {CurrentFioleNumber}, New: {NewFioleNumber}";
                // MessageBox.Show($"Valeur validée : {NewFioleNumber}", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
