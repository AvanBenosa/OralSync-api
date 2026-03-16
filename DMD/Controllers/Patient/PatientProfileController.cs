using DMD.APPLICATION.PatientsModule.PatientProfile.Model;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.PatientsModule.PatientProfile.Queries;
using Commands = DMD.APPLICATION.PatientsModule.PatientProfile.Commands;

namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient-profile")]
    public class PatientProfileController : BaseController
    {

        [HttpGet("get-patient-profile")]
        [Description("Query return Patient Profile model")]
        [ProducesResponseType(typeof(PatientProfileModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientProfile([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientProfileModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-patient")]
        [Description("Delete Patient Info, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatient([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
