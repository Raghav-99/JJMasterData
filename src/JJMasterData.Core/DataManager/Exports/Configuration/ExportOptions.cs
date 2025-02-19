﻿using JJMasterData.Core.Web.Http;

namespace JJMasterData.Core.DataManager.Exports.Configuration;

/// <summary>
/// Classe responsável por configurar a exportação dos dados da JJGridView
/// </summary>
public class ExportOptions
{
    internal const string FileName = "_export_table_file";
    internal const string TableOrientation = "_export_table_orientation";
    internal const string ExportTableFirstLine = "_export_table_firstline";
    internal const string ExportAll = "_export_table_all";
    internal const string ExportDelimiter = "_export_table_delimiter";

    public ExportFileExtension FileExtension { get; set; }
    public bool ExportFirstLine { get; set; }
    public bool ExportAllFields { get; set; }
    public bool IsLandScape { get; set; }
    public string Delimiter { get; set; }

    public ExportOptions()
    {
        FileExtension = ExportFileExtension.CSV;
        ExportFirstLine = true;
        ExportAllFields = true;
        IsLandScape = false;
        Delimiter = ";";
    }

    internal static ExportOptions LoadFromForm(JJHttpContext currentContext, string objname)
    {
        var expConfig = new ExportOptions();
        if (currentContext.Request[objname + FileName] != null)
        {
            expConfig.FileExtension = (ExportFileExtension)int.Parse(currentContext.Request[objname + FileName]);
            expConfig.IsLandScape = "1".Equals(currentContext.Request[objname + TableOrientation]);
            expConfig.ExportFirstLine = "1".Equals(currentContext.Request[objname + ExportTableFirstLine]);
            expConfig.ExportAllFields = "1".Equals(currentContext.Request[objname + ExportAll]);
            expConfig.Delimiter = currentContext.Request[objname + ExportDelimiter];
        }

        return expConfig;
    }


}
