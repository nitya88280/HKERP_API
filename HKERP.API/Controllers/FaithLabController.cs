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
    public class FaithLabController : ControllerBase
    {
        private readonly IGenericSpService _spService;

        public FaithLabController(IGenericSpService spService)
        {
            _spService = spService;
        }

        [HttpPost("mnl-prc")]
        [ApiAccessControl("FaithLab_MnlPrc")]
        public IActionResult GetMnlPrc([FromBody] FaithLabMnlPrcRequestDto request)
        {
            // form aur to compulsory - validate karo
            if (string.IsNullOrWhiteSpace(request.From) || string.IsNullOrWhiteSpace(request.To))
            {
                return BadRequest(new ApiResponseDto
                {
                    Success = false,
                    Message = "'From' aur 'To' dono parameter compulsory hai"
                });
            }

            try
            {
                string xml = "<DocumentElement>" +
                                "<form>" + request.From + "</form>" +
                                "<to>" + request.To + "</to>" +
                                (string.IsNullOrWhiteSpace(request.UniqueId)
                                    ? ""
                                    : "<UniqueId>" + request.UniqueId + "</UniqueId>") +
                             "</DocumentElement>";

                var result = _spService.ExecuteSp("Get_FaithLab_MnlPrc_Send_Surat_API", xml);

                return Ok(new ApiResponseDto { Success = true, Message = "Success", Data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponseDto { Success = false, Message = "Error: " + ex.Message });
            }
        }
    }
}
