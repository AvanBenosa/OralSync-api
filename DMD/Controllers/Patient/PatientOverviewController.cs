using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model;
using DMD.APPLICATION.PatientsModule.PatientOverview.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Commands = DMD.APPLICATION.PatientsModule.PatientOverview.Commands;
using Queries = DMD.APPLICATION.PatientsModule.PatientOverview.Queries;
namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient-overview")]
    public class PatientOverviewController : BaseController
    {
        [HttpGet("get-patient-overview")]
        [Description("Query return Patient OverView model")]
        [ProducesResponseType(typeof(List<PatientOverviewModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientOverview([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<List<PatientOverviewModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-patient-overview")]
        [Description("Create Patient OverView based on json body")]
        [ProducesResponseType(typeof(PatientOverviewModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatientOverview([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);
            var data = ((SuccessResponse<PatientOverviewModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-patient-overview")]
        [Description("Update Patient OverView based on param ID and json data")]
        [ProducesResponseType(typeof(PatientOverviewModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatientOverview([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientOverviewModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-patient-overview")]
        [Description("Delete Patient OverView, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatientOverview([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
