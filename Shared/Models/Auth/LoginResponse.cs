using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Enums;

namespace Shared.Models.Auth;

public class LoginResponse
{
    public Guid Id { get; set; }
    public Guid? ShopId { get; set; }
    public string? Email { get; set; }
    public string? Token { get; set; }
    public UserRole Role { get; set; }
    public bool? IsVerified { get; set; }

    public string? Message { get; set;  }
}
