using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.PatientsModule.Patient.Queries;
using Commands = DMD.APPLICATION.PatientsModule.Patient.Commands;


namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient")]
    public class PatientController : BaseController
    {

        [HttpGet("get-patient")]
        [Description("Query return Patient info model")]
        [ProducesResponseType(typeof(PatientResponseModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatient([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientResponseModel>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-patient")]
        [Description("Create Patient based on json body")]
        [ProducesResponseType(typeof(PatientModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatient([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);
            var data = ((SuccessResponse<PatientModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-patient")]
        [Description("Update Patient Info based on param ID and json data")]
        [ProducesResponseType(typeof(PatientModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatient([FromBody] Commands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientModel>)result).Data;
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
