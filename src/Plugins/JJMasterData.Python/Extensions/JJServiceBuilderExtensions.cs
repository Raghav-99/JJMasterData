﻿#nullable enable

using System;
using JJMasterData.Commons.DI;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.FormEvents.Abstractions;
using JJMasterData.Python.Engine;
using JJMasterData.Python.FormEvents;
using JJMasterData.Python.Models;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Python.Extensions;
public static class Extensions
{
    public static JJServiceBuilder WithPythonEngine(this JJServiceBuilder builder)
    {
        builder.WithPythonEngine<PythonEngine>();
        return builder;
    }
    
    public static JJServiceBuilder WithPythonFormEventResolver(this JJServiceBuilder builder, Action<PythonFormEventOptions>? configure = null)
    {
        if(configure != null)
            builder.Services.Configure(configure);
        
        builder.Services.AddTransient<IFormEventResolver,PythonFormEventResolver>();
        
        return builder;
    }
}