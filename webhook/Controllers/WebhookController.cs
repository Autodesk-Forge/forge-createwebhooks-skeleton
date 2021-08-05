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

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Autodesk.Forge;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Hangfire;

namespace WebHook.Controllers
{
    public class WebhookController : ControllerBase
    {
        /// <summary>
        /// Credentials on this request
        /// </summary>
        private Credentials Credentials { get; set; }

        // with the api/forge/callback/webhook endpoint
        // e.g. local testing with http://1234.ngrok.io
        public string CallbackUrl { get { return Credentials.GetAppSetting("FORGE_WEBHOOK_URL") + "/api/forge/callback/webhook"; } }


        [HttpGet]
        [Route("api/forge/webhook")]
        public async Task<IList<GetHookData.Hook>> GetHooks(string folder, string hub)
        {
            string folderId = HookInputData.ExtractFolderIdFromHref(folder);
            if (string.IsNullOrWhiteSpace(folderId)) return null;

            string hubId = HookInputData.ExtractHubIdFromHref(hub);
            if (string.IsNullOrWhiteSpace(hubId)) return null;

            Credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (Credentials == null) { return null; }

            DMWebhook webhooksApi = new DMWebhook(Credentials.TokenInternal, CallbackUrl);
            IList<GetHookData.Hook> hooks = await webhooksApi.Hooks(Event.VersionAdded, folderId, await webhooksApi.GetHubRegion(hubId));

            return hooks;
        }

        public class HookInputData
        {
            public static string ExtractFolderIdFromHref(string href)
            {
                string[] idParams = href.Split('/');
                string resource = idParams[idParams.Length - 2];
                string folderId = idParams[idParams.Length - 1];
                if (!resource.Equals("folders")) return string.Empty;
                return folderId;
            }

            public static string ExtractProjectIdFromHref(string href)
            {
                string[] idParams = href.Split('/');
                string resource = idParams[idParams.Length - 4];
                string folderId = idParams[idParams.Length - 3];
                if (!resource.Equals("projects")) return string.Empty;
                return folderId;
            }

            public static string ExtractHubIdFromHref(string href)
            {
                string[] idParams = href.Split('/');
                string resource = idParams[idParams.Length - 2];
                string hubId = idParams[idParams.Length - 1];
                if (!resource.Equals("hubs")) return string.Empty;
                return hubId;
            }
            public string folder {  get; set; }
            public string hub {  get; set; }

            public string FolderId { get { return ExtractFolderIdFromHref(folder); } }
            public string ProjectId { get { return ExtractProjectIdFromHref(folder); } }
            public string HubId { get { return ExtractHubIdFromHref(hub); } }
        }

        [HttpPost]
        [Route("api/forge/webhook")]
        public async Task<IActionResult> CreateHook([FromForm] HookInputData input)
        {
            if (string.IsNullOrWhiteSpace(input.FolderId)) return BadRequest();
            if (string.IsNullOrWhiteSpace(input.ProjectId)) return BadRequest();
            if (string.IsNullOrWhiteSpace(input.HubId)) return BadRequest();

            Credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (Credentials == null) { return Unauthorized(); }

            DMWebhook webhooksApi = new DMWebhook(Credentials.TokenInternal, CallbackUrl);
            await webhooksApi.CreateHook(Event.VersionAdded, input.ProjectId, input.FolderId, await webhooksApi.GetHubRegion(input.HubId));

            return Ok();
        }

        [HttpDelete]
        [Route("api/forge/webhook")]
        public async Task<IActionResult> DeleteHook(HookInputData input)
        {
            if (string.IsNullOrWhiteSpace(input.FolderId)) return BadRequest();
            if (string.IsNullOrWhiteSpace(input.ProjectId)) return BadRequest();
            if (string.IsNullOrWhiteSpace(input.HubId)) return BadRequest();

            Credentials = await Credentials.FromSessionAsync(base.Request.Cookies, Response.Cookies);
            if (Credentials == null) { return Unauthorized(); }

            DMWebhook webhooksApi = new DMWebhook(Credentials.TokenInternal, CallbackUrl);
            await webhooksApi.DeleteHook(Event.VersionAdded, input.FolderId, await webhooksApi.GetHubRegion(input.HubId));

            return Ok();
        }

        [HttpPost]
        [Route("api/forge/callback/webhook")]
        public async Task<IActionResult> WebhookCallback([FromBody] JObject body)
        {
            // catch any errors, we don't want to return 500
            try
            {
                string eventType = body["hook"]["event"].ToString();
                string userId = body["hook"]["createdBy"].ToString();
                string projectId = body["hook"]["hookAttribute"]["projectId"].ToString();
                string versionId = body["resourceUrn"].ToString();

                // do you want to filter events??
                if (eventType != "dm.version.added") return Ok();

                // your webhook should return immediately!
                // so can start a second thread (not good) or use a queueing system (e.g. hangfire)

                // starting a new thread is not an elegant idea, we don't have control if the operation actually complets...
                /*
                new System.Threading.Tasks.Task(async () =>
                  {
                      // your code here
                  }).Start();
                */

                // use Hangfire to schedule a job
                BackgroundJob.Schedule(() => ExtractMetadata(userId, projectId, versionId), TimeSpan.FromSeconds(30));
            }
            catch { }

            // ALWAYS return ok (200)
            return Ok();
        }

        public async static Task ExtractMetadata(string userId, string projectId, string versionId)
        {
            // this operation may take a moment
            Credentials credentials = await Credentials.FromDatabaseAsync(userId);

            // at this point we have:
            // projectId & versionId
            // valid access token

            // ready to access the files! let's do a quick test
            // as we're tracking the modified event, the manifest should be there...
            try
            {
                DerivativesApi derivativeApi = new DerivativesApi();
                derivativeApi.Configuration.AccessToken = credentials.TokenInternal;
                dynamic manifest = await derivativeApi.GetManifestAsync(Base64Encode(versionId));

                if (manifest.status == "inprogress") throw new Exception("Translating..."); // force run it again

                // now we have the metadata, can do something, like send email or generate a report...
                // for this sample, just a simple console write line
                Console.WriteLine(manifest);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw; // this should force Hangfire to try again 
            }
        }

        /// <summary>
        /// Base64 encode a string (source: http://stackoverflow.com/a/11743162)
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
