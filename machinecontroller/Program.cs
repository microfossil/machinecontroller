using System;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        var controller = new ModbusController("192.168.0.1", 502);
        await controller.ConnectAsync();

        // Create machine control interface
        var machineController = new MachineController(controller);

        // Configure machine parameters before starting
        machineController.SetVialNumber(5); // Work with vial 5
        machineController.SetDestinationA(DestinationPlateau.Analyzer, 0, 0, 0); // Send particle A to analyzer
        machineController.SetDestinationB(DestinationPlateau.LeftPlatform, 1000, 2000, 500); // Send particle B to left platform at specific coordinates
        machineController.SetVibrationParameters(75, 80, 85); // Set vibration percentages
        machineController.SetVisionZPosition(1500); // Set vision Z axis position
        await machineController.EnableEmptySlotControl(true); // Enable camera verification

        // Check if initialization is required
        if (await machineController.IsInitializationRequired())
        {
            Console.WriteLine("Initialization required, starting initialization...");
            await machineController.InitializeMachine();
            // Wait for initialization to complete (in real scenario, monitor cycle states)
            await Task.Delay(2000);
        }

        // Start main cycle for vial 5 (GEMMA F1 production mode)
        await machineController.StartMainCycle(5);

        // Read machine status
        var currentMode = await machineController.GetCurrentMode();
        var mainStep = await machineController.GetMainCycleStep();
        Console.WriteLine($"Machine Mode: {currentMode} ({machineController.GetGemmaModeDescription(currentMode)}), Main Cycle Step: {mainStep}");

        // Check collection status
        var collectionStatus = await machineController.GetCollectionStatus(5);
        Console.WriteLine($"Collection Status: {collectionStatus.Status}, Active: {collectionStatus.IsActive}");

        // Read some process states
        var processStates = await machineController.ReadProcessStates();
        Console.WriteLine($"Vision A analysis requested: {processStates.VisionAnalysisARequested}");

        // Check alarms and their severity
        var alarms = await machineController.ReadAlarms();
        var alarmLevel = alarms.GetAlarmLevel();
        Console.WriteLine($"Alarm Level: {alarmLevel}");
        if (alarms.EmergencyStop) Console.WriteLine("⚠️ Emergency Stop Active!");
        if (alarms.LowPneumatic) Console.WriteLine("⚠️ Low Pneumatic Pressure!");

        // Read vial states
        var vialStates = await machineController.ReadVialStates();
        Console.WriteLine($"Total vials present: {vialStates.Vials.Count(v => v.IsPresent)}");
        Console.WriteLine($"Completed processes: {vialStates.Vials.Count(v => v.ProcessCompleted)}");

        // Check specific vial
        var vial5 = await machineController.GetVialState(5);
        Console.WriteLine($"Vial 5 status: {vial5?.Status}");

        // Read particle states
        var particleStates = await machineController.ReadParticleStates();
        Console.WriteLine($"Analyzer Camera A has particle: {particleStates.AnalyzerCameraA.IsPresent}");
        Console.WriteLine($"Manipulator has particle: {particleStates.ManipulatorParticle.IsPresent}");
        Console.WriteLine($"Conveyor empty: {await machineController.IsConveyorEmpty()}");

        // Stop the main cycle (triggers drain if needed)
        await machineController.StopMainCycle();

        controller.Disconnect();
    }
}