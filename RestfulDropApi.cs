using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Newtonsoft.Json;

namespace DropDownloadCore
{
    //Http calls to drop api and azure storage uses the following apis.
    //https://www.1eswiki.com/wiki/VSTS_Drop#Drop_REST_API
    //https://opsstaging.www.visualstudio.com/en-us/docs/integrate/api/drop/manifest?branch=teams%2Fartifact%2Fversion2
    //https://opsstaging.www.visualstudio.com/en-us/docs/integrate/api/blobstore/blobs?branch=teams%2Fartifact%2Fversion2

    public sealed class RestfulDropApi : IDropApi
    {
        public static readonly string APIVersionParam = "api-version";
        private readonly string _pat;
        
        public RestfulDropApi(string pat)
        {
            if (string.IsNullOrWhiteSpace(pat))
            {
                throw new ArgumentException("Non-empty PAT required.", nameof(pat));
            }

            _pat = pat;
        }
        
        
        /// <summary>
        /// Gets the manifest details
        /// </summary>
        /// <param name="manifestUri">The manifest uri.</param>
        /// <param name="blobAPIVersion">The API version to use for the blob API.</param>
        /// <param name="relativeRoot">The root path relative to the drop to retrieve.</param>
        /// <returns>The manifest details.</returns>         
        public async Task<IEnumerable<VstsFile>> GetVstsManifest(Uri manifestUri, string blobAPIVersion, 
                                                                 string relativeRoot)
        {
            // dotnet core doesn't currently handle vsts redirects well. Poking both teams about it
            // for now disable redirects.

            var clientHandler = new HttpClientHandler() { AllowAutoRedirect = false };
            using (var client = new HttpClient(clientHandler))
            {
                var base64EncodedString = Convert.ToBase64String(Encoding.UTF8.GetBytes("vstsdockerbuild:" + _pat));
                
                Console.WriteLine($"asking for drop manifest at {manifestUri}");
                
                var manifestRequest = new HttpRequestMessage(HttpMethod.Get, manifestUri);
                manifestRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedString);
                
                //Todo Polly for rety?
                var manifestResponse = await client.SendAsync(manifestRequest);
                if (manifestResponse.StatusCode == HttpStatusCode.RedirectMethod ||
                    manifestResponse.StatusCode == HttpStatusCode.Redirect)
                {
                     manifestResponse = await client.GetAsync(manifestResponse.Headers.Location);
                }
                manifestResponse.EnsureSuccessStatusCode();
                string manifestContent = await manifestResponse.Content.ReadAsStringAsync();
                     
                //filter here so we can be case insensitve. manifest url would take a directory but unlike root in drop.exe it is case sensitve.
                var manifest = JsonConvert.DeserializeObject<List<VstsFile>>(manifestContent)
                                    .Where(f => f.Path.StartsWith(relativeRoot, StringComparison.OrdinalIgnoreCase));
                
                
                // forget what our limit was but this would be bad for a whole drop. Batch has a certain
                // limit. Should we batch to 500 or lazily get sas urls for only certain directories
                // (those with dockerfiles in them)
                var uniqueBlobs = manifest.Select(file => file.Blob.Id).Distinct().Select(id => new { id =  id});
                var blobList = JsonConvert.SerializeObject(new { blobs = uniqueBlobs });
                var content = new StringContent(blobList);
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

                var blobHostname = manifestUri.Host.Replace("artifacts", "vsblob");
                var uriBuilder = new UriBuilder(manifestUri.Scheme, blobHostname, -1, "defaultcollection/_apis/blob/blobsbatch");
                var queryParameters = HttpUtility.ParseQueryString("");
                queryParameters.Add(APIVersionParam, blobAPIVersion);
                uriBuilder.Query = queryParameters.ToString();
                
                Console.WriteLine($"asking for sas tokens at {uriBuilder.Uri}");
                var blobRequest = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);
                blobRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64EncodedString);
                blobRequest.Content = content;
                var response = await client.SendAsync(blobRequest);
                response.EnsureSuccessStatusCode();

                string sasUrlJson = await response.Content.ReadAsStringAsync();
                var blobBatch = JsonConvert.DeserializeObject<BatchBlobResponse>(sasUrlJson);
                
                var urlDictionary = blobBatch.Blobs.ToDictionary(b => b.Id, b => b.Url);
                foreach (var file in manifest)
                {
                    file.Blob.Url = urlDictionary[file.Blob.Id];
                }
                return manifest;
            }
        }   

        internal sealed class BatchBlobResponse
        {
            /// <summary>
            /// Blob ids and SAS urls.
            /// </summary>
            
            public IEnumerable<VstsBlob> Blobs { get; set; }
            
        }
    }
}
