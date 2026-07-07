using Infraestructure.Identity.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Infraestructure.Identity.Authentication
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(AppUser user, string[] permissions);
    }
}
