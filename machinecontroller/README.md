
## Description
This is a Modbus-based machine controller in C# designed for a particle handling and analysis system.
The controller interfaces with a machine that uses GEMMA (Guide d'Étude des Modes de Marche et d'Arrêt) state machine methodology with multiple Grafcet cycles.

## Features
- **Machine Control**: Start/stop cycles, initialization, fault acknowledgment
- **Process Monitoring**: Read vision analysis requests, cycle states, and machine mode
- **Parameter Setting**: Configure destinations, vibration parameters, and operational settings
- **Alarm Monitoring**: Real-time alarm status including emergency stops, drive faults, etc.
- **Multi-Cycle Management**: Monitor multiple simultaneous Grafcet cycles (main, initialization, manipulator operations, etc.)

## Key Components
- **ModbusController**: Low-level Modbus communication
- **MachineController**: High-level machine interface with proper register mapping
- **State Classes**: Structured data for process states, cycle states, and alarms

## Register Mapping
The system uses a comprehensive register mapping with:
- **Commands (Server→Machine)**: WORD0-WORD5 for automatic and manual commands
- **Parameters (Server→Machine)**: WORD6-WORD31 for operational parameters
- **Status (Machine→Server)**: WORD0-WORD69+ for process states, cycle steps, and alarms

## How to Extend
- Add more specific alarm handling in `AlarmStates`
- Extend `CycleStates` to monitor additional Grafcet cycles
- Add vision system integration methods
- Implement parameter validation and error handling
- Add logging and diagnostic features

This structure provides a solid foundation for industrial automation control with proper separation of concerns and extensibility.Here’s a minimal, clean base for a Modbus-based Grafcet controller in C#.
This example is focused on:

- Connecting to a Modbus device
- Reading/writing registers
- Launching a basic cycle (Grafcet step)
- Minimal class structure, easy to extend

## How to Extend
Add more Register objects for other variables you want to read/write.
Add more methods to Sequence for more complex Grafcet logic.
Add error handling, logging, or UI as needed.
This structure is minimal and easy to adapt for a new machine or Grafcet version.
Let me know if you want to add specific features or need further customization!