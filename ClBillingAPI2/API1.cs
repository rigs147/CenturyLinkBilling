﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ClBillingAPI2
{
    class Api1
    {
        public static void GetApi1Login()
        {
            string authCookie = null;
            string key = "08f1f01192ba420b8d808d93b5654c6e";
            string pw = "[f1B?jVo%`VawoHB";

            HttpWebRequest req = WebRequest.Create("https://api.tier3.com/REST/Auth/Logon/") as HttpWebRequest;
            req.Method = "POST";

            string payload = string.Format("{{ 'APIKey': '" + key + "', 'Password': '" + pw + "' }}");
            req.ContentType = "application/json";

            //convert message to send to byte array
            byte[] byteData = UTF8Encoding.UTF8.GetBytes(payload.ToString());
            req.ContentLength = byteData.Length;

            //put request into stream
            using (Stream postStream = req.GetRequestStream())
            {
                postStream.Write(byteData, 0, byteData.Length);
            }

            //get response and process it
            using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
            {
                //create variable to hold cookie
                GlobalVar.authCookie = resp.Headers["Set-Cookie"];
            }
        }

        public static object CallRest(string uniqueUrl, string postData)
        {
            //create new web request
            HttpWebRequest reqQuery = WebRequest.Create("https://api.tier3.com" + uniqueUrl) as HttpWebRequest;
            reqQuery.Method = "POST";
            reqQuery.Headers.Add("Cookie", GlobalVar.authCookie);

            //string acctAlias = accountAlias;

            string queryPayload = string.Empty;

            if (postData != null)
            {
                queryPayload = postData;
            }

            reqQuery.ContentType = "application/json";

            //convert message to send to byte array
            byte[] byteDataQuery = UTF8Encoding.UTF8.GetBytes(queryPayload.ToString());
            reqQuery.ContentLength = byteDataQuery.Length;

            dynamic queryResponse = null;

            //put request into stream
            using (Stream postStream = reqQuery.GetRequestStream())
            {
                postStream.Write(byteDataQuery, 0, byteDataQuery.Length);
            }

            //invoke service
            using (HttpWebResponse resp = reqQuery.GetResponse() as HttpWebResponse)
            {
                // Get the response stream  
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    //load response
                    queryResponse = reader.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject(queryResponse);

        }
    }
}
