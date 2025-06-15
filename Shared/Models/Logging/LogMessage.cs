using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models.Logging;

public class LogMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string? Source { get; set; } = "Client";
    public string? Message { get; set; }        
    public DateTime CreatedAt { get; set; }
}
