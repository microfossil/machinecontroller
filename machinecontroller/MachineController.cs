using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class MachineController
{
    private readonly ModbusController controller;

    public MachineController(ModbusController controller)
    {
        this.controller = controller;
    }

    // Automatic commands (WORD0 - Commands to machine)
    public async Task InitializeMachine()
    {
        // Set Init (WORD0.4)
        var commandWord = controller.ReadRegisters(0, 1)[0];
        commandWord |= (1 << 4); // Init = TRUE
        controller.WriteRegister(0, (ushort)commandWord);
        await Task.Delay(100);
    }

    public async Task StartMainCycle(int vialNumber)
    {
        // Start main automatic cycle (GEMMA F1 mode) for specified vial
        SetVialNumber(vialNumber);

        var commandWord = controller.ReadRegisters(0, 1)[0];
        commandWord |= (1 << 6); // Cde_Auto.Collect_Start (WORD0.6) - starts main cycle
        controller.WriteRegister(0, (ushort)commandWord);
        await Task.Delay(500); // Documentation mentions ~500ms pulse
    }

    public async Task StopMainCycle()
    {
        // Stop main cycle for current vial (triggers drain cycle)
        var commandWord = controller.ReadRegisters(0, 1)[0];
        commandWord |= (1 << 7); // Cde_Auto.Collect_Stop (WORD0.7)
        controller.WriteRegister(0, (ushort)commandWord);
        await Task.Delay(500); // Documentation mentions ~500ms pulse
    }

    public async Task EnableEmptySlotControl(bool enable)
    {
        // Enable/disable camera verification of empty slots after pickup/deposit
        var commandWord = controller.ReadRegisters(0, 1)[0];

        if (enable)
            commandWord |= (1 << 5); // Cde_Auto.Avec_controle_vide (WORD0.5)
        else
            commandWord &= ~(1 << 5);

        controller.WriteRegister(0, (ushort)commandWord);
        await Task.Delay(100);
    }

    // Read machine status (from machine to server)
    public async Task<int> GetCurrentMode()
    {
        // ActualMode is at WORD1 in the machine->server mapping
        // But we need to account for the offset in the actual Modbus addressing
        var values = controller.ReadRegisters(100, 1); // Assuming machine status starts at address 100
        return values[0]; // GEMMA mode
    }

    public async Task<int> GetMainCycleStep()
    {
        // G7_Main_ActiveStep is at WORD10 in machine->server mapping
        var values = controller.ReadRegisters(110, 1); // Assuming machine status starts at address 100
        return values[0];
    }

    public async Task<ProcessStates> ReadProcessStates()
    {
        // Process states are at WORD0 in machine->server mapping
        var values = controller.ReadRegisters(100, 1); // First word of machine status

        return new ProcessStates
        {
            VisionAnalysisARequested = (values[0] & (1 << 0)) != 0,
            VisionAnalysisBRequested = (values[0] & (1 << 1)) != 0,
            EmptyControlARequested = (values[0] & (1 << 2)) != 0,
            EmptyControlBRequested = (values[0] & (1 << 3)) != 0
        };
    }

    public async Task<AlarmStates> ReadAlarms()
    {
        // Alarms start at WORD70 in machine->server mapping
        var values = controller.ReadRegisters(170, 5); // Read 5 words of alarms

        return new AlarmStates
        {
            EmergencyStop = (values[0] & (1 << 0)) != 0,    // Alarme_1100
            LowPneumatic = (values[0] & (1 << 1)) != 0,     // Alarme_1101
            MachineNotRearmed = (values[0] & (1 << 2)) != 0, // Alarme_1102
            Power24VFault = (values[0] & (1 << 3)) != 0,    // Alarme_1103
            IndexerDriveFault = (values[0] & (1 << 4)) != 0, // Alarme_1104
            // Add more alarms as needed...
        };
    }

    public async Task<VialStates> ReadVialStates()
    {
        // Vial registers start at WORD77 in machine->server mapping
        var values = controller.ReadRegisters(177, 8); // Read 8 words (WORD77-WORD84)

        var vialStates = new VialStates();

        // Process vials 1-30 (WORD77-WORD84)
        for (int vialNumber = 1; vialNumber <= 30; vialNumber++)
        {
            int wordIndex = (vialNumber - 1) / 4; // 4 vials per word
            int bitOffset = ((vialNumber - 1) % 4) * 4; // 4 bits per vial

            var vialState = new VialState
            {
                VialNumber = vialNumber,
                IsPresent = (values[wordIndex] & (1 << (bitOffset + 0))) != 0,
                ProcessCompleted = (values[wordIndex] & (1 << (bitOffset + 1))) != 0,
                CollectCompleted = (values[wordIndex] & (1 << (bitOffset + 2))) != 0,
                ProcessAborted = (values[wordIndex] & (1 << (bitOffset + 3))) != 0
            };

            vialStates.Vials.Add(vialState);
        }

        // Special vials from WORD84
        vialStates.RejectVial = new SpecialVialState
        {
            IsPresent = (values[7] & (1 << 8)) != 0,
            NeedsVerification = (values[7] & (1 << 9)) != 0
        };

        vialStates.AnalyzerVial = new SpecialVialState
        {
            IsPresent = (values[7] & (1 << 10)) != 0,
            NeedsVerification = (values[7] & (1 << 11)) != 0
        };

        vialStates.ManipulatorVial = new SpecialVialState
        {
            IsPresent = (values[7] & (1 << 12)) != 0,
            NeedsVerification = false // Not specified in register map
        };

        return vialStates;
    }

    public async Task<ParticleStates> ReadParticleStates()
    {
        // Particle registers at WORD85 in machine->server mapping
        var values = controller.ReadRegisters(185, 1); // Read 1 word (WORD85)

        return new ParticleStates
        {
            AnalyzerVibratorA = new ParticleState
            {
                IsPresent = (values[0] & (1 << 0)) != 0,
                NeedsVerification = (values[0] & (1 << 2)) != 0
            },
            AnalyzerVibratorB = new ParticleState
            {
                IsPresent = (values[0] & (1 << 1)) != 0,
                NeedsVerification = (values[0] & (1 << 3)) != 0
            },
            AnalyzerCameraA = new ParticleState
            {
                IsPresent = (values[0] & (1 << 4)) != 0,
                NeedsVerification = (values[0] & (1 << 6)) != 0
            },
            AnalyzerCameraB = new ParticleState
            {
                IsPresent = (values[0] & (1 << 5)) != 0,
                NeedsVerification = (values[0] & (1 << 7)) != 0
            },
            AnalyzerManipA = new ParticleState
            {
                IsPresent = (values[0] & (1 << 8)) != 0,
                NeedsVerification = (values[0] & (1 << 10)) != 0
            },
            AnalyzerManipB = new ParticleState
            {
                IsPresent = (values[0] & (1 << 9)) != 0,
                NeedsVerification = (values[0] & (1 << 11)) != 0
            },
            ManipulatorParticle = new ParticleState
            {
                IsPresent = (values[0] & (1 << 12)) != 0,
                NeedsVerification = false // Not specified in register map
            }
        };
    }

    public async Task<VialState> GetVialState(int vialNumber)
    {
        if (vialNumber < 1 || vialNumber > 30)
            throw new ArgumentOutOfRangeException(nameof(vialNumber), "Vial number must be between 1 and 30");

        var allVials = await ReadVialStates();
        return allVials.Vials.FirstOrDefault(v => v.VialNumber == vialNumber);
    }

    // Cycle monitoring methods (based on documentation)
    public async Task<bool> IsInitializationRequired()
    {
        var alarms = await ReadAlarms();
        var mode = await GetCurrentMode();

        // Initialization required after major failure or when in A6 mode
        return alarms.GetAlarmLevel() == AlarmLevel.Major || mode == 6;
    }

    public async Task<bool> IsConveyorEmpty()
    {
        // Check if conveyor is empty based on particle presence
        var particleStates = await ReadParticleStates();
        return !particleStates.AnalyzerVibratorA.IsPresent &&
               !particleStates.AnalyzerVibratorB.IsPresent &&
               !particleStates.ManipulatorParticle.IsPresent;
    }

    public async Task<CollectionStatus> GetCollectionStatus(int vialNumber)
    {
        var vialState = await GetVialState(vialNumber);
        var isConveyorEmpty = await IsConveyorEmpty();

        if (vialState == null)
            return new CollectionStatus { Status = "Vial not found", IsActive = false };

        if (vialState.ProcessCompleted)
            return new CollectionStatus { Status = "Process completed (conveyor emptied naturally)", IsActive = false };

        if (vialState.CollectCompleted)
            return new CollectionStatus { Status = "Collection completed (stopped by operator)", IsActive = false };

        if (vialState.ProcessAborted)
            return new CollectionStatus { Status = "Process aborted (fault during startup)", IsActive = false };

        if (vialState.IsPresent)
        {
            if (isConveyorEmpty)
                return new CollectionStatus { Status = "Collection finishing (conveyor empty)", IsActive = true };
            else
                return new CollectionStatus { Status = "Collection in progress", IsActive = true };
        }

        return new CollectionStatus { Status = "Ready for collection", IsActive = false };
    }
    // Parameter setting methods (Server -> Machine WORD6-WORD31)
    public void SetDestinationA(int plateau, int x, int y, int z)
    {
        // Parameters start at WORD6
        controller.WriteRegister(6, (ushort)plateau);  // Param.Destination_A_Plateau
        controller.WriteRegister(7, (ushort)x);        // Param.Destination_A_X
        controller.WriteRegister(8, (ushort)y);        // Param.Destination_A_Y
        controller.WriteRegister(9, (ushort)z);        // Param.Destination_A_Z
    }

    public void SetDestinationB(int plateau, int x, int y, int z)
    {
        controller.WriteRegister(10, (ushort)plateau); // Param.Destination_B_Plateau
        controller.WriteRegister(11, (ushort)x);       // Param.Destination_B_X
        controller.WriteRegister(12, (ushort)y);       // Param.Destination_B_Y
        controller.WriteRegister(13, (ushort)z);       // Param.Destination_B_Z
    }

    public void SetVibrationParameters(int bowlPercent, int rail1Percent, int rail2Percent)
    {
        // Validate percentages (0-100%)
        if (bowlPercent < 0 || bowlPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(bowlPercent), "Must be between 0 and 100");
        if (rail1Percent < 0 || rail1Percent > 100)
            throw new ArgumentOutOfRangeException(nameof(rail1Percent), "Must be between 0 and 100");
        if (rail2Percent < 0 || rail2Percent > 100)
            throw new ArgumentOutOfRangeException(nameof(rail2Percent), "Must be between 0 and 100");

        controller.WriteRegister(17, (ushort)bowlPercent);  // Param.Vibration_Bol
        controller.WriteRegister(18, (ushort)rail1Percent); // Param.Vibration_rail_1
        controller.WriteRegister(19, (ushort)rail2Percent); // Param.Vibration_rail_2
    }

    public void SetVialNumber(int vialNumber)
    {
        if (vialNumber < 1 || vialNumber > 30)
            throw new ArgumentOutOfRangeException(nameof(vialNumber), "Vial number must be between 1 and 30");

        controller.WriteRegister(15, (ushort)vialNumber); // Param.N_Fiole
    }

    public void SetVisionZPosition(int positionMillimeters)
    {
        controller.WriteRegister(16, (ushort)positionMillimeters); // Param.Axe_Vision_Z_position
    }
}

