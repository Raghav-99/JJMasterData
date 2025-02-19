﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Options;

namespace JJMasterData.Commons.Data.Providers;

public class OracleProvider : BaseProvider
{
    private const string INSERT = "I";
    private const string UPDATE = "A";
    private const string DELETE = "E";
    private const string TAB = "\t";
    public override string VariablePrefix => "p_";

    public OracleProvider(DataAccess dataAccess) : base(dataAccess)
    {
    }

    public override string GetCreateTableScript(Element element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(Element));

        if (element.Fields == null || element.Fields.Count == 0)
            throw new ArgumentNullException(nameof(Element.Fields));

        StringBuilder sSql = new StringBuilder();
        StringBuilder sKeys = new StringBuilder();

        sSql.AppendLine("-- TABLE");
        sSql.Append("CREATE TABLE ");
        sSql.Append(element.TableName);
        sSql.AppendLine(" (");
        bool isFirst = true;
        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        foreach (var f in fields)
        {
            if (isFirst)
                isFirst = false;
            else
                sSql.AppendLine(",");

            sSql.Append(TAB);
            sSql.Append(f.Name);
            sSql.Append(" ");
            sSql.Append(GetStrType(f.DataType));

            if (f.DataType == FieldType.Varchar ||
                f.DataType == FieldType.NVarchar)
            {
                sSql.Append(" (");
                sSql.Append(f.Size);
                sSql.Append(")");
            }

            if (f.IsRequired)
                sSql.Append(" NOT NULL");

            if (f.IsPk)
            {
                if (sKeys.Length > 0)
                    sKeys.Append(",");

                sKeys.Append(f.Name);
                sKeys.Append(" ");
            }
        }

        if (sKeys.Length > 0)
        {
            sSql.AppendLine(", ");
            sSql.Append(TAB);
            sSql.Append("CONSTRAINT PK_");
            sSql.Append(element.TableName);
            sSql.Append(" PRIMARY KEY (");
            sSql.Append(sKeys);
            sSql.Append(")");
        }


        sSql.AppendLine("");
        sSql.AppendLine(")");
        sSql.AppendLine("/");
        sSql.AppendLine("");
        sSql.AppendLine(GetRelationshipsScript(element));
        sSql.AppendLine("");

        int nIndex = 1;
        if (element.Indexes.Count > 0)
        {
            foreach (var index in element.Indexes)
            {
                sSql.Append("CREATE");
                sSql.Append(index.IsUnique ? " UNIQUE" : "");
                sSql.Append(index.IsClustered ? " CLUSTERED" : "");
                sSql.Append(" INDEX IX_");
                sSql.Append(element.TableName);
                sSql.Append("_");
                sSql.Append(nIndex);
                sSql.Append(" ON ");
                sSql.AppendLine(element.TableName);

                sSql.Append(TAB);
                sSql.AppendLine("(");
                for (int i = 0; i < index.Columns.Count; i++)
                {
                    if (i > 0)
                        sSql.AppendLine(", ");

                    sSql.Append(TAB);
                    sSql.Append(index.Columns[i]);
                }
                sSql.AppendLine("");
                sSql.Append(TAB);
                sSql.AppendLine(")");
                sSql.AppendLine("/");
                nIndex++;
            }
        }
        sSql.AppendLine("");

        //Criando sequencias (IDENTITY)
        var listSeq = element.Fields.ToList().FindAll(x => x.AutoNum);
        for (int i = 0; i < listSeq.Count; i++)
        {
            sSql.Append("CREATE SEQUENCE ");
            sSql.Append(element.TableName);
            sSql.Append("_seq");
            if (i > 0)
                sSql.Append(i.ToString());

            sSql.AppendLine(" START WITH 1 INCREMENT BY 1;");
            sSql.AppendLine("/");
        }
        sSql.AppendLine("");

