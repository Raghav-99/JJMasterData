﻿using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using System;
using System.Collections;
using System.IO;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Commons.Localization;

namespace JJMasterData.Core.DataManager;

public class FormFilePathBuilder
{
    private FormElement FormElement { get; set; }

    public FormFilePathBuilder(FormElement formElement)
    {
        FormElement = formElement;
    }

    public string GetFolderPath(FormElementField field, Hashtable formValues)
    {
        if (field.DataFile == null)
            throw new ArgumentException($"{nameof(FormElementField.DataFile)} not defined.", field.Name);

        //Pks concat with  underline
        string pkval = DataHelper.ParsePkValues(FormElement, formValues, '_');
        if (!Validate.ValidFileName(pkval))
            throw new JJMasterDataException(Translate.Key("Error rendering upload! Primary key value {0} contains invalid characters.",
                pkval));

        //Path configured in the dictionary
        string path = field.DataFile.FolderPath;

        if (string.IsNullOrEmpty(path))
            throw new ArgumentException($"{nameof(FormElementField.DataFile.FolderPath)} cannot be empty.", field.Name);

        char separator = Path.DirectorySeparatorChar;

        string appPath = FileIO.GetApplicationPath().TrimEnd(separator);

        path = path.Replace("{app.path}", appPath);
        path = Path.Combine(path, pkval);

        if (!path.EndsWith(separator.ToString()))
            path += separator;

        return path;
    }

   

}
