using ICQ.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace ICQ.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SmileysController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<SmileyDto>> GetAll() => Ok(SmileyPack.List());
}