public class ProcessStates
{
    public bool VisionAnalysisARequested { get; set; }
    public bool VisionAnalysisBRequested { get; set; }
    public bool EmptyControlARequested { get; set; }
    public bool EmptyControlBRequested { get; set; }
}

public class AlarmStates
{
    public bool EmergencyStop { get; set; }
    public bool LowPneumatic { get; set; }
    public bool MachineNotRearmed { get; set; }
    public bool Power24VFault { get; set; }
    public bool IndexerDriveFault { get; set; }
}

public class VialStates
{
    public List<VialState> Vials { get; set; } = new List<VialState>();
    public SpecialVialState RejectVial { get; set; }
    public SpecialVialState AnalyzerVial { get; set; }
    public SpecialVialState ManipulatorVial { get; set; }
}

public class VialState
{
    public int VialNumber { get; set; }
    public bool IsPresent { get; set; }
    public bool ProcessCompleted { get; set; }
    public bool CollectCompleted { get; set; }
    public bool ProcessAborted { get; set; }

    public VialStatus Status
    {
        get
        {
            if (!IsPresent) return VialStatus.Absent;
            if (ProcessAborted) return VialStatus.Aborted;
            if (ProcessCompleted) return VialStatus.ProcessCompleted;
            if (CollectCompleted) return VialStatus.CollectCompleted;
            return VialStatus.Processing;
        }
    }
}

