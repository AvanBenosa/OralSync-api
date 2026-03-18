using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;

using ClinicCommands = DMD.APPLICATION.ClinicProfiles.Commands;

namespace DMD.API.Controllers.Clinic
{
    [Route("api/dmd/clinic")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ClinicController : BaseController
    {
        [HttpPost("create-clinic")]
        [Description("Create clinic profile")]
        [ProducesResponseType(typeof(ClinicProfileModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateClinic([FromBody] ClinicCommands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<ClinicProfileModel>)result).Data;
            return Created("", data);
        }
    }
}
