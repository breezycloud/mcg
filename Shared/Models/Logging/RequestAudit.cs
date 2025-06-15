using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models.Logging;

public class RequestAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();    
    public string? User { get; set; }
    public string? Method { get; set; }
    public string? Action { get; set; }
    public string? Path { get; set; }
    public DateTime CreatedAt { get; set; }    
}