public class SpecialVialState
{
    public bool IsPresent { get; set; }
    public bool NeedsVerification { get; set; }
}

public enum VialStatus
{
    Absent,
    Processing,
    ProcessCompleted,
    CollectCompleted,
    Aborted
}

public class ParticleStates
{
    public ParticleState AnalyzerVibratorA { get; set; }
    public ParticleState AnalyzerVibratorB { get; set; }
    public ParticleState AnalyzerCameraA { get; set; }
    public ParticleState AnalyzerCameraB { get; set; }
    public ParticleState AnalyzerManipA { get; set; }
    public ParticleState AnalyzerManipB { get; set; }
    public ParticleState ManipulatorParticle { get; set; }
}

public class ParticleState
{
    public bool IsPresent { get; set; }
    public bool NeedsVerification { get; set; }
}

public class CollectionStatus
{
    public string Status { get; set; }
    public bool IsActive { get; set; }
}

// Enums for parameter values
public enum DestinationPlateau
{
    Reject = 0,
    LeftPlatform = 1,
    RightPlatform = 2,
    Analyzer = 3
}

public enum AlarmLevel
{
    None = 0,
    Minor = 1,      // Minor faults - cycle stop, ack + restart sufficient
    Major = 2       // Major faults - cycle stop, motors stopped, initialization required
}

