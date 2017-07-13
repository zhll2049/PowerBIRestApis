using System;
using System.Net;
//Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory -Version 2.21.301221612
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Text;
//Install-Package Newtonsoft.Json 
using Newtonsoft.Json;
using System.IO;

namespace ConsoleApplication39
{

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

        private static AuthenticationContext authContext = null;
        private static string token = String.Empty;

        static void Main(string[] args)
        {

            HttpWebResponse response;
            string responseText;
            //This is the targetgatewayName that you'd like to create datasource for
            string targetGatewayName = "wxsqltest01";
            //This is the targetDataSourceName that you'd like to create 
            string targetDataSourceName = "MyDataSource";
            string gatewayID = "";
            string datasourceID = "";


            #region Get Gateways
            responseText = getGateways();

            dynamic respJson = JsonConvert.DeserializeObject<dynamic>(responseText);

            foreach (var gateway in respJson.value)
            {

                //get the gatewayID of my target gateway
                if (gateway["name"] == targetGatewayName)
                {
                    gatewayID = gateway["id"];
                    Console.WriteLine(gateway["id"]);
                    Console.WriteLine(gateway["name"]);
                    Console.WriteLine("");
                }

            }
            #endregion Get Gateways


            #region Get data sources (gateways)

            if (!string.IsNullOrEmpty(gatewayID))
            {
                responseText = getDataSourcesInfo(gatewayID, null, false);
                respJson = JsonConvert.DeserializeObject<dynamic>(responseText);

                foreach (var datasource in respJson.value)
                {
                    Console.WriteLine("datasourceName:{0}", datasource["datasourceName"]);
                    Console.WriteLine("datasourceType:{0}", datasource["datasourceType"]);
                    Console.WriteLine("connectionDetails:{0}", datasource["connectionDetails"]);
                    Console.WriteLine("");
                }
            }

            #endregion Get data sources (gateways)

            string isCreated = "";

            #region Create Data Source -- create a datasource for the target gateway
            if (!string.IsNullOrEmpty(gatewayID))
            {
                response = CreateDatasource(targetDataSourceName, gatewayID);
                isCreated = response.StatusCode.ToString();

                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), ASCIIEncoding.ASCII))
                {
                    responseText = reader.ReadToEnd();
                    respJson = JsonConvert.DeserializeObject<dynamic>(responseText);
                    datasourceID = respJson["id"];
                }

            }
            #endregion Create Data Source 


            #region Get data sources
            if (isCreated == "Created" && !string.IsNullOrEmpty(datasourceID))
            {

                responseText = getDataSourcesInfo(gatewayID, datasourceID, false);
                respJson = JsonConvert.DeserializeObject<dynamic>(responseText);
            }
            #endregion Get data sources

            #region Get data source users
            responseText = getDataSourcesInfo(gatewayID, datasourceID, true);
            respJson = JsonConvert.DeserializeObject<dynamic>(responseText);

            foreach (var users in respJson.value)
            {
                Console.WriteLine("emailAddress:{0}", users["emailAddress"]);
                Console.WriteLine("datasourceAccessRight:{0}", users["datasourceAccessRight"]);
                Console.WriteLine("");
            }
            #endregion Get data source users


            Console.ReadKey();

        }


        static HttpWebResponse CreateDatasource(string datasourceName, string gatewayId)
        {

            string powerBIDatasourcesApiUrl = String.Format("https://api.powerbi.com/v1.0/myorg/gateways/{0}/datasources", gatewayId);

            HttpWebRequest request = System.Net.WebRequest.Create(powerBIDatasourcesApiUrl) as System.Net.HttpWebRequest;
            //POST web request to create a datasource.
            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentLength = 0;
            request.ContentType = "application/json";

            //Add token to the request header
            request.Headers.Add("Authorization", String.Format("Bearer {0}", token));

            //Create dataset JSON for POST request
            string datasourceJson =
                "{" +
                    "\"dataSourceName\":\"" + datasourceName + "\"," +
                    "\"dataSourceType\":\"Sql\"," +
                    "\"onPremGatewayRequired\":true," +
                    "\"connectionDetails\":\"{\\\"server\\\":\\\"ericvm2\\\",\\\"database\\\":\\\"testdb2\\\"}\"," +
                    "\"credentialDetails\":{  " +
                    "\"credentials\":\"pkEnC6VcCxPQibuYZin6nk0HHfiHX9n/3UeFmcA/CDvMVDiJxfGtqv+9mfpFXM886f50Nex+24swH9wDMZVIsmIVN2GN2Dr5ba464Nmw5a7TqQjTR+AjnHAA73Ond69HPGDi6yroHizK/y8HDMR4w/MkNvKmEhGUT8VOPeEJELZ1mjaZQ/spZ9kqjjIKHjR5v4z3egKSc3wvloy9hzrkJKQWlMtcbfz5d5BT3bUSetcSynFwq5AMdgsjyD1r6S4eUcaoOpzFMLBIA9ft8mhqudb9EtVxwAElZwTwPYQ319XQALC9c4N0oo7/iBGnJjFGIFMm/cpS0EWgGZf192oSRg==\"," +
                    "\"credentialType\":\"Basic\"," +
                    "\"encryptedConnection\":\"Encrypted\"," +
                    "\"privacyLevel\":\"Public\"," +
                    "\"encryptionAlgorithm\":\"RSA-OAEP\"" +
                    "}}";

            Console.WriteLine(datasourceJson);

            //POST web request
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(datasourceJson);
            request.ContentLength = byteArray.Length;

            //Write JSON byte[] into a Stream
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteArray, 0, byteArray.Length);

                var response = (HttpWebResponse)request.GetResponse();

                Console.WriteLine(string.Format("Datasource {0}", response.StatusCode.ToString()));

                return response;
            }

        }

        public static string getGateways()
        {
            string responseStatusCode = string.Empty;

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.powerbi.com/v1.0/myorg/gateways");

            request.Method = "GET";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", AccessToken()));

            HttpWebResponse response2 = request.GetResponse() as System.Net.HttpWebResponse;

            string responseText = "bad request";

            using (HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse)
            {

                responseStatusCode = response.StatusCode.ToString();

                WebHeaderCollection header = response.Headers;

                var encoding = ASCIIEncoding.ASCII;

                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    responseText = reader.ReadToEnd();
                }
            }

            return responseText;
        }
        public static string getDataSourcesInfo(string gatewayID, string DataSourceID, Boolean isQueryUsers)
        {
            string responseStatusCode = string.Empty;
            HttpWebRequest request = null;

            if (!string.IsNullOrEmpty(DataSourceID) && isQueryUsers)
            {
                request = (HttpWebRequest)WebRequest.Create(String.Format("https://api.powerbi.com/v1.0/myorg/gateways/{0}/dataSources/{1}/users", gatewayID, DataSourceID));

            }
            else if (!string.IsNullOrEmpty(DataSourceID))
            {
                request = (HttpWebRequest)WebRequest.Create(String.Format("https://api.powerbi.com/v1.0/myorg/gateways/{0}/dataSources/{1}", gatewayID, DataSourceID));

            }
            else
            {

                request = (HttpWebRequest)WebRequest.Create(String.Format("https://api.powerbi.com/v1.0/myorg/gateways/{0}/dataSources", gatewayID));

            }


            request.Method = "GET";
            request.Headers.Add("Authorization", String.Format("Bearer {0}", AccessToken()));

            HttpWebResponse response2 = request.GetResponse() as System.Net.HttpWebResponse;

            string responseText = "bad request";

            using (HttpWebResponse response = request.GetResponse() as System.Net.HttpWebResponse)
            {

                responseStatusCode = response.StatusCode.ToString();

                WebHeaderCollection header = response.Headers;

                var encoding = ASCIIEncoding.ASCII;

                using (var reader = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    responseText = reader.ReadToEnd();
                }
            }

            return responseText;
        }


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
    }
}
