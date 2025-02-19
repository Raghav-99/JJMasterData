﻿using System.Collections;
using System.Data;
using AutoFixture;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.DI;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DI;
using JJMasterData.Core.FormEvents.Abstractions;
using JJMasterData.Core.FormEvents.Args;

namespace JJMasterData.Xunit.Tester;

internal class DataDictionaryTester : IDataDictionaryTester
{
    private readonly IEntityRepository _entityRepository;
    private readonly IFormEventResolver _formEventResolver;
    private readonly FormService _formService;
 
    public string DictionaryName { get; }
    public string? UserId { get; }
    
    public DataDictionaryTester(Metadata metadata, string? userId = null)
    {
        DictionaryName = metadata.Table.Name;
        UserId = userId;
        
        _entityRepository = JJService.EntityRepository;
        _formEventResolver = JJServiceCore.FormEventResolver;
        _formService = GetFormService(metadata);
    }
   
    private FormService GetFormService(Metadata metadata)
    {
        bool logActionIsVisible = metadata.Options.ToolBarActions.LogAction.IsVisible;
        var userValues = new Hashtable
        {
            { "USERID", UserId }
        };
        
        var dataContext = new DataContext(DataContextSource.Form, UserId);
        var formEvent = _formEventResolver.GetFormEvent(metadata.Table.Name);
        formEvent?.OnMetadataLoad(dataContext,new MetadataLoadEventArgs(metadata));
        
        var formElement = metadata.GetFormElement();
        var expManager = new ExpressionManager(userValues, _entityRepository);
        var formManager = new FormManager(formElement, expManager);
        var service = new FormService(formManager, dataContext)
        {
            EnableHistoryLog = logActionIsVisible
        };

        service.AddFormEvent(formEvent);
        return service;
    }
    
    /// <summary>
    /// Test all DataDictionary operations with the given values.
    /// </summary>
    /// <param name="values"></param>
    /// <returns>A Dictionary with the name of the operation and the result.</returns>
    public DataDictionaryTesterResult AllOperations(Hashtable? values = null)
    {
        values ??= GetFixtureValues();

        var insertResult = Insert(values);
        var updateResult = Update(values);
        var readResult = Read(values);
        var deleteResult = Delete(values);

        return new DataDictionaryTesterResult(new()
        {
            { nameof(Insert), insertResult},
            { nameof(Update), updateResult},
            { nameof(Read), readResult},
            { nameof(Delete), deleteResult}
        });
    }
    
    /// <summary>
    /// Test all DataDictionary operations with the given values.
    /// </summary>
    /// <param name="configure"></param>
    /// <returns>A Dictionary with the name of the operation and the result.</returns>
    public DataDictionaryTesterResult AllOperations(Action<DataDictionaryTesterValues> configure)
    {

        var values = new DataDictionaryTesterValues();

        configure(values);
        
        var insertResult = Insert(values.InsertValues);
        var updateResult = Update(values.UpdateValues);
        var readResult = Read(values.ReadValues);
        var deleteResult = Delete(values.DeleteValues);
        
        return new DataDictionaryTesterResult(new Dictionary<string, FormLetter>
        {
            { nameof(Insert), insertResult},
            { nameof(Update), updateResult},
            { nameof(Read), readResult},
            { nameof(Delete), deleteResult}
        });
    }

    public FormLetter Insert(Hashtable? values = null)
    {
        values ??= GetFixtureValues();
        return _formService.Insert(values);
    }

    public FormLetter Update(Hashtable values) => _formService.Update(values);

    public FormLetter<DataTable> Read(Hashtable? filters = null)
    {
        var ret = new FormLetter<DataTable>();
        ret.Result =_entityRepository.GetDataTable( _formService.FormElement, filters);
        return ret;
    }
    
    public FormLetter Delete(Hashtable values) => _formService.Delete(values);
    
    public Hashtable GetFixtureValues()
    {
        var fixture = new Fixture();
        var example = new Hashtable();

        foreach (var field in _formService.FormElement.Fields)
        {
            switch (field.DataType)
            {
                case FieldType.Date:
                case FieldType.DateTime:
                case FieldType.DateTime2:
                    example[field.Name] = fixture.Create<DateTime>();
                    break;
                case FieldType.Float:
                    example[field.Name] = fixture.Create<float>();
                    break;
                case FieldType.Int:
                    example[field.Name] = fixture.Create<int>();
                    break;
                case FieldType.NText:
                case FieldType.NVarchar:
                case FieldType.Text:
                case FieldType.Varchar:
                    example[field.Name] = fixture.Create<string>();
                    break;
            }
        }

        return example;
    }
}