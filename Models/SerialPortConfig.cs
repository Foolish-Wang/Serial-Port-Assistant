using System.IO.Ports;
using System;
namespace Serial_Port_Assistant.Models;

public class SerialPortConfig
{
    public string   PortName { get; set; } = "COM1";
    public int      BaudRate { get; set; } = 9600;
    public int      DataBits { get; set; } = 8;
    public Parity   Parity   { get; set; } = Parity.None;
    public StopBits StopBits { get; set; } = StopBits.One;
}