public static class MachineParameterExtensions
{
    /// <summary>
    /// Helper method to set destination using enum for better readability
    /// </summary>
    public static void SetDestinationA(this MachineController controller, DestinationPlateau plateau, int x = 0, int y = 0, int z = 0)
    {
        controller.SetDestinationA((int)plateau, x, y, z);
    }

    /// <summary>
    /// Helper method to set destination using enum for better readability
    /// </summary>
    public static void SetDestinationB(this MachineController controller, DestinationPlateau plateau, int x = 0, int y = 0, int z = 0)
    {
        controller.SetDestinationB((int)plateau, x, y, z);
    }

    /// <summary>
    /// Get human-readable description of GEMMA mode
    /// </summary>
    public static string GetGemmaModeDescription(this MachineController controller, int mode)
    {
        return mode switch
        {
            1 => "A1 - Stopped in Initial State / F1 - Normal Production",
            2 => "A2 - Stop Requested at End of Cycle",
            4 => "F4 - Manual Mode",
            6 => "A6 - Initialization",
            _ => $"Unknown Mode: {mode}"
        };
    }

    /// <summary>
    /// Determine alarm level based on alarm type (according to documentation)
    /// </summary>
    public static AlarmLevel GetAlarmLevel(this AlarmStates alarms)
    {
        // Major failures (require initialization)
        if (alarms.EmergencyStop || alarms.MachineNotRearmed ||
            alarms.Power24VFault || alarms.IndexerDriveFault)
            return AlarmLevel.Major;

        // Minor failures (require acknowledgment + restart)
        if (alarms.LowPneumatic)
            return AlarmLevel.Minor;

        return AlarmLevel.None;
    }
}
