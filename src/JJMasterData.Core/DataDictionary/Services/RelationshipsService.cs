﻿using JJMasterData.Core.DataDictionary.Repository;
using JJMasterData.Core.DataDictionary.Services.Abstractions;
using System.Collections.Generic;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;

namespace JJMasterData.Core.DataDictionary.Services;

public class RelationshipsService : BaseService
{
    public RelationshipsService(IValidationDictionary validationDictionary, IDataDictionaryRepository dataDictionaryRepository)
        : base(validationDictionary, dataDictionaryRepository)
    {
    }

    public void Save(ElementRelationship elementRelationship, string sIndex, string dictionaryName)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        var relations = dictionary.Table.Relationships;

        if (!string.IsNullOrEmpty(sIndex))
        {
            int index = int.Parse(sIndex);
            relations[index] = elementRelationship;
        }
        else
        {
            relations.Add(elementRelationship);
        }

        DataDictionaryRepository.InsertOrReplace(dictionary);
    }

    public bool ValidateRelation(string childElement, string pkColumnName, string fkColumnName)
    {
        if (string.IsNullOrEmpty(childElement))
        {
            AddError("PkTableName", Translate.Key("Required PkTableName Field"));
            return IsValid;
        }
        
        if (!DataDictionaryRepository.Exists(childElement))
        {
            AddError("Entity", Translate.Key("Entity {0} not found", childElement));
            return IsValid;
        }

        if (string.IsNullOrEmpty(pkColumnName))
        {
            AddError("PkColumn", Translate.Key("Required PkColumn field"));
            return IsValid;
        }

        if (string.IsNullOrEmpty(fkColumnName))
        {
            AddError("FkColumn", Translate.Key("Required FkColumn field"));
            return IsValid;
        }

        var dictionary = DataDictionaryRepository.GetMetadata(childElement);
        var element = dictionary.Table;
        if (!element.Fields.ContainsKey(fkColumnName))
        {
            AddError("", Translate.Key("Column {0} not found in {1}.", fkColumnName, childElement));
            return IsValid;
        }

        if (!element.Fields.ContainsKey(pkColumnName))
        {
            AddError("", Translate.Key("Column {0} not found.", pkColumnName));
            return IsValid;
        }

        ElementField fkColumn = element.Fields[fkColumnName];
        ElementField pkColumn = element.Fields[pkColumnName];

        if (fkColumn.Filter.Type == FilterMode.None && !fkColumn.IsPk)
        {
            AddError("", Translate.Key("Column {0} has no filter or is not a primary key.", fkColumnName));
            return IsValid;
        }

        if (pkColumn.DataType != fkColumn.DataType)
        {
            AddError("", Translate.Key("Column {0} has incompatible types with column {1}", pkColumnName, fkColumnName));
            return IsValid;
        }

        return IsValid;
    }

    public bool ValidateFields(ElementRelationship elementRelationship, string dictionaryName, string sIndex)
    {
        if (string.IsNullOrWhiteSpace(elementRelationship.ChildElement))
            AddError("", Translate.Key("Mandatory <b>PKTable </b> field"));

        if (IsValid)
        {
            if (elementRelationship.Columns.Count == 0)
                AddError("", Translate.Key("No relationship registered."));
        }

        if (IsValid)
        {
            foreach (var r in elementRelationship.Columns)
            {
                ValidateRelation(elementRelationship.ChildElement, r.PkColumn, r.FkColumn);
            }
        }

        if (IsValid && string.IsNullOrEmpty(sIndex))
        {
            List<ElementRelationship> listRelation = GetFormElement(dictionaryName).Relationships.FindAll(x => x.ChildElement.Equals(elementRelationship.ChildElement));
            if (listRelation.Count > 0)
                AddError("", Translate.Key("There is already a relationship registered for ") + elementRelationship.ChildElement);
        }

        return IsValid;
    }

    public bool ValidateFinallyAddRelation(ElementRelationship elementRelationship, string pkColumnName, string fkColumnName)
    {
        if (ValidateRelation(elementRelationship.ChildElement, pkColumnName, fkColumnName))
        {
            var list = elementRelationship.Columns.FindAll(x =>
                x.PkColumn.Equals(pkColumnName) &&
                x.FkColumn.Equals(fkColumnName));

            if (list.Count > 0)
                AddError("", Translate.Key("Relationship already registered"));

        }

        return IsValid;
    }


    public void Delete(string dictionaryName, string index)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        ElementRelationship elementRelationship = dictionary.Table.Relationships[int.Parse(index)];
        dictionary.Table.Relationships.Remove(elementRelationship);
        DataDictionaryRepository.InsertOrReplace(dictionary);
    }


    public void MoveDown(string dictionaryName, string index)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        var relations = dictionary.Table.Relationships;
        int indexToMoveDown = int.Parse(index);
        if (indexToMoveDown >= 0 && indexToMoveDown < relations.Count - 1)
        {
            ElementRelationship elementRelationship = relations[indexToMoveDown + 1];
            relations[indexToMoveDown + 1] = relations[indexToMoveDown];
            relations[indexToMoveDown] = elementRelationship;

            DataDictionaryRepository.InsertOrReplace(dictionary);
        }
    }


    public void MoveUp(string dictionaryName, string index)
    {
        var dictionary = DataDictionaryRepository.GetMetadata(dictionaryName);
        var relations = dictionary.Table.Relationships;
        int indexToMoveUp = int.Parse(index);
        if (indexToMoveUp > 0)
        {
            ElementRelationship elementRelationship = relations[indexToMoveUp - 1];
            relations[indexToMoveUp - 1] = relations[indexToMoveUp];
            relations[indexToMoveUp] = elementRelationship;
            DataDictionaryRepository.InsertOrReplace(dictionary);
        }
    }

}