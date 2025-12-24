using System.Net;

namespace _3DApi.Models;

public class Printer : Base
{
    public string Name { get; set; }
    
    public string Status { get; set; }
    
    public DateTimeOffset LastPing { get; set; }
    
    public IPAddress Ip { get; set; }
    
    public IEnumerable<PrinterMaterial> Materials { get; set; }
}