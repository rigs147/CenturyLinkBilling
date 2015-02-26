using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Mail;
using Newtonsoft.Json.Linq;

namespace ClBillingAPI2
{
    /// <summary>
    /// Main entry
    /// </summary>
    class Program
    {
        /// <summary>
        /// Mains the specified arguments.
        /// </summary>
        /// <param name="args">The arguments.</param>
        static void Main(string[] args)
        {
            //API version 1 only needed to get all the accounts for the api being used. In this case, top level api.
            Api1.GetApi1Login();

            //API version 2 login
            GetLogin();

            GetData();
        }

        /// <summary>
        /// Gets the login.
        /// </summary>
        public static void GetLogin()
        {
            string bearerToken = null;

            HttpClient authClient = new HttpClient();
            authClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpContent content =
             new StringContent("{ \"username\":\"testadmin\", \"password\":\"Password1\" }", null, "application/json");

            HttpResponseMessage message =
                authClient.PostAsync("https://api.tier3.com/v2/authentication/login", content).Result;//using .Result to make the call synchronous

            var responseString = message.Content.ReadAsStringAsync();

            dynamic result = JsonConvert.DeserializeObject(responseString.Result);

            GlobalVar.bearerToken = result.bearerToken.ToString();
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        public static void GetData()
        {
            ////const string getDataCentreListUrl = "/v2/datacenters/{alias}";
            ////const string getDataCentres = "/v2/datacenters/{alias}/gb3";
            const string getDataCentreGroupsUrl = "/v2/datacenters/{alias}/GB3?groupLinks=true";
            ////const string getBillingUrl = "/v2/groups/{alias}/gb3-833/billing";
            ////const string getGroupUrl = "/v2/groups/{alias}/gb3-833";

            dynamic AccountEmails = string.Empty;

            using (var webClient = new System.Net.WebClient())
            {
                AccountEmails = JsonConvert.DeserializeObject(webClient.DownloadString("../../Accounts.json"));
            }

            //Get list of accounts for this api (means api must always be at the top level)
            dynamic accounts = Api1.CallRest("/REST/Account/GetAccounts/", null);

            List<object> accountList = new List<object>();
            
            //Filter out inactive accounts
            foreach (var account in accounts.Accounts)
            {
                if (account.IsActive == true)
                {
                    accountList.Add(account);
                }
            }

            foreach (dynamic account in accountList)
            {
                bool hasServers = false;

                string accountAlias = account.AccountAlias.ToString();
                ////string email = account.email;

                string dataCentreGroupsUrl = getDataCentreGroupsUrl;
                dataCentreGroupsUrl = dataCentreGroupsUrl.Replace("{alias}", accountAlias);

                dynamic data = CallRest(dataCentreGroupsUrl);


                //Send email to the account primary email contact summarizing the estimates for each server group.
                string subject = "Weekly DEV CLC cost update for: " + accountAlias + " - " + account.BusinessName.ToString();

                ////This is the for the email for Ben. list of all servers for all accounts with creation date and creator
                List<object> accountServersList = new List<object>();
                

                //string emailBodyHeader = "<h1>Billing details for: <h1>" + accountAlias + " - " + account.BusinessName.ToString();
                StringBuilder mailBody = new StringBuilder();
                mailBody.Append("<br />");
                mailBody.Append("<h2>Please see below for the weekly update for CenturyLink public cloud based servers:</h2>");
                mailBody.Append("<br />");
                mailBody.Append("<br />");
                mailBody.Append("<br />");
                //mailBody.Append(emailBodyHeader);

                ////todo: Disabled accounts, or inactive accounts will not have billing links etc. there is a better way of finding this out.
                if (data.links.Count >= 10)
                {
                    string billingUrl = data.links[10].href;

                    //you now have the billing URL for this account. When this is called you will get JSON with estimates etc for all server groups for
                    //that account only.

                    dynamic billingData = CallRest(billingUrl);

                    double totalTemplateCost = 0;
                    double totalArchiveCost = 0;
                    double totalMonthlyEstimate = 0;
                    double totalMonthToDate = 0;
                    //double totalCurrentHour = 0;

                    //Build up email body
                    foreach (var group in billingData.groups)
                    {

                        foreach (var openGroup in group)
                        {
                            
                            string name = openGroup.name;

                            
                            if (name == "GB3 Hardware")
                            {
                                break;
                            }

                            if (((JObject)openGroup.servers).Count != 0)
                            {
                                hasServers = true;
                                // The servers object is not empty
                               
                                
                                foreach (var serverObject in openGroup.servers)
                                {
                                    
                                    dynamic server = serverObject.First;

                                    totalTemplateCost += (double)server.templateCost;
                                    totalArchiveCost += (double)server.archiveCost;
                                    totalMonthlyEstimate += (double)server.monthlyEstimate;
                                    totalMonthToDate += (double)server.monthToDate;
                                    //totalCurrentHour += (double)server.currentHour;
                                    
                                }

                            }
                            //else
                            //{
                            //    //the server object is empty
                            //    //mailBody.AppendFormat("<h1>Billing details for server group: {0}</h1>", name);
                            //    //mailBody.AppendFormat("<br />");
                            //    //mailBody.AppendFormat("There are no active servers in this group.");
                            //}

                        }

                    }
                    if (!hasServers)
                    {
                        //no servers for the account
                        //mailBody.AppendFormat("<h1>Billing details for account: {0}</h1>", name);
                        mailBody.AppendFormat("<br />");
                        mailBody.AppendFormat("There are no active servers in this account.");
                    }
                    else
                    {
                        //mailBody.AppendFormat(emailBodyHeader);
                        //mailBody.AppendFormat("<h1>Billing details for server group: {0}</h1>", name);
                        mailBody.AppendFormat("<p>Template Cost: ${0}</p>", totalTemplateCost);
                        mailBody.AppendFormat("<br />");
                        mailBody.AppendFormat("<p>Archive Cost: ${0}</p>", totalArchiveCost);
                        mailBody.AppendFormat("<br />");
                        mailBody.AppendFormat("<p>Month to date cost: ${0}</p>", totalMonthToDate);
                        mailBody.AppendFormat("<br />");
                        mailBody.AppendFormat("<p>Monthly estimate: ${0}</p>", totalMonthlyEstimate);
                        mailBody.AppendFormat("<br />");
                        //mailBody.AppendFormat("<p>Current Hour: ${0}</p>", totalCurrentHour);
                    }
                    

                }
                else
                {
                    //Console.WriteLine("Account {0} is diabled", accountAlias);
                }

                //Todo: this needs to be sent to the primary notification holder for each ACCOUNT!! currently there is no api to find this out so will either 
                //need to be hardcoded or the email can be sent to every user in that account. (you cant see who is the primary.) This is actually viable
                //as each account will only have a few users.
                //dynamic accountUsers = Api1.CallRest("/REST/User/GetUsers/", string.Format("{{\"AccountAlias\":\"{0}\"}}", accountAlias));

                //foreach (var user in accountUsers.Users)
                //{
                //    SendEmail(user.EmailAddress.ToString(), subject, mailBody);
                //}
                
                //There is no api to find the primary email holder for an account. So for now needs to either be hardcoded in a json file or send email to
                //every user of the account.
                //SendEmail("devtest1@aoglobal.dev", subject, mailBody);

                //foreach (var email in AccountEmails.accounts)
                //{
                //    if (email.alias == accountAlias)
                //    {

                //        foreach (var address in email.emails)
                //        {
                //            SendEmail(address.email.ToString(), subject, mailBody);
                //        }

                //        break;
            
                //    }
                    
                //}
                SendEmail("sean.rigney@allenovery.com", subject, mailBody);
                //SendEmail("sean.rigney@allenovery.com", subject, mailBody);

            }

        }

        //public static void SendEmail(string email, string subject, StringBuilder body)
        //{
        //    var server = "smtpinternal.omnia.aoglobal.dev";
        //    string fromAddress = "CenturyLink@allenovery.com";

        //    var client = new SmtpClient(server);

        //    //System.Net.NetworkCredential credentials =
        //    //    new System.Net.NetworkCredential("smtpUser", "pasword");

        //    var mailMessage = new MailMessage(fromAddress, email, subject, body.ToString()) { IsBodyHtml = true };

        //    client.Send(mailMessage);
        //}

        //public static void SendEmail(string email, string subject, StringBuilder body)
        //{

        //    //AODT1-relay@t3mx.com

        //    //    .s-necM9fHOLUG]:
        //    var server = "relay.t3mx.com";
        //    string fromAddress = "CenturyLink@allenovery.com";

        //    var client = new SmtpClient(server);

        //    //System.Net.NetworkCredential credentials =
        //    //    new System.Net.NetworkCredential("smtpUser", "pasword");
            
        //    System.Net.NetworkCredential credentials =
        //        new System.Net.NetworkCredential("AODT1", ".s-necM9fHOLUG]:");

        //    var mailMessage = new MailMessage(fromAddress, email, subject, body.ToString()) { IsBodyHtml = true };

        //    client.Send(mailMessage);
        //}

        /// <summary>
        /// Sends the email.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="body">The body.</param>
        public static void SendEmail(string email, string subject, StringBuilder body)
        {

                    MailMessage m = new MailMessage() { IsBodyHtml = true };
                    SmtpClient sc = new SmtpClient();

                    m.From = new MailAddress("AODT1-relay@t3mx.com", "CenturyLink");
                    m.To.Add(new MailAddress(email, "Display name To"));
                    m.Subject = subject;
                    m.Body = body.ToString();
                    sc.Host = "relay.t3mx.com";
                    sc.Port = 25;
                    sc.Credentials = new System.Net.NetworkCredential("AOD12-relay@t3mx.com", "-!4P=12gWpObm?,/");
                    //sc.EnableSsl = true; // runtime encrypt the SMTP communications using SSL
                    sc.Send(m);
        }



        /// <summary>
        /// Calls the rest service.
        /// </summary>
        /// <param name="uniqueUrl">The unique URL.</param>
        /// <returns>JSON response</returns>
        public static object CallRest(string uniqueUrl)
        {

            HttpClient Request = new HttpClient();
            Request.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //add bearer token to the header
            Request.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalVar.bearerToken);

            HttpResponseMessage serverMessage = Request.GetAsync("https://api.tier3.com" + uniqueUrl).Result;

            var ResponseString = serverMessage.Content.ReadAsStringAsync();

            var restResult = JsonConvert.DeserializeObject(ResponseString.Result);

            return restResult;
        }

    }
}
