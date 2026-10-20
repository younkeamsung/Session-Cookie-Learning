using Microsoft.AspNetCore.Mvc;

namespace MyAuth.Mvc.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                message = "MyAuth.Mvc API 연결 성공"
            });
        }
    }
}
