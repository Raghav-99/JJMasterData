﻿using System.Runtime.Serialization;
using JJMasterData.Core.DataDictionary;

namespace JJMasterData.WebApi.Models;

[DataContract(Name = "formValues")]
public class FormValues
{
    [DataMember(Name = "value")]
    public object? Value { get; set; }

    [DataMember(Name = "enable")]
    public bool Enable { get; set; }

    [DataMember(Name = "visible")]
    public bool Visible { get; set; }

    [DataMember(Name = "dataItems")]
    public IList<DataItemValue>? DataItems { get; set; }
}