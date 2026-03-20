using DMD.APPLICATION.PatientsModule.PatientForms.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Commands = DMD.APPLICATION.PatientsModule.PatientForms.Commands;
using Queries = DMD.APPLICATION.PatientsModule.PatientForms.Queries;

namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient-form")]
    public class PatientFormController : BaseController
    {
        [HttpGet("get-patient-form")]
        [Description("Query return Patient Form model")]
        [ProducesResponseType(typeof(List<PatientFormModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientForm([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<List<PatientFormModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-patient-form")]
        [Description("Create Patient Form based on json body")]
        [ProducesResponseType(typeof(PatientFormModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatientForm([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<PatientFormModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-patient-form")]
        [Description("Update Patient Form based on param ID and json data")]
        [ProducesResponseType(typeof(PatientFormModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatientForm([FromBody] Commands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<PatientFormModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-patient-form")]
        [Description("Delete Patient Form, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatientForm([FromBody] Commands.Delete.Command command)
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
