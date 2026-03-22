using DMD.APPLICATION.PatientsModule.Patient.Models;
using DMD.APPLICATION.PatientsModule.PatientProgressNotes.Models;
using DMD.APPLICATION.Responses;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using Commands = DMD.APPLICATION.PatientsModule.PatientProgressNotes.Commands;
using Queries = DMD.APPLICATION.PatientsModule.PatientProgressNotes.Queries;
namespace DMD.API.Controllers.Patient
{
    public class UploadPatientProgressNoteExcelRequest
    {
        public IFormFile? File { get; set; }
    }

    [Route("api/dmd/patient-progress-note")]
    public class PatientProgressNoteController : BaseController
    {
        [HttpGet("get-patient-progress-note")]
        [Description("Query return Patient Progres sNote model")]
        [ProducesResponseType(typeof(List<PatientProgressNoteModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientProgressNote([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<List<PatientProgressNoteModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-patient-progress-note")]
        [Description("Create Patient Progress Note based on json body")]
        [ProducesResponseType(typeof(PatientProgressNoteModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatientProgressNote([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);
            var data = ((SuccessResponse<PatientProgressNoteModel>)result).Data;
            return Created("", data);
        }

        [HttpPost("upload-patient-progress-note-xlsx")]
        [Description("Upload patient progress note xlsx file and import progress note records")]
        [ProducesResponseType(typeof(PatientUploadResultModel), (int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> UploadPatientProgressNoteXlsx([FromForm] UploadPatientProgressNoteExcelRequest request)
        {
            var file = request?.File;

            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            await using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);

            var command = new Commands.Upload.Command
            {
                FileName = file.FileName,
                FileContent = memoryStream.ToArray(),
            };

            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientUploadResultModel>)result).Data;
            return Ok(data);
        }

        [HttpPut("put-patient-progress-note")]
        [Description("Update Patient Progress Note based on param ID and json data")]
        [ProducesResponseType(typeof(PatientProgressNoteModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatientProgressNote([FromBody] Commands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<PatientProgressNoteModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-patient-progress-note")]
        [Description("Delete Patient Progress Note, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatientProgressNote([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
                return BadRequest(result.Message);

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }
    }
}
