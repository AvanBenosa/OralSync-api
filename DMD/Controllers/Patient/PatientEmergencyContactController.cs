using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Queries;
using Commands = DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Commands;

namespace DMD.API.Controllers.Patient
{

    [Route("api/dmd/patient-emergencyContact")]
    public class PatientEmergencyContactController : BaseController
    {
        [HttpGet("get-emergency-contact")]
        [Description("Query return Patient Medical History model")]
        [ProducesResponseType(typeof(List<PatientEmergencyContactModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientEmergencyContact([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<List<PatientEmergencyContactModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-emergency-contact")]
        [Description("Create Patient based on json body")]
        [ProducesResponseType(typeof(PatientModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatientEmergencyContact([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);
            var data = ((SuccessResponse<PatientModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-emergency-contact")]
        [Description("Update Patient Info based on param ID and json data")]
        [ProducesResponseType(typeof(PatientModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatientEmergencyContact([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-emergency-contact")]
        [Description("Delete Patient Info, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatientEmergencyContact([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
