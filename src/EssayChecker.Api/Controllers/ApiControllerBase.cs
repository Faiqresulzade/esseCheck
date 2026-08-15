using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace EssayChecker.Api.Controllers;

/// <summary>Autentifikasiya (JWT) tələb edən controller-lər üçün ortaq baza.</summary>
public abstract class ApiControllerBase : ControllerBase
{
    protected int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
