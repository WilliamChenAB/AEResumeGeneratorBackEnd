﻿using ae_resume_api.DBContext;
using ae_resume_api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ae_resume_api.Controllers
{

	[Route("Export")]
	[ApiController]
	public class ExportController : Controller
	{

		readonly DatabaseContext _databaseContext;
		private readonly IConfiguration configuration;

		public ExportController(DatabaseContext dbContext, IConfiguration configuration)
		{
			_databaseContext = dbContext;
			this.configuration = configuration;
		}

		/// <summary>
		/// Export Resume
		/// </summary>
		[HttpGet]
		[Route("Resume")]
		public async Task<IActionResult> ExportResume(int ResumeId)
		{
			var resume = await _databaseContext.Resume.FindAsync(ResumeId);
			if (resume == null) return NotFound("Resume not found");

			var result = ControllerHelpers.ResumeEntityToModel(resume);

			return new JsonResult(result);
		}

		/// <summary>
		/// Export Resumes in Workspace
		/// </summary>
		[HttpGet]
		[Route("ResumesInWorkspace")]
		public async Task ExportResumesInWorkspace(int WorkspaceId)
		{
			var workspace = await _databaseContext.Workspace.FindAsync(WorkspaceId);

            var resumes = await (from r in _databaseContext.Resume
								 where r.WorkspaceId == WorkspaceId
								 select r).ToListAsync();

			// Get all sectors in resume and set status as exported
			// Employees cannot use exported resumes

			List<ResumeModel> result = new List<ResumeModel>();
			foreach (var resume in resumes)
			{				
				if (!await ExportExists(resume))
                {
					// Create a persistant resume copy for exporting
					var exportedResume = new ResumeEntity
					{
						Creation_Date = resume.Creation_Date,
						Last_Edited = resume.Last_Edited,
						Name = $"Exported_{resume.Name}",
						Status = Status.Exported,
						WorkspaceId = null,
						TemplateId = resume.TemplateId,
						EmployeeId = resume.EmployeeId
					};
										
					var exported =  _databaseContext.Resume.Add(exportedResume);
					await _databaseContext.SaveChangesAsync();
					await CopySectorsHelper(resume, exported.Entity);

				}																			
				result.Add(ControllerHelpers.ResumeEntityToModel(resume));
			}

			await _databaseContext.SaveChangesAsync();


            // Create a file to write to
            // https://swimburger.net/blog/dotnet/create-zip-files-on-http-request-without-intermediate-files-using-aspdotnet-mvc-razor-pages-and-endpoints                       

            Response.ContentType = "application/octet-stream";
            Response.Headers.Add("Content-Disposition", "attachment; filename=\"resumes.zip\"");
            using (ZipArchive archive = new ZipArchive(Response.BodyWriter.AsStream(), ZipArchiveMode.Create, true))
            {
				foreach (var resumeText in result)
				{
					var path = resumeText.EmployeeName + ".txt";
					var text = JsonSerializer.Serialize(resumeText);

					var botFileName = Path.GetFileName(path);
					var entry = archive.CreateEntry(botFileName);
					using (var entryStream = entry.Open())
                    {
						using (MemoryStream stringInMemoryStream = new MemoryStream(ASCIIEncoding.Default.GetBytes(text)))
						{
							await stringInMemoryStream.CopyToAsync(entryStream);
						}
					}
				}
            }            
        }

		/// <summary>
		/// Export Resumes in Workspace
		/// </summary>
		[HttpGet]
		[Route("ResumesInWorkspaceXML.xml")]
		public async Task<ActionResult<IEnumerable<ResumeModel>>> ExportResumesInWorkspaceXML(int WorkspaceId)
		{
			throw new NotImplementedException();
		}

		private async Task<bool> ExportExists(ResumeEntity resume)
        {
			return await _databaseContext.Resume.AnyAsync(r => r.EmployeeId == resume.EmployeeId && 
															   r.Name == $"Exported_{resume.Name}");
		}
		private async Task CopySectorsHelper(ResumeEntity source, ResumeEntity dest)
		{
			var sectors = source.Sectors;

			sectors.ForEach(
				s => _databaseContext.Sector.Add(new SectorEntity
				{
					Content = s.Content,
					Creation_Date = ControllerHelpers.CurrentTimeAsString(),
					TypeId = s.TypeId,
					Last_Edited = s.Last_Edited,
					ResumeId = dest.ResumeId,
					Image = s.Image,
					Division = s.Division
				}));

			await _databaseContext.SaveChangesAsync();

		}

	}
}
