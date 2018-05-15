/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System.Web;
using System.Web.Http;
using System.Web.SessionState;

namespace forgesample
{
  public class WebApiApplication : System.Web.HttpApplication
  {
    protected void Application_Start()
    {
      GlobalConfiguration.Configure(WebApiConfig.Register);
    }

    // Enable session on WebAPI app
    // https://stackoverflow.com/a/17539008/4838205
    protected void Application_PostAuthorizeRequest()
    {
      HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
    }
    private bool IsWebApiRequest()
    {
      return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith("~/api");
    }
  }
}
