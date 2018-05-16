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

using forgesample.Forge;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace forgesample.Controllers
{
  public class WebhookController : ApiController
  {
    public string CallbackUrl { get { return Credentials.GetAppSetting("FORGE_WEBHOOK_CALLBACK_URL"); } }

    private string ExtractFolderIdFromHref(string href)
    {
      string[] idParams = href.Split('/');
      string resource = idParams[idParams.Length - 2];
      string folderId = idParams[idParams.Length - 1];
      if (!resource.Equals("folders")) return string.Empty;
      return folderId;
    }

    [HttpGet]
    [Route("api/forge/webhook")]
    public async Task<IList<GetHookData.Hook>> GetHooks([FromUri]string href)
    {
      string folderId = ExtractFolderIdFromHref(href);
      if (string.IsNullOrWhiteSpace(folderId)) return null;

      Credentials credentials = await Credentials.FromSessionAsync();

      DMWebhook webhooksApi = new DMWebhook(credentials.TokenInternal, CallbackUrl);
      IList<GetHookData.Hook> hooks = await webhooksApi.Hooks(Event.VersionAdded, folderId);

      return hooks; // return everything for now...
    }

    public struct HookInputData
    {
      public string href { get; set; }
    }

    [HttpPost]
    [Route("api/forge/webhook")]
    public async Task CreateHook([FromBody]HookInputData input)
    {
      string folderId = ExtractFolderIdFromHref(input.href);
      if (string.IsNullOrWhiteSpace(folderId)) return;

      Credentials credentials = await Credentials.FromSessionAsync();

      DMWebhook webhooksApi = new DMWebhook(credentials.TokenInternal, CallbackUrl);
      await webhooksApi.CreateHook(Event.VersionAdded, folderId);
    }

    [HttpDelete]
    [Route("api/forge/webhook")]
    public async Task DeleteHook([FromBody]HookInputData input)
    {
      string folderId = ExtractFolderIdFromHref(input.href);
      if (string.IsNullOrWhiteSpace(folderId)) return;

      Credentials credentials = await Credentials.FromSessionAsync();

      DMWebhook webhooksApi = new DMWebhook(credentials.TokenInternal, CallbackUrl);
      await webhooksApi.DeleteHook(Event.VersionAdded, folderId);
    }

    [HttpPost]
    [Route("api/forge/callback/webhook")]
    public async Task WebhookCallback([FromBody]JObject body)
    {

    }

  }
}
