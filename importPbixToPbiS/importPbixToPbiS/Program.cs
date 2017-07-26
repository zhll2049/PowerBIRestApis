
using System;
using System.Net;
//Install-Package Newtonsoft.Json 
using Newtonsoft.Json;
//Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory -Version 2.21.301221612
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IO;

namespace PBIGettingStarted
{

    //Sample to show how to use the Power BI API
    //  See also, http://docs.powerbi.apiary.io/reference

    //To run this sample:
    //Step 1 - Replace {Client ID from Azure AD app registration} with your client app ID. 
    //To learn how to get a client app ID, see Register a client app (https://msdn.microsoft.com/en-US/library/dn877542.aspx#clientID)

    class Program
    {
        //Step 1 - Replace {client id} with your client app ID. 
        //To learn how to get a client app ID, see Register a client app (https://msdn.microsoft.com/en-US/library/dn877542.aspx#clientID)
        private static string clientID = "49df1bc7-db68-4fb4-91c0-6d93f770d1a4";

        //RedirectUri you used when you registered your app.
        //For a client app, a redirect uri gives AAD more details on the specific application that it will authenticate.
        private static string redirectUri = "https://login.live.com/oauth20_desktop.srf";

        //Resource Uri for Power BI API
        private static string resourceUri = "https://analysis.windows.net/powerbi/api";

        //OAuth2 authority Uri
        private static string authority = "https://login.windows.net/common/oauth2/authorize";

        //Uri for Power BI datasets
        private static string PBIAPIUri = "https://api.powerbi.com/v1.0/myorg";

        private static AuthenticationContext authContext = null;
        private static string token = String.Empty;

        private static string datasetName = "testdataset";
        private static string groupID = "dc581184-a209-463b-8446-5432f16b6c15";

        private static string pbixPath = @"C:\test\test.pbix";
        private static string datasetDisplayName = "testdataset";

        static void Main(string[] args)
        {
            //Import sample 
            string importResponse = Import(string.Format("{0}/groups/{1}/imports?datasetDisplayName={2}", PBIAPIUri, groupID, datasetDisplayName), pbixPath);
            //append  parameter nameConflict=Overwrite when trying to replace an existing dataset
            //string importResponse = Import(string.Format("{0}/groups/{1}/imports?datasetDisplayName={2}&nameConflict=Overwrite", PBIAPIUri,groupID,  datasetDisplayName), pbixPath);

            Console.ReadLine();

        }

        /// <summary>
        /// Use AuthenticationContext to get an access token
        /// </summary>
        /// <returns></returns>
        static string AccessToken()
        {
            if (token == String.Empty)
            {
                //Get Azure access token
                // Create an instance of TokenCache to cache the access token
                TokenCache TC = new TokenCache();
                // Create an instance of AuthenticationContext to acquire an Azure access token
                authContext = new AuthenticationContext(authority, TC);
                // Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
                token = authContext.AcquireToken(resourceUri, clientID, new Uri(redirectUri), PromptBehavior.RefreshSession).AccessToken;
            }
            else
            {
                // Get the token in the cache
                token = authContext.AcquireTokenSilent(resourceUri, clientID).AccessToken;
            }

            return token;
        }

        public static string Import(string url, string fileName)
        {
            string responseStatusCode = string.Empty;

            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            request.Headers.Add("Authorization", String.Format("Bearer {0}", AccessToken()));

            using (Stream rs = request.GetRequestStream())
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);

                string headerTemplate = "Content-Disposition: form-data; filename=\"{0}\"\r\nContent-Type: application / octet - stream\r\n\r\n";
                string header = string.Format(headerTemplate, fileName);
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);

                using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = 0;
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        rs.Write(buffer, 0, bytesRead);
                    }
                }

                byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
                rs.Write(trailer, 0, trailer.Length);
            }


            try
            {

                using (HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse)
                {
                    responseStatusCode = response.StatusCode.ToString();
                    Console.WriteLine("imported pbix ", responseStatusCode);
                }
            }
            catch (WebException wex)
            {
                if (wex.Response != null)
                {
                    using (var errorResponse = (HttpWebResponse)wex.Response)
                    {
                        using (var reader = new StreamReader(errorResponse.GetResponseStream()))
                        {
                            string errorString = reader.ReadToEnd();
                            dynamic respJson = JsonConvert.DeserializeObject<dynamic>(errorString);
                            Console.WriteLine(respJson.ToString());
                            //TODO: use JSON.net to parse this string and look at the error message
                        }
                    }
                }
            }

            return responseStatusCode;
        }

    }
}
