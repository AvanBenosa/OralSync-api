using DMD.APPLICATION.ClinicProfiles.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;

using ClinicCommands = DMD.APPLICATION.ClinicProfiles.Commands;
using ClinicModels = DMD.APPLICATION.ClinicProfiles.Models;
using ClinicQueries = DMD.APPLICATION.ClinicProfiles.Queries;

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

        [HttpGet("get-current-clinic-profile")]
        [Description("Get current clinic profile")]
        [ProducesResponseType(typeof(ClinicProfileModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCurrentClinicProfile([FromQuery] ClinicQueries.GetCurrent.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<ClinicProfileModel>)result).Data;
            return Ok(data);
        }

        [HttpPut("put-clinic-profile")]
        [Description("Update clinic profile")]
        [ProducesResponseType(typeof(ClinicProfileModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateClinicProfile([FromBody] ClinicCommands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<ClinicProfileModel>)result).Data;
            return Ok(data);
        }

        [HttpGet("data-privacy-status")]
        [Description("Get current clinic data privacy acceptance status")]
        [ProducesResponseType(typeof(ClinicModels.DataPrivacyStatusModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDataPrivacyStatus()
        {
            var result = await Mediator.Send(new ClinicQueries.GetDataPrivacyStatus.Query());
            if (result is NotFoundResponse)
            {
                return NotFound(result.Message);
            }

            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<ClinicModels.DataPrivacyStatusModel>)result).Data;
            return Ok(data);
        }

        [HttpPost("accept-data-privacy")]
        [Description("Accept data privacy for the current clinic")]
        [ProducesResponseType(typeof(ClinicModels.DataPrivacyStatusModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> AcceptDataPrivacy()
        {
            var result = await Mediator.Send(new ClinicCommands.AcceptDataPrivacy.Command());
            if (result is NotFoundResponse)
            {
                return NotFound(result.Message);
            }

            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<ClinicModels.DataPrivacyStatusModel>)result).Data;
            return Ok(data);
        }
    }
}
