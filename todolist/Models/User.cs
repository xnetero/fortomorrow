using System;
using System.Collections.Generic;

namespace todolist.Models;

public partial class User
{
    public Guid Iduser { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public bool? Active { get; set; }

    public string? Password { get; set; }

    public string? Token { get; set; }

    public string? Resetpasswordtoken { get; set; }

    public DateTime? Resetpasswordexpiry { get; set; }
}
