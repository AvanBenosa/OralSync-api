using DMD.APPLICATION.AdminPortal.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using AdminQueries = DMD.APPLICATION.AdminPortal.Queries;
using AdminCommands = DMD.APPLICATION.AdminPortal.Commands;

namespace DMD.API.Controllers.Admin
{
    [Route("api/dmd/admin")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class AdminController : BaseController
    {
        [HttpGet("dashboard-summary")]
        [Description("Get admin portal dashboard summary")]
        [ProducesResponseType(typeof(AdminDashboardModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetDashboardSummary()
        {
            var result = await Mediator.Send(new AdminQueries.GetDashboard.Query());
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<AdminDashboardModel>)result).Data;
            return Ok(data);
        }

        [HttpGet("get-clinics")]
        [Description("Get admin portal clinics")]
        [ProducesResponseType(typeof(List<AdminClinicModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetClinics()
        {
            var result = await Mediator.Send(new AdminQueries.GetClinics.Query());
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<List<AdminClinicModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("set-clinic-lock")]
        [Description("Update clinic lock status")]
        [ProducesResponseType(typeof(AdminClinicModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetClinicLock([FromBody] AdminCommands.UpdateClinicLock.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<AdminClinicModel>)result).Data;
            return Ok(data);
        }

        [HttpGet("get-clinic-subscription-histories")]
        [Description("Get clinic subscription history records")]
        [ProducesResponseType(typeof(List<AdminClinicSubscriptionHistoryModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetClinicSubscriptionHistories([FromQuery] AdminQueries.GetClinicSubscriptionHistories.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<List<AdminClinicSubscriptionHistoryModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-clinic-subscription-history")]
        [Description("Create clinic subscription history record")]
        [ProducesResponseType(typeof(AdminClinicSubscriptionHistoryModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreateClinicSubscriptionHistory([FromBody] AdminCommands.CreateClinicSubscriptionHistory.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<AdminClinicSubscriptionHistoryModel>)result).Data;
            return Created(string.Empty, data);
        }

        [HttpPut("put-clinic-subscription-history")]
        [Description("Update clinic subscription history record")]
        [ProducesResponseType(typeof(AdminClinicSubscriptionHistoryModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdateClinicSubscriptionHistory([FromBody] AdminCommands.UpdateClinicSubscriptionHistory.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<AdminClinicSubscriptionHistoryModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-clinic-subscription-history")]
        [Description("Delete clinic subscription history record")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteClinicSubscriptionHistory([FromBody] AdminCommands.DeleteClinicSubscriptionHistory.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
