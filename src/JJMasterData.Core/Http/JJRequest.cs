﻿#if NETFRAMEWORK
using System.Web;
#endif
#if NETCOREAPP || NETSTANDARD
using System;
using Microsoft.AspNetCore.Http;
#endif
namespace JJMasterData.Core.Http;

/// <summary>
/// Http Request helper class.
/// </summary>
public class JJRequest
{
#if NETFRAMEWORK
    private static HttpContext SystemWebCurrent => JJHttpContext.SystemWebCurrent;
#else
    private static Microsoft.AspNetCore.Http.HttpContext AspNetCoreCurrent => JJHttpContext.AspNetCoreCurrent;
#endif

    public string UserHostAddress
    {
        get
        {
#if NETFRAMEWORK
            return SystemWebCurrent.Request.UserHostAddress;
#else
            return AspNetCoreCurrent.Connection.RemoteIpAddress.ToString();
#endif
        }

    }

    public string HttpMethod
    {

        get
        {
#if NETFRAMEWORK
            return SystemWebCurrent.Request.HttpMethod;
#else
            return AspNetCoreCurrent.Request.Method;
#endif
        }

    }

#if NETFRAMEWORK
    public HttpPostedFile GetFile(string file)
    {
        return SystemWebCurrent.Request.Files[file];
    }
#else
    public IFormFile GetFile(string file) => AspNetCoreCurrent.Request.Form.Files[file];
#endif

    public object GetUnvalidated(string key)
    {
#if NETFRAMEWORK
        return SystemWebCurrent.Request.Unvalidated[key];
#else
        return GetValue(key);
#endif
    }

    public string UserAgent
    {
        get
        {
#if NETFRAMEWORK
            return SystemWebCurrent.Request.UserAgent;
#else

            return AspNetCoreCurrent.Request.Headers["User-Agent"];
#endif
        }
    }


#if NETFRAMEWORK
    public string AbsoluteUri => SystemWebCurrent.Request.Url.AbsoluteUri;
#else
    public string AbsoluteUri
    {
        get
        {
            var request = AspNetCoreCurrent.Request;
            var uriBuilder = new UriBuilder
            {
                Scheme = request.Scheme,
                Host = request.Host.Host,
                Port = request.Host.Port ?? 3000,
                Path = request.Path.ToString(),
                Query = request.QueryString.ToString()
            };
            return uriBuilder.Uri.ToString();
        }
    }
#endif


    public string this[string key] => GetValue(key);


    private string GetValue(string key)
    {
#if NETFRAMEWORK
        return SystemWebCurrent.Request[key];
#else

        if (AspNetCoreCurrent.Request.Query.ContainsKey(key))
        {
            return AspNetCoreCurrent.Request.Query[key];
        }

        if (AspNetCoreCurrent.Request.HasFormContentType)
        {
            return AspNetCoreCurrent.Request.Form[key];
        }

        return null;
#endif
    }

    public string QueryString(string key)
    {
#if NETFRAMEWORK
        return SystemWebCurrent.Request.QueryString[key];
#else
        return AspNetCoreCurrent.Request.Query[key];
#endif
    }


    public string Form(string key)
    {
#if NETFRAMEWORK
        return SystemWebCurrent.Request.Form[key];
#else
        if (AspNetCoreCurrent.Request.HasFormContentType)
            return (string)AspNetCoreCurrent.Request.Form[key];

        return null;
#endif
    }

}
