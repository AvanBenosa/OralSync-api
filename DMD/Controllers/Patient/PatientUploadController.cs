using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.PatientsModule.PatientEmergencyContact.Model;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Queries = DMD.APPLICATION.PatientsModule.PatientUploads.Queries;
using Commands = DMD.APPLICATION.PatientsModule.PatientUploads.Commands;

namespace DMD.API.Controllers.Patient
{
    [Route("api/dmd/patient-uploads")]
    public class PatientUploadController : BaseController
    {
        [HttpGet("get-patient-uploads")]
        [Description("Query return Patient Upload model")]
        [ProducesResponseType(typeof(List<PatientEmergencyContactModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientUploads([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<List<PatientEmergencyContactModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-patient-uploads")]
        [Description("Create Patient Upload based on json body")]
        [ProducesResponseType(typeof(PatientModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatientUpload([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);
            var data = ((SuccessResponse<PatientModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-patient-uploads")]
        [Description("Update Patient Upload based on param ID and json data")]
        [ProducesResponseType(typeof(PatientModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatientUploads([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-patient-uploads")]
        [Description("Delete Patient Upload, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatientUploads([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
