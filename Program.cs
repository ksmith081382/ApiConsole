namespace ApiConsole
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Script.Serialization;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Newtonsoft.Json;
    using DnsRequestGroup.Constants;
    using DnsRequestGroup.Models;

    /// <summary>
    /// The console application to execute.
    /// </summary>
    class Program
    {
        /// <summary>The header content type.</summary>
        private static readonly string ContentType = "application/x-www-form-urlencoded";

        /// <summary>The API endpoint.</summary>
        private static string Endpoint = "[replace with the API endpoint]";

        /// <summary>The API AUTH url.</summary>
        private static string AuthUrl = "[replace with the address of the authority]";

        /// <summary>The Azure resource.</summary>
        private static string Resource = "[replace with the Azure resource]";

        /// <summary>
        /// The executing program.
        /// </summary>
        /// <param name="args">The array of string arguments.</param>
        static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        /// <summary>
        /// The main asynchronous task to execute.
        /// </summary>
        static async Task MainAsync()
        {
            DnsRequestGroupModel dnsRequestGroupModel = CreateDnsRequestGroupModel();
            List<string> errors = await CreateDnsRequestGroup(dnsRequestGroupModel);
        }

        /// <summary>
        /// Attempts to create the DNS request group.
        /// </summary>
        /// <param name="dnsRequestGroupModel">The DNS request group model.</param>
        private static async Task<List<string>> CreateDnsRequestGroup(DnsRequestGroupModel dnsRequestGroupModel)
        {
            List<string> errors = new List<string>();

            try
            {
                string token = await GetApiAccessToken();
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri(Endpoint);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(ContentType));
                client.DefaultRequestHeaders.Add("Authorization", string.Format("Bearer {0}", token));
                string json = "=" + JsonConvert.SerializeObject(dnsRequestGroupModel);
                StringContent content = new StringContent(json, Encoding.UTF8, ContentType);
                HttpResponseMessage response = client.PostAsync("DnsRequestGroups", content).Result;

                if (!response.IsSuccessStatusCode)
                {
                    errors = ParseErrorMessages(response);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                errors.Add(ex.ToString());
            }

            return errors;
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
            dnsRequestGroupModel.Zone = "zone";

            // If environment is Internet/Public-Facing, set the manual flag.
            // The manual flag is set to true, if you want manual submission of DNS records (No automation of DNS changes).
            // For Sovereign Cloud and Ad-Integrated records, manual is always true and this property is ignored.
            dnsRequestGroupModel.Manual = false;

            // If environment is sovereign cloud, set child environment.
            //dnsRequestGroupModel.Environment = (byte)Environment.SovereignCloud;
            //dnsRequestGroupModel.ChildEnvironment = (byte)SovereignCloudEnvironment.DEME;

            // If environment is AD-Integrated, set child environment.
            //dnsRequestGroupModel.Environment = (byte)Environment.ADIntegrated;
            //dnsRequestGroupModel.ChildEnvironment = (byte)AdIntegratedEnvironment.AME;

            // Replace the string values below with the real values.
            dnsRequestGroupModel.RequestorAlias = "Alias";
            dnsRequestGroupModel.PrimaryContactAlias = "Alias";
            dnsRequestGroupModel.BusinessJustification = "Business Justification";
            dnsRequestGroupModel.OwnerGroupAlias = "Owner Group Alias";

            // Add the DNS requests and set the model properties.
            // A record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record1",
                RecordType = "A",
                Data = "10.10.10.5"
            });

            // AAAA record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record2",
                RecordType = "AAAA",
                Data = "A123:B345:C567:D321:E456:43DD:Y7TR:SS39"
            });

            // NS record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record3",
                RecordType = "NS",
                Data = "test.nameserver.com."
            });

            // CNAME record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record4",
                RecordType = "CNAME",
                Data = "test.cname.com."
            });

            // SRV record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record5",
                RecordType = "SRV",
                Data = "test.srv.com.",
                Priority = 1,
                Weight = 1,
                Port = 300
            });

            // MX record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record6",
                RecordType = "MX",
                Data = "test.mx.com.",
                Preference = 100
            });

            // TXT record.
            dnsRequestGroupModel.DnsRequestModels.Add(new DnsRequestModel
            {
                Name = "record7",
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
            // The API uses bearer auth, so you need to have the AD application client Id and the app key.
            // Please replace these values with your Client Id and App Key.
            string clientId = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx";
            string appKey = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
            AuthenticationContext authenticationContext = new AuthenticationContext(AuthUrl, false);
            ClientCredential clientCredential = new ClientCredential(clientId, appKey);
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenAsync(Resource, clientCredential);
            return authenticationResult.AccessToken;
        }

        /// <summary>
        /// Parses the Http response message object. 
        /// </summary>
        /// <param name="response">The Http response message object.</param>
        /// <returns>The list of error messages.</returns>
        private static List<string> ParseErrorMessages(HttpResponseMessage response)
        {
            string resultString = response.Content.ReadAsStringAsync().Result;
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            Dictionary<string, object> deserializedJson = (Dictionary<string, object>)serializer.Deserialize(resultString, typeof(object));
            Dictionary<string, object> errorDictionary = (Dictionary<string, object>)deserializedJson["error"];
            List<string> errors = new List<string>();

            foreach (KeyValuePair<string, object> keyValuePair in errorDictionary)
            {
                if (keyValuePair.Key == "message")
                {
                    dynamic objectValue = keyValuePair.Value;
                    string value = objectValue == null ? string.Empty : objectValue.ToString();

                    if (value.Contains("|"))
                    {
                        foreach (string error in value.Split('|'))
                        {
                            errors.Add(error.Trim());
                        }
                    }
                    else
                    {
                        errors.Add(value);
                    }
                }
            }

            return errors;
        }
    }
}