        return sSql.ToString();
    }

    private string GetRelationshipsScript(Element element)
    {
        var sql = new StringBuilder();

        if (element.Relationships.Count > 0)
        {
            sql.AppendLine("-- RELATIONSHIPS");
            var listContraint = new List<string>();
            foreach (var r in element.Relationships)
            {
                string contraintName = string.Format("FK_{0}_{1}", r.ChildElement, element.TableName);

                //Tratamento para nome repedido de contraint
                if (!listContraint.Contains(contraintName))
                {
                    listContraint.Add(contraintName);
                }
                else
                {
                    bool hasContraint = true;
                    int nCount = 1;
                    while (hasContraint)
                    {
                        if (!listContraint.Contains(contraintName + nCount))
                        {
                            contraintName = contraintName + nCount;
                            listContraint.Add(contraintName);
                            hasContraint = false;
                        }
                        nCount++;
                    }
                }

                sql.Append("ALTER TABLE ");
                sql.AppendLine(r.ChildElement);
                sql.Append("ADD CONSTRAINT ");
                sql.Append(contraintName);
                sql.AppendLine(" ");
                sql.Append(TAB);
                sql.Append("FOREIGN KEY (");

                for (int rc = 0; rc < r.Columns.Count; rc++)
                {
                    if (rc > 0)
                        sql.Append(", ");

                    sql.Append("[");
                    sql.Append(r.Columns[rc].FkColumn);
                    sql.Append("]");
                }
                sql.AppendLine(")");
                sql.Append(TAB);
                sql.Append("REFERENCES ");
                sql.Append(element.TableName);
                sql.Append(" (");
                for (int rc = 0; rc < r.Columns.Count; rc++)
                {
                    if (rc > 0)
                        sql.Append(", ");

                    sql.Append(r.Columns[rc].PkColumn);
                }
                sql.Append(")");

                if (r.UpdateOnCascade)
                {
                    sql.AppendLine("");
                    sql.Append(TAB).Append(TAB);
                    sql.Append("ON UPDATE CASCADE ");
                }

                if (r.DeleteOnCascade)
                {
                    sql.AppendLine("");
                    sql.Append(TAB).Append(TAB);
                    sql.Append("ON DELETE CASCADE ");
                }

                sql.AppendLine("");
                sql.AppendLine("/");
            }
        }

        return sql.ToString();
    }

    public override string GetWriteProcedureScript(Element element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(Element));

        if (element.Fields == null || element.Fields.Count == 0)
            throw new ArgumentNullException(nameof(Element.Fields));

        var sql = new StringBuilder();
        bool isFirst = true;
        bool hasPk = HasPK(element);
        bool hasUpd = HasUpdateFields(element);
        string procedureFinalName = JJMasterDataCommonsOptions.GetWriteProcedureName(element);

        sql.AppendLine("-- PROC SET");

        //Criando proc
        sql.Append("CREATE OR REPLACE PROCEDURE ");
        sql.Append(procedureFinalName);
        sql.AppendLine("( ");
        sql.Append(VariablePrefix);
        sql.AppendLine("action IN VARCHAR2, ");

        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        foreach (var f in fields)
        {
            sql.Append(VariablePrefix);
            sql.Append(f.Name);
            sql.Append(" IN ");
            sql.Append(GetStrType(f.DataType));
            sql.AppendLine(", ");
        }
        sql.Append(VariablePrefix);
        sql.AppendLine("RET OUT NUMBER)");
        sql.AppendLine("IS ");
        sql.Append(TAB);
        sql.AppendLine(" v_TYPEACTION VARCHAR2(1); ");
        sql.Append(TAB);
        sql.AppendLine(" v_NCOUNT INTEGER; ");
        sql.AppendLine("BEGIN ");
        sql.Append(TAB);
        sql.Append("v_TYPEACTION := ");
        sql.Append(VariablePrefix);
        sql.AppendLine("action; ");
        sql.Append(TAB);
        sql.AppendLine("IF v_TYPEACTION = ' ' THEN");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("v_TYPEACTION := '" + INSERT + "'; ");

        if (hasPk)
        {
            sql.Append(TAB).Append(TAB);
            sql.AppendLine("SELECT COUNT(*) AS QTD INTO v_NCOUNT ");
            sql.Append(TAB).Append(TAB);
            sql.Append("FROM ");
            sql.Append(element.TableName);
            foreach (var f in fields)
            {
                if (f.IsPk)
                {
                    sql.AppendLine("");
                    if (isFirst)
                    {
                        sql.Append(TAB).Append(TAB);
                        sql.Append("WHERE ");
                        isFirst = false;
                    }
                    else
                    {
                        sql.Append(TAB).Append(TAB);
                        sql.Append("AND ");
                    }

                    sql.Append(f.Name);
                    sql.Append(" = ");
                    sql.Append(VariablePrefix);
                    sql.Append(f.Name);
                }
            }
            sql.AppendLine(";");
            sql.AppendLine(" ");
            sql.Append(TAB).Append(TAB);
            sql.AppendLine("IF v_NCOUNT > 0 THEN ");
            sql.Append(TAB).Append(TAB).Append(TAB);
            sql.AppendLine("v_TYPEACTION := '" + UPDATE + "';");
            sql.Append(TAB).Append(TAB);
            sql.AppendLine("END IF;");
        }
        sql.Append(TAB);
        sql.AppendLine("END IF;");
        sql.AppendLine(" ");


        //SCRIPT INSERT
        sql.Append(TAB);
        sql.AppendLine("IF v_TYPEACTION = '" + INSERT + "' THEN");
        sql.Append(TAB).Append(TAB);
        sql.Append("INSERT INTO ");
        sql.Append(element.TableName);
        sql.AppendLine(" (");
        isFirst = true;
        foreach (var f in fields)
        {
            if (!f.AutoNum)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sql.AppendLine(",");

                sql.Append("			");
                sql.Append(f.Name);
            }
        }
        sql.AppendLine(")");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("VALUES (");
        isFirst = true;
        foreach (var f in fields)
        {
            if (!f.AutoNum)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sql.AppendLine(",");

                sql.Append(TAB).Append(TAB).Append(TAB);
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
            }
        }
        sql.AppendLine(");");
        sql.Append(TAB).Append(TAB);
        sql.Append(VariablePrefix);
        sql.AppendLine("RET := 0; ");

        //SCRIPT UPDATE
        isFirst = true;
        if (hasUpd)
        {
            sql.Append(TAB);
            sql.AppendLine("ELSIF v_TYPEACTION = '" + UPDATE + "' THEN ");
            sql.Append(TAB).Append(TAB);
            sql.Append("UPDATE ");
            sql.Append(element.TableName);
            sql.AppendLine(" SET ");
            foreach (var f in fields)
            {
                if (!f.IsPk)
                {
                    if (isFirst)
                        isFirst = false;
                    else
                        sql.AppendLine(", ");

                    sql.Append(TAB).Append(TAB).Append(TAB);
                    sql.Append(f.Name);
                    sql.Append(" = ");
                    sql.Append(VariablePrefix);
                    sql.Append(f.Name);
                }
            }

            isFirst = true;
            foreach (var f in fields)
            {
                if (f.IsPk)
                {
                    sql.AppendLine("");
                    if (isFirst)
                    {
                        sql.Append(TAB).Append(TAB);
                        sql.Append("WHERE ");
                        isFirst = false;
                    }
                    else
                    {
                        sql.Append(TAB).Append(TAB);
                        sql.Append("AND ");
                    }

                    sql.Append(f.Name);
                    sql.Append(" = ");
                    sql.Append(VariablePrefix);
                    sql.Append(f.Name);
                }
            }
            sql.AppendLine(";");
            sql.Append(TAB).Append(TAB);
            sql.Append(VariablePrefix);
            sql.AppendLine("RET := 1; ");
        }
        else
        {
            sql.Append(TAB);
            sql.AppendLine("ELSIF v_TYPEACTION = '" + UPDATE + "' THEN ");
            sql.Append(TAB).Append(TAB);
            sql.AppendLine("--NO UPDATABLED");
            sql.Append(TAB).Append(TAB);
            sql.Append(VariablePrefix);
            sql.AppendLine("RET := 1; ");
        }

        //SCRIPT DELETE
        sql.Append(TAB);
        sql.AppendLine("ELSIF v_TYPEACTION = '" + DELETE + "' THEN ");
        sql.Append(TAB).Append(TAB);
        sql.Append("DELETE FROM ");
        sql.Append(element.TableName);

        isFirst = true;
        foreach (var f in fields)
        {
            if (f.IsPk)
            {
                sql.AppendLine("");
                if (isFirst)
                {
                    sql.Append(TAB).Append(TAB);
                    sql.Append("WHERE ");
                    isFirst = false;
                }
                else
                {
                    sql.Append(TAB).Append(TAB);
                    sql.Append("AND ");
                }

                sql.Append(f.Name);
                sql.Append(" = ");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
            }
        }
        sql.AppendLine(";");
        sql.Append(TAB).Append(TAB);
        sql.Append(VariablePrefix);
        sql.AppendLine("RET := 2; ");
        sql.Append(TAB);
        sql.AppendLine("END IF;");
        sql.AppendLine(" ");
        sql.AppendLine("END; ");
        sql.AppendLine("/");

        return sql.ToString();
    }

    public override string GetReadProcedureScript(Element element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(Element));

        if (element.Fields == null || element.Fields.Count == 0)
            throw new ArgumentNullException(nameof(Element.Fields));

        //Verificamos se existe chave primaria
        bool unused = HasPK(element);

        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior != FieldBehavior.Virtual);

        StringBuilder sql = new StringBuilder();
        string procedureFinalName = JJMasterDataCommonsOptions.GetReadProcedureName(element);

        sql.AppendLine("-- PROC GET");

        //Criando proc
        sql.Append("CREATE OR REPLACE PROCEDURE ");
        sql.Append(procedureFinalName);
        sql.AppendLine(" (");
        sql.Append(VariablePrefix);
        sql.AppendLine("orderby NVARCHAR2, ");

        foreach (var f in fields)
        {
            if (f.Filter.Type == FilterMode.Range)
            {
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.Append("_from ");
                sql.Append(GetStrType(f.DataType));
                sql.AppendLine(",");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.Append("_to ");
                sql.Append(GetStrType(f.DataType));
                sql.AppendLine(",");
            }
            else if (f.Filter.Type != FilterMode.None || f.IsPk)
            {
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.Append(" ");
                sql.Append(GetStrType(f.DataType));
                sql.AppendLine(", ");
            }
        }

        sql.Append(VariablePrefix);
        sql.AppendLine("regporpag INTEGER, ");
        sql.Append(VariablePrefix);
        sql.AppendLine("pag INTEGER, ");
        sql.Append(VariablePrefix);
        sql.AppendLine("qtdtotal IN OUT INTEGER, ");
        sql.Append(VariablePrefix);
        sql.AppendLine("cur_OUT OUT SYS_REFCURSOR) ");
        sql.AppendLine("IS ");
        sql.Append(TAB);
        sql.AppendLine("v_sqlcolumn    CLOB;");
        sql.Append(TAB);
        sql.AppendLine("v_sqltable     CLOB;");
        sql.Append(TAB);
        sql.AppendLine("v_sqlcond      CLOB;");
        sql.Append(TAB);
        sql.AppendLine("v_sqlorder     CLOB;");
        sql.Append(TAB);
        sql.AppendLine("v_query        CLOB;");
        sql.Append(TAB);
        sql.AppendLine("v_Cursor       SYS_REFCURSOR;");
        sql.AppendLine("BEGIN");
        sql.AppendLine("");

        sql.Append(TAB);
        sql.AppendLine("--COLUMNS");
        sql.Append(TAB);
        sql.AppendLine("v_sqlcolumn := '';");
        int nAux = 1;
        foreach (var f in fields)
        {
            sql.Append(TAB);


            sql.Append("v_sqlcolumn := v_sqlcolumn || '");

            if (f.DataBehavior == FieldBehavior.ViewOnly)
                sql.Append("'''' AS ");

            sql.Append(f.Name);

            if (nAux != element.Fields.Count)
                sql.Append(", ");

            sql.Append("'; ");

            if (f.DataBehavior == FieldBehavior.ViewOnly)
                sql.Append(" --TODO: ");

            sql.AppendLine("");

            nAux++;
        }

        sql.AppendLine("");
        sql.AppendLine("");

        sql.Append(TAB);
        sql.AppendLine("--TABLES");
        sql.Append(TAB);

        sql.Append("v_sqltable := 'FROM ");
        sql.Append(element.TableName);
        sql.AppendLine("';");
        sql.AppendLine("");
        sql.AppendLine("");

        sql.Append(TAB);
        sql.AppendLine("--CONDITIONALS");
        sql.Append(TAB);
        sql.AppendLine("v_sqlcond := ' WHERE 1=1 ';");

        foreach (var f in fields)
        {
            if (f.Filter.Type != FilterMode.None || f.IsPk)
            {
                if (f.DataBehavior == FieldBehavior.ViewOnly)
                {
                    sql.AppendLine("");
                    sql.Append(TAB);
                    sql.AppendLine("/*");
                    sql.Append(TAB);
                    sql.Append("TODO: FILTER ");
                    sql.AppendLine(f.Name);
                }
            }

            if (f.Filter.Type == FilterMode.Range)
            {
                sql.AppendLine("");
                sql.Append(TAB);
                sql.Append("IF ");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine("_from IS NOT NULL THEN");
                sql.Append(TAB).Append(TAB);
                sql.Append("v_sqlcond := v_sqlcond || ' AND ");
                sql.Append(f.Name);
                sql.AppendLine(" BETWEEN ';");
                sql.Append(TAB).Append(TAB);
                sql.Append("v_sqlcond := v_sqlcond || 'TO_DATE (''' || TO_CHAR (");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine("_from, 'YYYY-MM-DD') || ''', ''YYYY-MM-DD'') '; ");
                sql.Append(TAB).Append(TAB);
                sql.AppendLine("v_sqlcond := v_sqlcond || ' AND ';");
                sql.Append(TAB).Append(TAB);
                sql.Append("v_sqlcond := v_sqlcond || 'TO_DATE (''' || TO_CHAR (");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine("_to, 'YYYY-MM-DD') || ''', ''YYYY-MM-DD'') '; ");
                sql.Append(TAB);
                sql.AppendLine("END IF;");
            }
            else if (f.Filter.Type == FilterMode.Contain)
            {
                sql.AppendLine("");
                sql.Append(TAB);
                sql.Append("IF ");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine(" IS NOT NULL THEN");
                sql.Append(TAB).Append(TAB);
                sql.Append("v_sqlcond := v_sqlcond || ' AND ");
                sql.Append(VariablePrefix);
                sql.Append(" LIKE ''%' || ");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine(" ||  '%''';");
                sql.Append(TAB);
                sql.AppendLine("END IF;");
            }
            else if (f.Filter.Type == FilterMode.Equal || f.IsPk)
            {
                sql.AppendLine("");
                sql.Append(TAB);
                sql.Append("IF ");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine(" IS NOT NULL THEN");
                sql.Append(TAB).Append(TAB);
                sql.Append("v_sqlcond := v_sqlcond || ' AND ");
                sql.Append(VariablePrefix);
                sql.Append(" = ''' || ");
                sql.Append(VariablePrefix);
                sql.Append(f.Name);
                sql.AppendLine(" ||  '''';");
                sql.Append(TAB);
                sql.AppendLine("END IF;");
            }

            if (f.Filter.Type != FilterMode.None || f.IsPk)
            {
                if (f.DataBehavior == FieldBehavior.ViewOnly)
                {
                    sql.Append(TAB);
                    sql.AppendLine("*/");
                }
            }
        }

        sql.AppendLine("");
        sql.AppendLine("");
        sql.Append(TAB);
        sql.AppendLine("--ORDER");
        sql.Append(TAB);
        sql.AppendLine("v_sqlorder := ''; ");
        sql.Append(TAB);
        sql.Append("IF ");
        sql.Append(VariablePrefix);
        sql.AppendLine("orderby IS NOT NULL THEN ");
        sql.Append(TAB).Append(TAB);
        sql.Append("v_sqlorder  := ' ORDER BY ' || ");
        sql.Append(VariablePrefix);
        sql.AppendLine("orderby;");
        sql.Append(TAB);
        sql.Append("END IF; ");
        sql.AppendLine("");
        sql.AppendLine("");


        sql.Append(TAB);
        sql.AppendLine("--TOTAL OF RECORDS");
        sql.Append(TAB);
        sql.Append("IF ");
        sql.Append(VariablePrefix);
        sql.Append("qtdtotal IS NULL OR ");
        sql.Append(VariablePrefix);
        sql.AppendLine("qtdtotal = 0 THEN");
        sql.Append(TAB).Append(TAB);
        sql.Append(VariablePrefix);
        sql.AppendLine("qtdtotal := 0;");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("v_query := N'SELECT COUNT(*) ' || v_sqltable || v_sqlcond;");
        sql.AppendLine("");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("EXECUTE IMMEDIATE v_query");
        sql.Append(TAB).Append(TAB);
        sql.Append("INTO ");
        sql.Append(VariablePrefix);
        sql.AppendLine("qtdtotal; ");
        sql.Append(TAB);
        sql.AppendLine("END IF;");
        sql.AppendLine("");
        sql.AppendLine("");


        sql.Append(TAB);
        sql.AppendLine("--DATASET RESULT");
        sql.Append(TAB);
        sql.AppendLine("v_query := 'SELECT ' || v_sqlcolumn || ' ' ||");
        sql.Append(TAB);
        sql.AppendLine("'FROM (");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("SELECT ROWNUM R_NUM, TMP.* ");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("FROM (");
        sql.Append(TAB).Append(TAB).Append(TAB);
        sql.AppendLine("SELECT ' || v_sqlcolumn || v_sqltable || v_sqlcond || v_sqlorder || ");
        sql.Append(TAB).Append(TAB);
        sql.AppendLine("') TMP' ||");
        sql.Append(TAB);
        sql.AppendLine("')");
        sql.Append(TAB);
        sql.AppendLine("WHERE R_NUM BETWEEN ' || ");
        sql.Append(TAB);
        sql.Append("to_char((");
        sql.Append(VariablePrefix);
        sql.Append("pag - 1) * ");
        sql.Append(VariablePrefix);
        sql.AppendLine("regporpag + 1) || ");
        sql.Append(TAB);
        sql.Append("' AND ' ||");
        sql.Append(TAB);
        sql.Append("to_char(");
        sql.Append(VariablePrefix);
        sql.Append("pag * ");
        sql.Append(VariablePrefix);
        sql.AppendLine("regporpag); ");
        sql.AppendLine("");
        sql.Append(TAB);
        sql.AppendLine("OPEN v_Cursor FOR v_query;");
        sql.Append(TAB);
        sql.Append(VariablePrefix);
        sql.AppendLine("cur_OUT := v_Cursor;");

        sql.AppendLine("");
        sql.AppendLine("");
        sql.Append(TAB);
        sql.AppendLine("--DBMS_OUTPUT.PUT_LINE(v_query);");

        sql.AppendLine("");
        sql.AppendLine("END;");

        return sql.ToString();
    }

    public override DataAccessCommand GetInsertCommand(Element element, IDictionary values)
    {
        return GetCommandWrite(INSERT, element, values);
    }

    public override DataAccessCommand GetUpdateCommand(Element element, IDictionary values)
    {
        return GetCommandWrite(UPDATE, element, values);
    }

    public override DataAccessCommand GetDeleteCommand(Element element, IDictionary filters)
    {
        return GetCommandWrite(DELETE, element, filters);
    }

    public override DataAccessCommand GetInsertOrReplaceCommand(Element element, IDictionary values)
    {
        return GetCommandWrite(string.Empty, element, values);
    }

    private DataAccessCommand GetCommandWrite(string action, Element element, IDictionary values)
    {
        DataAccessCommand cmd = new DataAccessCommand();
        cmd.CmdType = CommandType.StoredProcedure;
        cmd.Sql = JJMasterDataCommonsOptions.GetWriteProcedureName(element);
        cmd.Parameters = new List<DataAccessParameter>();
        cmd.Parameters.Add(new DataAccessParameter(VariablePrefix + "action", action, DbType.String, 1));

        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        foreach (var f in fields)
        {
            var param = new DataAccessParameter
            {
                Name = $"{VariablePrefix}{f.Name}",
                Size = f.Size,
                Value = values.Contains(f.Name) ? values[f.Name] : DBNull.Value,
                Type = GetDbType(f.DataType)
            };
            cmd.Parameters.Add(param);
        }

        var pRet = new DataAccessParameter
        {
            Direction = ParameterDirection.Output,
            Name = VariablePrefix + "RET",
            Type = DbType.Int32
        };
        cmd.Parameters.Add(pRet);

        return cmd;
    }

    public override DataAccessCommand GetReadCommand(Element element, IDictionary filters, string orderBy, int recordsPerPage, int currentPage, ref DataAccessParameter pTot)
    {
        var cmd = new DataAccessCommand
        {
            CmdType = CommandType.StoredProcedure,
            Sql = JJMasterDataCommonsOptions.GetReadProcedureName(element),
            Parameters = new List<DataAccessParameter>
            {
                new(VariablePrefix + "orderby", orderBy),
                new(VariablePrefix + "regporpag", recordsPerPage),
                new(VariablePrefix + "pag", currentPage)
            }
        };

        foreach (var field in element.Fields)
        {
            if (field.Filter.Type == FilterMode.Range)
            {
                object valueFrom = DBNull.Value;
                if (filters != null &&
                    filters.Contains(field.Name + "_from") &&
                    filters[field.Name + "_from"] != null)
                {
                    valueFrom = filters[field.Name + "_from"];
                }
                var fromParameter = new DataAccessParameter
                {
                    Direction = ParameterDirection.Input,
                    Type = GetDbType(field.DataType),
                    Size = field.Size,
                    Name = VariablePrefix + field.Name + "_from",
                    Value = valueFrom
                };
                cmd.Parameters.Add(fromParameter);

                object valueTo = DBNull.Value;
                if (filters != null &&
                    filters.Contains(field.Name + "_to") &&
                    filters[field.Name + "_to"] != null)
                {
                    valueTo = filters[field.Name + "_to"];
                }
                var toParameter = new DataAccessParameter
                {
                    Direction = ParameterDirection.Input,
                    Type = GetDbType(field.DataType),
                    Size = field.Size,
                    Name = VariablePrefix + field.Name + "_to",
                    Value = valueTo
                };
                cmd.Parameters.Add(toParameter);
            }
            else if (field.Filter.Type != FilterMode.None || field.IsPk)
            {
                object value = DBNull.Value;
                if (filters != null &&
                    filters.Contains(field.Name) &&
                    filters[field.Name] != null)
                {
                    value = filters[field.Name];
                }

                var p = new DataAccessParameter();
                p.Direction = ParameterDirection.Input;
                p.Type = GetDbType(field.DataType);
                p.Size = field.Size;
                p.Name = VariablePrefix + field.Name;
                p.Value = value;
                cmd.Parameters.Add(p);
            }
        }

        cmd.Parameters.Add(pTot);

        var pCur = new DataAccessParameter
        {
            Name = VariablePrefix + "cur_OUT",
            Direction = ParameterDirection.Output,
            Type = DbType.Object
        };

        cmd.Parameters.Add(pCur);


        return cmd;
    }
    
    private string GetStrType(FieldType dataType)
    {
        string sType = dataType.ToString();
        switch (dataType)
        {
            case FieldType.Int:
                sType = "INTEGER";
                break;
            case FieldType.Varchar:
            case FieldType.NVarchar:
                sType = dataType + "2";
                break;
            case FieldType.DateTime:
                sType = "DATE";
                break;
            case FieldType.Text:
                sType = "CLOB";
                break;
            case FieldType.NText:
                sType = "NCLOB";
                break;
        }

        return sType;

    }

    private DbType GetDbType(FieldType dataType)
    {
        DbType t = DbType.String;
        switch (dataType)
        {
            case FieldType.Date:
                t = DbType.Date;
                break;
            case FieldType.DateTime:
                t = DbType.DateTime;
                break;
            case FieldType.Float:
                t = DbType.Double;
                break;
            case FieldType.Int:
                t = DbType.Int32;
                break;
        }
        return t;
    }

    private bool HasPK(Element element)
    {
        bool ret = false;
        foreach (var f in element.Fields)
        {
            if (f.IsPk)
            {
                ret = true;
                break;
            }
        }
        return ret;
    }

    private bool HasUpdateFields(Element element)
    {
        bool ret = false;
        foreach (var f in element.Fields)
        {
            if (!f.IsPk && f.DataBehavior == FieldBehavior.Real)
            {
                ret = true;
                break;
            }
        }
        return ret;
    }

    public override Element GetElementFromTable(string tableName)
    {
        return null;
    }
}