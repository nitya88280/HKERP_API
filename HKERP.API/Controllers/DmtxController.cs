using HKERP.API.Filters;
using HKERP.Application.DTOs;
using HKERP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HKERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DmtxController : ControllerBase
    {
        private readonly IGenericSpService _spService;

        public DmtxController(IGenericSpService spService)
        {
            _spService = spService;
        }

        [HttpGet("prc-dmtx-inv")]
        [ApiAccessControl("PRC_DMTX_INV_API")]
        public IActionResult GetPrcDmtxInv()
        {
            try
            {
                var result = _spService.ExecuteSp("RPT_PRC_DMTX_INV_API");
                return Ok(new ApiResponseDto { Success = true, Message = "Success", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { Success = false, Message = "Error: " + ex.Message });
            }
        }
    }
}
