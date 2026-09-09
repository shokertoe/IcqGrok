using ICQ.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

/// <summary>
/// Классические ICQ-смайлики (*HEADBANG*, *BANG* и др.) для клиентов.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SmileysController : ControllerBase
{
    /// <summary>Полный список смайликов (код → эмодзи/описание).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SmileyDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<SmileyDto>> GetAll() => Ok(SmileyPack.List());
}
