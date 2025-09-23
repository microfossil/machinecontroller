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
                TxtFiole.Text = $"Vial n°{Modbus.FioleNumber}";
                TxtRepetitionNettoyage.Text = $"{Modbus.Repetition_Nettoyage}";
                TxtAxeVisionZPosition.Text = $"{(ushort)Modbus.Axe_Vision_Z_position}µm";
                TxtVibrationBol.Text = $"{Modbus.Vibration_Bol}%";
                TxtVibrationRail1.Text = $"{Modbus.Vibration_rail_1}%";
                TxtVibrationRail2.Text = $"{Modbus.Vibration_rail_2}%";
                TxtTempsAspiration.Text = $"{Modbus.Temps_aspiration}s";
                TxtTempsSoufflage.Text = $"{Modbus.Temps_soufflage}s";
                TxtNbMaxParticuleNonVue.Text = $"{Modbus.NbMaxParticule_Non_Vue}";
                TxtTempsVibrationConvoyageVide.Text = $"{Modbus.Temps_vibration_convoyage_vide}s";
                TxtTempsEspacementRail1.Text = $"{Modbus.Temps_espacement_rail_1}ms";
                TxtTempsEspacementRail2.Text = $"{Modbus.Temps_espacement_rail_2}ms";
                TxtVibrationAmorcageVidange.Text = $"{Modbus.Vibration_AmorcageVidange}%";
                TxtTempsAmorcage.Text = $"{Modbus.Temps_Amorcage}s";

                TxtDest_P_A.Text = $"{(ushort)Modbus.Dest_P_A}";
                TxtDest_X_A.Text = $"{(ushort)Modbus.Dest_X_A/100}mm"; //µm
                TxtDest_Y_A.Text = $"{(ushort)Modbus.Dest_Y_A/100}mm";
                TxtDest_Z_A.Text = $"{(ushort)Modbus.Dest_Z_A/100}mm";
                TxtDest_P_B.Text = $"{(ushort)Modbus.Dest_P_B}";
                TxtDest_X_B.Text = $"{(ushort)Modbus.Dest_X_B/100}mm";
                TxtDest_Y_B.Text = $"{(ushort)Modbus.Dest_Y_B/100}mm";
                TxtDest_Z_B.Text = $"{(ushort)Modbus.Dest_Z_B/100}mm";

                TxtWord90.Text = Modbus.TxtWord90;
                TxtStepCyclePrincipal.Text = $"{Modbus.StepCyclePrincipal}";

                LedRequestAnalyseVisionA.Fill = Modbus.RequestAnalyseVisionA ? Brushes.LimeGreen : Brushes.Red;
                LedRequestAnalyseVisionB.Fill = Modbus.RequestAnalyseVisionB ? Brushes.LimeGreen : Brushes.Red;

                LedVisionPresenceADone.Fill = Modbus.VisionPresenceADone ? Brushes.LimeGreen : Brushes.Red;
                LedVisionPresenceBDone.Fill = Modbus.VisionPresenceBDone ? Brushes.LimeGreen : Brushes.Red;

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

                // Activate BtnAnalyseVisionADone and BtnVisionPresenceADone when RequestAnalyseVisionA is true
                if (Modbus.RequestAnalyseVisionA)
                {
                    BtnAnalyseVisionADone.IsEnabled = true;
                    BtnVisionPresenceADone.IsEnabled = true;
                }
                else
                {
                    BtnAnalyseVisionADone.IsEnabled = false;
                    BtnVisionPresenceADone.IsEnabled = false;
                }

                // Activate BtnAnalyseVisionBDone and BtnVisionPresenceBDone when RequestAnalyseVisionB is true
                if (Modbus.RequestAnalyseVisionB)
                {
                    BtnAnalyseVisionBDone.IsEnabled = true;
                    BtnVisionPresenceBDone.IsEnabled = true;
                }
                else
                {
                    BtnAnalyseVisionBDone.IsEnabled = false;
                    BtnVisionPresenceBDone.IsEnabled = false;
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
            TxtStatus.Text = "Cde_Auto.Collect_Start\nstart collect";
            await Modbus.StartCollectAsync();
        }
        private async void StopCollect_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Collect_Stop\nstop collect";
            await Modbus.StopCollectAsync();
        }

        private async void StopVidange_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Vidange_Stop\nstop vidange";
            await Modbus.StopVidangeAsync();
        }

        private async void StartNettoyage_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Nettoyage_Start\nstart nettoyage";
            await Modbus.StartNettoyageAsync();
        }

        private async void BtnHardReset_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.HardReset\nhard reset";
            await Modbus.HardResetAsync();
        }

        private async void AnalyseVisionADone_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Vision_Analyse_A_Done\nAnalyse Vision A Done";
            await Modbus.AnalyseVisionADoneAsync();
        }

        private async void AnalyseVisionBDone_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Vision_Analyse_B_Done\nAnalyse Vision B Done";
            await Modbus.AnalyseVisionBDoneAsync();
        }

        private async void VisionPresenceADone_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Vision_Presence_A\nVision Presence A Done";
            await Modbus.VisionPresenceADoneAsync();
        }

        private async void VisionPresenceBDone_Click(object sender, RoutedEventArgs e)
        {
            TxtStatus.Text = "Cde_Auto.Vision_Presence_B\nVision Presence B Done";
            await Modbus.VisionPresenceBDoneAsync();
        }

        private async void ValidateFiole_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(FioleInput.Text, out int NewFioleNumber))
            {
                // int CurrentFioleNumber = await Modbus.ReadHoldingRegisterAsync(105);
                TxtStatus.Text = $"Vial nb requested: {NewFioleNumber}";
                await Modbus.SetVialNbAsync(NewFioleNumber);
                // TxtFiole.Text = $"Old: {CurrentFioleNumber}, New: {NewFioleNumber}";
                // MessageBox.Show($"Valeur validée : {NewFioleNumber}", "Confirmation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void BtnManSetValidate_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ManSetWord.Text, out int WordSelected))
            {
                int BitSelected = int.Parse((CmbManSetBit.SelectedItem as ComboBoxItem).Content.ToString());
                string MethodSelected = (CmbManSetMethod.SelectedItem as ComboBoxItem).Content.ToString();
                await Modbus.ManualSetBitAsync(WordSelected, BitSelected, MethodSelected);

            }
            else
            {
                MessageBox.Show("Please choose a valide integer", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateRepetitionNettoyage_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(RepetitionNettoyageInput.Text, out int repetitionNettoyage))
            {
                TxtStatus.Text = $"Repetition nettoyage requested: {repetitionNettoyage}";
                await Modbus.SetRepetitionNettoyageAsync(repetitionNettoyage);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour la répétition de nettoyage.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private async void ValidateAxeVisionZPosition_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(AxeVisionZPositionInput.Text, out int axeVisionZPosition))
            {
                TxtStatus.Text = $"Axe Vision Z Position requested: {axeVisionZPosition}µm";
                await Modbus.SetAxeVisionZPositionAsync(axeVisionZPosition);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour la position Axe Vision Z.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateVibrationBol_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(VibrationBolInput.Text, out int vibrationBol))
            {
                TxtStatus.Text = $"Vibration bol requested: {vibrationBol}%";
                await Modbus.SetVibrationBolAsync(vibrationBol);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour la vibration bol.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateVibrationRail1_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(VibrationRail1Input.Text, out int vibrationRail1))
            {
                TxtStatus.Text = $"Vibration rail 1 requested: {vibrationRail1}%";
                await Modbus.SetVibrationRail1Async(vibrationRail1);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour la vibration rail 1.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateVibrationRail2_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(VibrationRail2Input.Text, out int vibrationRail2))
            {
                TxtStatus.Text = $"Vibration rail 2 requested: {vibrationRail2}%";
                await Modbus.SetVibrationRail2Async(vibrationRail2);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour la vibration rail 2.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateTempsAspiration_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TempsAspirationInput.Text, out int tempsAspiration))
            {
                TxtStatus.Text = $"Temps aspiration requested: {tempsAspiration}s";
                await Modbus.SetTempsAspirationAsync(tempsAspiration);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le temps d'aspiration.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateTempsSoufflage_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TempsSoufflageInput.Text, out int tempsSoufflage))
            {
                TxtStatus.Text = $"Temps soufflage requested: {tempsSoufflage}s";
                await Modbus.SetTempsSoufflageAsync(tempsSoufflage);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le temps de soufflage.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateNbMaxParticuleNonVue_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(NbMaxParticuleNonVueInput.Text, out int nbMaxParticuleNonVue))
            {
                TxtStatus.Text = $"Nb max particule non vue requested: {nbMaxParticuleNonVue}";
                await Modbus.SetNbMaxParticuleNonVueAsync(nbMaxParticuleNonVue);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le nombre max de particules non vues.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateTempsVibrationConvoyageVide_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TempsVibrationConvoyageVideInput.Text, out int tempsVibrationConvoyageVide))
            {
                TxtStatus.Text = $"Temps vibration convoyage vide requested: {tempsVibrationConvoyageVide}s";
                await Modbus.SetTempsVibrationConvoyageVideAsync(tempsVibrationConvoyageVide);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le temps de vibration convoyage vide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateTempsEspacementRail1_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TempsEspacementRail1Input.Text, out int tempsEspacementRail1))
            {
                TxtStatus.Text = $"Temps espacement rail 1 requested: {tempsEspacementRail1}ms";
                await Modbus.SetTempsEspacementRail1Async(tempsEspacementRail1);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le temps d'espacement rail 1.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateTempsEspacementRail2_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TempsEspacementRail2Input.Text, out int tempsEspacementRail2))
            {
                TxtStatus.Text = $"Temps espacement rail 2 requested: {tempsEspacementRail2}ms";
                await Modbus.SetTempsEspacementRail2Async(tempsEspacementRail2);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le temps d'espacement rail 2.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void ValidateVibrationAmorcageVidange_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(VibrationAmorcageVidangeInput.Text, out int vibrationAmorcageVidange))
            {
                TxtStatus.Text = $"Vibration amorçage vidange requested: {vibrationAmorcageVidange}%";
                await Modbus.SetVibrationAmorcageVidangeAsync(vibrationAmorcageVidange);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour la vibration amorçage vidange.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        private async void ValidateTempsAmorcage_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TempsAmorcageInput.Text, out int tempsAmorcage))
            {
                TxtStatus.Text = $"Temps amorçage requested: {tempsAmorcage}s";
                await Modbus.SetTempsAmorcageAsync(tempsAmorcage);
            }
            else
            {
                MessageBox.Show("Veuillez entrer un entier valide pour le temps d'amorçage.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
