﻿using System;
using JJMasterData.Commons.DI;
using JJMasterData.Core.DataManager.Exports.Abstractions;
using JJMasterData.Core.DataManager.Exports.Configuration;
using JJMasterData.Core.Web.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Core.DataManager.Exports;

public static class WriterFactory
{

    public static IPdfWriter GetPdfWriter()
    {
        return JJService.Provider.GetService<IPdfWriter>();
    }

    public static IExcelWriter GetExcelWriter()
    {
        return JJService.Provider.GetService<IExcelWriter>();
    }

    public static ITextWriter GetTextWriter()
    {
        return JJService.Provider.GetService<ITextWriter>();
    }

    public static BaseWriter GetInstance(JJDataExp exporter)
    {
        BaseWriter writer;
        switch (exporter.ExportOptions.FileExtension)
        {
            case ExportFileExtension.CSV:
            case ExportFileExtension.TXT:
                var textWriter = GetTextWriter();
                textWriter.Delimiter = exporter.ExportOptions.Delimiter;
                textWriter.OnRenderCell += exporter.OnRenderCell;

                writer = (BaseWriter)textWriter;
                break;

            case ExportFileExtension.XLS:
                var excelWriter = GetExcelWriter();
                excelWriter.ShowRowStriped = exporter.ShowRowStriped;
                excelWriter.ShowBorder = exporter.ShowBorder;
                excelWriter.OnRenderCell += exporter.OnRenderCell;

                writer = (BaseWriter)excelWriter;

                break;
            case ExportFileExtension.PDF:
                var pdfWriter = GetPdfWriter();

                if (pdfWriter == null)
                    throw new NotImplementedException("Please implement IPdfWriter in your application services.");

                pdfWriter.ShowRowStriped = exporter.ShowRowStriped;
                pdfWriter.ShowBorder = exporter.ShowBorder;
                pdfWriter.OnRenderCell += exporter.OnRenderCell;

                // ReSharper disable once SuspiciousTypeConversion.Global;
                // PdfWriter is dynamic loaded by plugin.
                writer = pdfWriter as BaseWriter;

                break;
            default:
                throw new NotImplementedException();
        }

        ConfigureWriter(exporter, writer);

        return writer;
    }

    private static void ConfigureWriter(JJDataExp exporter, BaseWriter writer)
    {
        writer.FormElement = exporter.FormElement;
        writer.FieldManager = exporter.FieldManager;
        writer.Configuration = exporter.ExportOptions;
        writer.UserId = exporter.UserId;
        writer.ProcessOptions = exporter.ProcessOptions;
    }
}
