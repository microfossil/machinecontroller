using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading.Tasks;
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

                TxtDest_P_A.Text = $"{Modbus.Dest_P_A}";
                TxtDest_X_A.Text = $"{Modbus.Dest_X_A}µm";
                TxtDest_Y_A.Text = $"{Modbus.Dest_Y_A}µm";
                TxtDest_Z_A.Text = $"{Modbus.Dest_Z_A}µm";
                TxtDest_P_B.Text = $"{Modbus.Dest_P_B}";
                TxtDest_X_B.Text = $"{Modbus.Dest_X_B}µm";
                TxtDest_Y_B.Text = $"{Modbus.Dest_Y_B}µm";
                TxtDest_Z_B.Text = $"{Modbus.Dest_Z_B}µm";

                TxtWord90.Text = Modbus.TxtWord90;
                TxtStepCyclePrincipal.Text = $"{Modbus.StepCyclePrincipal}";

                LedRequestAnalyseVisionA.Fill = Modbus.RequestAnalyseVisionA ? Brushes.LimeGreen : Brushes.Red;
                LedRequestAnalyseVisionB.Fill = Modbus.RequestAnalyseVisionB ? Brushes.LimeGreen : Brushes.Red;
                LedRequestControlVoidA.Fill = Modbus.RequestControlVoidA ? Brushes.LimeGreen : Brushes.Red;
                LedRequestControlVoidB.Fill = Modbus.RequestControlVoidB ? Brushes.LimeGreen : Brushes.Red;

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

                // Activate Btn AnalyseVisionADone when RequestAnalyseVisionA is true
                if (Modbus.RequestAnalyseVisionA)
                {
                    BtnAnalyseVisionADone.IsEnabled = true;
                }
                else
                {
                    BtnAnalyseVisionADone.IsEnabled = false;
                }

                // Activate Btn AnalyseVisionBDone when RequestAnalyseVisionB is true
                if (Modbus.RequestAnalyseVisionB)
                {
                    BtnAnalyseVisionBDone.IsEnabled = true;
                }
                else
                {
                    BtnAnalyseVisionBDone.IsEnabled = false;
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

        private async void AnalyseVisionADone_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.AnalyseVisionADone\nAnalyse Vision A Done";
            await Modbus.AnalyseVisionADoneAsync();
        }

        private async void AnalyseVisionBDone_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.AnalyseVisionBDone\nAnalyse Vision B Done";
            await Modbus.AnalyseVisionBDoneAsync();
        }

        private async void ValidateFiole_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FioleInput.Text, out int NewFioleNumber))
            {
                // int CurrentFioleNumber = await Modbus.ReadHoldingRegisterAsync(105);
                TxtStatus.Text = $"Vial nb requested : {NewFioleNumber}";
                await Modbus.SetVialNbAsync(NewFioleNumber);
                // TxtFiole.Text = $"Old: {CurrentFioleNumber}, New: {NewFioleNumber}";
                // MessageBox.Show($"Valeur validée : {NewFioleNumber}", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnSendCoord_Click(object sender, RoutedEventArgs e)
        {
            int Plateau = int.Parse((CmbPlateau.SelectedItem as ComboBoxItem).Content.ToString());
            int SlideX = int.Parse((CmbSlideX.SelectedItem as ComboBoxItem).Content.ToString());
            int SlideY = int.Parse((CmbSlideY.SelectedItem as ComboBoxItem).Content.ToString());
            int CavityX = int.Parse((CmbCavityX.SelectedItem as ComboBoxItem).Content.ToString());
            int CavityY = int.Parse((CmbCavityY.SelectedItem as ComboBoxItem).Content.ToString());

            TxtStatus.Text = $"Selected values:\nPlateau : {Plateau},\nSlide X : {SlideX}, Slide Y : {SlideY},\nCavity X : {CavityX}, Cavity Y : {CavityY}";

            await Modbus.SendCoordinatesAsync(Plateau, SlideX, SlideY, CavityX, CavityY);
        }
    }
}
