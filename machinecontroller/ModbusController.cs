using EasyModbus;
using System.Threading.Tasks;

public class ModbusController
{
    private ModbusClient modbusClient;

    public bool IsConnected => modbusClient?.Connected ?? false;

    public ModbusController(string ip, int port)
    {
        modbusClient = new ModbusClient(ip, port);
    }

    public async Task ConnectAsync()
    {
        await Task.Run(() => modbusClient.Connect());
    }

    public void Disconnect()
    {
        modbusClient.Disconnect();
    }

    public void WriteRegister(int address, ushort value)
    {
        modbusClient.WriteSingleRegister(address, value);
    }

    public int[] ReadRegisters(int startAddress, int count)
    {
        return modbusClient.ReadHoldingRegisters(startAddress, count);
    }

    public ushort ReadSingleRegister(int address)
    {
        var result = modbusClient.ReadHoldingRegisters(address, 1);
        return (ushort)result[0];
    }

    public void WriteMultipleRegisters(int startAddress, ushort[] values)
    {
        modbusClient.WriteMultipleRegisters(startAddress, values);
    }
}