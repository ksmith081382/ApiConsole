namespace ApiConsole
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;
    using DnsRequestGroup.Constants;
    using DnsRequestGroup.Models;


    /// <summary>
    /// The console application to execute.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The executing program.
        /// </summary>
        /// <param name="args">The array of string arguments.</param>
        static void Main(string[] args)
        {
            DnsRequestGroupModel dnsRequestGroupModel = CreateDnsRequestGroupModel();
            string requestGroupId = CreateDnsRequestGroup(dnsRequestGroupModel).Result;
            string status = GetStatus(requestGroupId).Result;
        }

        /// <summary>
        /// Attempts to create the DNS request group.
        /// </summary>
        /// <param name="dnsRequestGroupModel">The DNS request group model.</param>
        /// <returns>The Http response message object.</returns>
        private static async Task<string> CreateDnsRequestGroup(DnsRequestGroupModel dnsRequestGroupModel)
        {
            string id = string.Empty;

            try
            {
                HttpClient client = await CreateHttpClient();
                string json = "=" + JsonConvert.SerializeObject(dnsRequestGroupModel);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage response = client.PostAsync("DnsRequestGroups", content).Result;

                if (response.IsSuccessStatusCode)
                {
                    id = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    // The http response message will contain the error messages.
                    string errorResult = response.Content.ReadAsStringAsync().Result;

                    // Parse the string result into a list of error messages.
                    List<string> errors = errorResult.Split('|').ToList();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            return id;
        }

        /// <summary>
        /// Gets the DNS request group status.
        /// </summary>
        /// <param name="id">The DNS request group id.</param>
        private static async Task<string> GetStatus(string id)
        {
            string status = string.Empty;

            try
            {
                HttpClient client = await CreateHttpClient();
                HttpResponseMessage response = client.GetAsync("DnsRequestGroups/GetStatus/" + id).Result;

                if (response.IsSuccessStatusCode)
                {
                    status = response.Content.ReadAsStringAsync().Result;
                }
                else
                {
                    // The http response message will contain the error messages.
                    string errorResult = response.Content.ReadAsStringAsync().Result;

                    // Parse the string result into a list of error messages.
                    List<string> errors = errorResult.Split('|').ToList();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }

            return status;
        }

        /// <summary>
        /// Creates the HttpClient object and sets the header with the Azure access token.
        /// </summary>
        /// <returns>The Http client object.</returns>
        private static async Task<HttpClient> CreateHttpClient()
        {
            string token = await GetApiAccessToken();
            HttpClient client = new HttpClient();
            string ContentType = "application/x-www-form-urlencoded";
            client.BaseAddress = new Uri("https://dev-api.msftdomains.com/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType));
            client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
            return client;
        }

        /// <summary>
        /// Creates the DNS request group model.
        /// </summary>
        /// <returns>The DNS request group model.</returns>
        private static DnsRequestGroupModel CreateDnsRequestGroupModel()
        {
            // Set request group model properties.
            DnsRequestGroupModel dnsRequestGroupModel = new DnsRequestGroupModel();

            // List to hold the DNS requst models.
            dnsRequestGroupModel.DnsRequestModels = new List<DnsRequestModel>();

            // Set the action - Add: 0, Modify: 1, Delete: 2, ChangeOwner: 3, ChangeTimeToLive: 4
            dnsRequestGroupModel.Action = (byte)DnsAction.Add;

            // Set the priority - Low: 0, Medium: 1, High: 2
            dnsRequestGroupModel.Priority = (byte)DnsPriority.Low;

            // Set the DNS environment - Internet/Public-Facing: 0, Sovereign Cloud: 1, AD Integrated: 2
            dnsRequestGroupModel.Environment = (byte)DnsEnvironment.InternetPublicFacing;

            // Set timed event if needed.
            dnsRequestGroupModel.IsTimedEvent = true;

            // If timed event, the date and time should be set in pacific standard time (PST).
            dnsRequestGroupModel.DateToExecute = DateTime.Now.AddDays(10);

            // Time to execute format has to be 'hh:MM AM|PM'.
            dnsRequestGroupModel.TimeToExecute = "12:00 PM";

            // If environment is Internet/Public-Facing, set the zone.
            dnsRequestGroupModel.Zone = "kevsm.tst";

            // If environment is Internet/Public-Facing, set the manual flag.
            // The manual flag is set to true, if you want manual submission of DNS records (No automation of DNS changes).
            // For Sovereign Cloud and Ad-Integrated records, manual is always true and this property is ignored.
            dnsRequestGroupModel.Manual = true;

            // If environment is sovereign cloud, set child environment.
            //dnsRequestGroupModel.Environment = (byte)DnsEnvironment.SovereignCloud;
            //dnsRequestGroupModel.ChildEnvironment = (byte)SovereignCloudEnvironment.DEME;

            // If environment is AD-Integrated, set child environment.
            //dnsRequestGroupModel.Environment = (byte)DnsEnvironment.ADIntegrated;
            //dnsRequestGroupModel.ChildEnvironment = (byte)AdIntegratedEnvironment.AME;

            // Replace the string values below with the real values.
            dnsRequestGroupModel.RequestorAlias = "v-kevsm";
            dnsRequestGroupModel.PrimaryContactAlias = "v-kevsm";
            dnsRequestGroupModel.BusinessJustification = "Business Justification";
            dnsRequestGroupModel.OwnerGroupAlias = "DMAdminTest";

            // Add the DNS requests and set the model properties.
            // A record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay1",
                RecordType = "A",
                Data = "10.10.10.5"
            });

            // AAAA record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay2",
                RecordType = "AAAA",
                Data = "A123:B345:C567:D321:E456:43DD:Y7TR:SS39"
            });

            // NS record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay3",
                RecordType = "NS",
                Data = "test.nameserver.com."
            });

            // CNAME record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay4",
                RecordType = "CNAME",
                Data = "test.cname.com."
            });

            // SRV record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay5",
                RecordType = "SRV",
                Data = "test.srv.com.",
                Priority = 1,
                Weight = 1,
                Port = 300
            });

            // MX record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay6",
                RecordType = "MX",
                Data = "test.mx.com.",
                Preference = 100
            });

            // TXT record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "goodDay7",
                RecordType = "TXT",
                Data = "This is the text record data"
            });

            return dnsRequestGroupModel;
        }

        /// <summary>
        /// Gets the API access token.
        /// </summary>
        /// <returns>The access token string.</returns>
        private static async Task<string> GetApiAccessToken()
        {
            // The API uses bearer auth, so you need to have the AAD application Id and the app key.
            // Please replace these values with the real Client Id and App Key.
            string clientId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            string appKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

            string AuthUrl = "https://login.windows.net/microsoft.onmicrosoft.com";
            string Resource = "https://login.microsoftonline.com/dev-api.msftdomains.com";

            AuthenticationContext authenticationContext = new AuthenticationContext(AuthUrl);
            ClientCredential clientCredential = new ClientCredential(clientId, appKey);
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(Resource, clientCredential);
            return authenticationResult.AccessToken;
        }
    }
}