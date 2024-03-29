﻿using ae_resume_api.DBContext;
using ae_resume_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ae_resume_api.Controllers
{

    [Route("Resume")]
    [ApiController]
    public class ResumeController : Controller
    {

        readonly DatabaseContext _databaseContext;
        private readonly IConfiguration configuration;

        public ResumeController(DatabaseContext dbContext, IConfiguration configuration)
        {
            _databaseContext = dbContext;
            this.configuration = configuration;
        }

		/// <summary>
		/// Add a Resume to personal resumes
		/// </summary>
		[HttpPost]
		[Route("NewPersonal")]
		public async Task<ActionResult<ResumeModel>> NewResume(int TemplateId, string resumeName)
		{
			var EmployeeId = User.FindFirst(configuration["TokenIDClaimType"])?.Value;
			if (EmployeeId == null) return NotFound();
			return await New(TemplateId, resumeName, EmployeeId);
		}

		/// <summary>
		/// Add a Resume to an Employee
		/// </summary>
		[HttpPost]
		[Route("New")]
		[Authorize(Policy = "PA")]
		public async Task<ActionResult<ResumeModel>> New(int TemplateId, string resumeName, string EmployeeId)
		{
			var guid = Guid.Parse(EmployeeId);

			// Find the template record
			var template = await _databaseContext.Template.FindAsync(TemplateId);
			if (template == null) return NotFound("Template not found");

			var employee = await _databaseContext.Employee.FindAsync(guid);
			if (employee == null) return NotFound("Employee not found");

			ResumeEntity entity = new ResumeEntity
			{
				Creation_Date = ControllerHelpers.CurrentTimeAsString(),
				EmployeeId = guid,
				TemplateId = TemplateId,
				Name = resumeName,
				Last_Edited = ControllerHelpers.CurrentTimeAsString(),
				Status = Status.Regular
			};

			var resume = _databaseContext.Resume.Add(entity);
			await _databaseContext.SaveChangesAsync();

			await ControllerHelpers.PopulateTemplateSectors(template, resume.Entity.ResumeId, _databaseContext);

			return CreatedAtAction(
				nameof(Get),
				new { ResumeId = resume.Entity.ResumeId },
				 resume.Entity);
		}


		/// <summary>
		/// Delete a Resume
		/// </summary>
		[HttpDelete]
		[Route("Delete")]
		public async Task<IActionResult> Delete(int ResumeId)
		{
			var resume = await _databaseContext.Resume.FindAsync(ResumeId);
			if (resume == null) return NotFound("Resume not found");

			_databaseContext.Resume.Remove(resume);
			await _databaseContext.SaveChangesAsync();

			return Ok();
		}

		/// <summary>
		/// Get a Resume
		/// </summary>
		[HttpGet]
		[Route("Get")]
		public async Task<ActionResult<ResumeModel>> Get(int ResumeId)
		{
			var resume = await _databaseContext.Resume.FindAsync(ResumeId);
			if (resume == null) return NotFound("Resume not found");

			return ControllerHelpers.ResumeEntityToModel(resume); ;
		}

		[HttpPut]
		[Route("Edit")]
		public async Task<IActionResult> Edit(int ResumeId, string? resumeName)
        {
			var resume = await _databaseContext.Resume.FindAsync(ResumeId);

			if (resume == null)
			{
				return NotFound("Sector not found");
			}

			// Clean null content
			resumeName = resumeName == null ? "" : resumeName;

			resume.Creation_Date = ControllerHelpers.CurrentTimeAsString();
			resume.Last_Edited = ControllerHelpers.CurrentTimeAsString();
			resume.Name = resumeName;


			try
			{
				await _databaseContext.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				return BadRequest(ex.Message);
			}

			return Ok(resume);
		}

		/// <summary>
		/// Get all Resumes for an Employee
		/// </summary>
		[HttpGet]
		[Route("GetAllForEmployee")]
		[Authorize(Policy = "PA")]
		public async Task<ActionResult<IEnumerable<ResumeModel>>> GetAllForEmployee(string EmployeeId)
		{
			var resumes = _databaseContext.Resume.Where(r => r.EmployeeId == Guid.Parse(EmployeeId));
			List<ResumeModel> result = new List<ResumeModel>();

			foreach (var resume in resumes) result.Add(ControllerHelpers.ResumeEntityToModel(resume));

			return result;
		}

		/// <summary>
		/// Get personal Resumes
		/// </summary>
		[HttpGet]
		[Route("GetPersonal")]
		public async Task<ActionResult<IEnumerable<ResumeModel>>> GetPersonal()
		{
			var EmployeeId = User.FindFirst(configuration["TokenIDClaimType"])?.Value;
			if (EmployeeId == null) return NotFound();
			return await GetPersonalForEmployee(EmployeeId);
		}

		/// <summary>
		/// Get personal Resume for an Employee
		/// </summary>
		[HttpGet]
		[Route("GetPersonalForEmployee")]
		[Authorize(Policy = "PA")]
		public async Task<ActionResult<IEnumerable<ResumeModel>>> GetPersonalForEmployee(string EmployeeId)
		{
			// TODO: add only three statuses for resumes reqested, regular, exported
			var resumes =
				(await _databaseContext.Resume
				.Where(r => r.EmployeeId == Guid.Parse(EmployeeId))
				.ToListAsync())
				.Where(r => ControllerHelpers.ResumeIsPersonal(r));

			if (resumes == null) return NotFound("Resume not found");

			List<ResumeModel> result = new List<ResumeModel>();
			foreach (var resume in resumes)
			{
				result.Add(ControllerHelpers.ResumeEntityToModel(resume));
			}

			return result;
		}

	}
}
