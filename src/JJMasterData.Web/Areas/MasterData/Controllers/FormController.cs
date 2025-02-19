﻿using JJMasterData.Commons.Util;
using JJMasterData.Core.Web.Components;
using JJMasterData.Web.Areas.MasterData.Models.ViewModels;
using JJMasterData.Web.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace JJMasterData.Web.Areas.MasterData.Controllers;

[Area("MasterData")]
public class FormController : MasterDataController
{
    public IActionResult Render(string dictionaryName)
    {
        var model = new FormViewModel(dictionaryName, ConfigureFormView);
        return View(model);
    }

    private void ConfigureFormView(JJFormView formView)
    {
        var userId = HttpContext.GetUserId();

        if (userId == null) 
            return;
        
        formView.SetCurrentFilter("USERID", userId);
        formView.SetUserValues("USERID", userId);
    }
}