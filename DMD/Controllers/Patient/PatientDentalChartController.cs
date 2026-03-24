using DMD.APPLICATION.PatientsModule.PatientDentalChart.Models;
using DMD.APPLICATION.Responses;
using DMD.API.Storage;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using System.Security.Claims;
using Commands = DMD.APPLICATION.PatientsModule.PatientDentalChart.Commands;
using Queries = DMD.APPLICATION.PatientsModule.PatientDentalChart.Queries;
using DMD.APPLICATION.Common.ProtectedIds;

namespace DMD.API.Controllers.Patient
{
    public class UploadPatientDentalChartImageRequest
    {
        public IFormFile? File { get; set; }
        public string PatientInfoId { get; set; } = string.Empty;
        public int ToothNumber { get; set; }
    }

    public class UploadPatientDentalChartImageResponse
    {
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }

    [Route("api/dmd/patient-dental-chart")]
    public class PatientDentalChartController : BaseController
    {
        private readonly IClinicStorageService clinicStorageService;

        public PatientDentalChartController(IClinicStorageService clinicStorageService)
        {
            this.clinicStorageService = clinicStorageService;
        }

        [HttpGet("get-patient-dental-chart")]
        [Description("Query return Patient Dental Chart model")]
        [ProducesResponseType(typeof(List<PatientDentalChartModel>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPatientDentalChart([FromQuery] Queries.GetByParams.Query query)
        {
            var result = await Mediator.Send(query);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<List<PatientDentalChartModel>>)result).Data;
            return Ok(data);
        }

        [HttpPost("create-patient-dental-chart")]
        [Description("Create Patient Dental Chart based on json body")]
        [ProducesResponseType(typeof(PatientDentalChartModel), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePatientDentalChart([FromBody] Commands.Create.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<PatientDentalChartModel>)result).Data;
            return Created("", data);
        }

        [HttpPut("put-patient-dental-chart")]
        [Description("Update Patient Dental Chart based on json body")]
        [ProducesResponseType(typeof(PatientDentalChartModel), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> UpdatePatientDentalChart([FromBody] Commands.Update.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<PatientDentalChartModel>)result).Data;
            return Ok(data);
        }

        [HttpDelete("delete-patient-dental-chart")]
        [Description("Delete Patient Dental Chart, returns boolean")]
        [ProducesResponseType(typeof(bool), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> DeletePatientDentalChart([FromBody] Commands.Delete.Command command)
        {
            var result = await Mediator.Send(command);
            if (result is BadRequestResponse)
            {
                return BadRequest(result.Message);
            }

            var data = ((SuccessResponse<bool>)result).Data;
            return Ok(data);
        }

        [HttpPost("upload-patient-dental-chart-image")]
        [Description("Upload patient dental chart image and return saved file path")]
        [ProducesResponseType(typeof(UploadPatientDentalChartImageResponse), (int)HttpStatusCode.OK)]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> UploadPatientDentalChartImage([FromForm] UploadPatientDentalChartImageRequest request)
        {
            var file = request?.File;
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            if (string.IsNullOrWhiteSpace(request.PatientInfoId))
            {
                return BadRequest("PatientInfoId is required.");
            }

            if (request.ToothNumber < 1 || request.ToothNumber > 32)
            {
                return BadRequest("ToothNumber must be between 1 and 32.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest("Invalid file type. Allowed: jpg, jpeg, png, gif, webp.");
            }

            var clinicIdValue = User.FindFirstValue("clinicId");
            if (!int.TryParse(clinicIdValue, out var clinicId))
            {
                return BadRequest("Authenticated clinic was not found.");
            }

            var storedFile = await clinicStorageService.SaveClinicFileAsync(
                clinicId,
                file,
                HttpContext.RequestAborted,
                "patients",
                "dental-chart",
                request.PatientInfoId.Substring(0,10),
                $"tooth-{request.ToothNumber}");

            return Ok(new UploadPatientDentalChartImageResponse
            {
                FileName = storedFile.FileName,
                OriginalFileName = file.FileName,
                FilePath = storedFile.FilePath
            });
        }
    }
}
