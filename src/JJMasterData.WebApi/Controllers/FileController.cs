using JJMasterData.WebApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JJMasterData.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("masterApi/{elementName}/{id}/{fieldName}/file")]
public class FileController : ControllerBase
{
    private readonly FileService _service;
    
    public FileController(FileService service)
    {
        _service = service;
    }
    
    [HttpGet]
    [Route("{fileName}")]
    public IActionResult GetFile(string elementName, string id, string fieldName, string fileName)
    {
        var fileStream = _service.GetDictionaryFile(elementName, id, fieldName, fileName);

        return File(fileStream, "application/octet-stream", fileName);
    }
    
    [HttpPost]
    public IActionResult PostFile(string elementName, string fieldName, string id, IFormFile file)
    {
        _service.SetDictionaryFile(elementName, fieldName, id, file);

        return Created($"masterApi/{elementName}/{id}/{fieldName}/{file.FileName}", "File successfully created.");
    }
    
    [HttpPatch]
    [Route("{fileName}")]
    public IActionResult RenameFile(
        string elementName,
        string fieldName,
        string id,
        string fileName,
        [FromQuery] string newName)
    {
        _service.RenameFile(elementName, fieldName, id, fileName, newName);

        return Ok($"File sucessfuly renamed from {fileName} to {newName}");
    }
    
    [HttpDelete]
    [Route("{fileName}")]
    public IActionResult DeleteFile(string elementName, string fieldName, string id, string fileName)
    {
        _service.DeleteFile(elementName, fieldName, id, fileName);

        return Ok("File successfully deleted.");
    }
}