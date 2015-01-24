// Copyright Dee Clawson, Created January 24, 2015
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to permit
// persons to whom the Software is furnished to do so, subject to the
// following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
// OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
// NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
// USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Net;
using System.IO;
using System.Collections.Generic; //For Dictionary
using System.Web.Script.Serialization; //For JSON Serilization, Reference System.Web.Extensions

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            //Declarations
            var ser = new JavaScriptSerializer();

            //Authenticate to Cityworks - Send Username Password In Return for Token
            Console.Write("Cityworks Username: ");
            string loginName = Console.ReadLine();
            Console.Write("Cityworks Password: ");
            string passWord = Console.ReadLine();

            //Call Cityworks To Return JSON
            string authReturn = callCityworks("authentication/authenticate", null, "{'LoginName':'" + loginName + "','Password':'" + passWord + "'}"); //service, token, data
            var authDict = ser.Deserialize<dynamic>(authReturn);

            if (authDict["Status"] != 0) //If Not Sucessfull 
            {
                Console.Write("\n" + authDict["Message"]);
            }
            else //Continue
            {

                //Parse out the Token & Get User Information 
                string userToken = authDict["Value"]["Token"];
                string userReturn = callCityworks("Authentication/User", userToken, "{}"); //service, token, data

                var userDict = ser.Deserialize<dynamic>(userReturn);
                Console.Write("\nHello " + userDict["Value"]["FullName"]);
                var empId = userDict["Value"]["EmployeeSid"];
                Console.Write("\nYou are employee " + empId);

                //Get The Users Work Orders
                string woIndexReturn = callCityworks("WorkOrder/Search", userToken, "{'ActualFinishDateIsNull':true,'Closed':false,'Canceled':false,'SubmitTo':[" + empId + "]}"); //service, token, data
                var woIndexDict = ser.Deserialize<WorkOrders>(woIndexReturn);
                //This is my favorite website: http://json2csharp.com/ it will convert the json to a class for you! Alot easier to use a class v. dynamic serialization.

                Console.Write("\n\nYou have " + woIndexDict.Value.Count + " open workorders.");

                foreach (string x in woIndexDict.Value)
                {
                    string woShowReturn = callCityworks("WorkOrder/ById", userToken, "{'WorkOrderId':'" + x + "'}"); //service, token, data
                    var woShowDict = ser.Deserialize<dynamic>(woShowReturn);
                    Console.Write("\nId: " + woShowDict["Value"]["WorkOrderId"] + "   " + woShowDict["Value"]["Description"] + "  Supervisor: " + woShowDict["Value"]["Supervisor"]);
                }

            }

            Console.ReadLine();
        }

        public class WorkOrders
        {
            public List<string> Value { get; set; }
            public int Status { get; set; }
            public object Message { get; set; }
        }

        static public string callCityworks(string service, string token, string data)
        {

            string baseURL = Convert.ToString("http://www.example.com/cityworks");
            string cwData = "data=" + data;
            string cwToken = "&token=" + token;
            string url = baseURL + "/services/" + service + "?" + cwData + cwToken;
            string feedData = string.Empty;

            try
            {
                HttpWebRequest request = null;
                HttpWebResponse response = null;
                Stream stream = null;
                StreamReader streamReader = null;
                request = (HttpWebRequest)WebRequest.Create(url);

                request.Method = "GET"; //Everthing is GET or POST in Cityworks, doesnt matter. Post might be a better choice so you can send everythin in as form-data vs. relying on the url. 

                request.ContentLength = 0;
                response = (HttpWebResponse)request.GetResponse();
                stream = response.GetResponseStream();
                streamReader = new StreamReader(stream);
                feedData = streamReader.ReadToEnd();

                response.Close();
                stream.Dispose();
                streamReader.Dispose();
            }

            catch (Exception ex)
            {
                Console.Write("\nMessage ---\n{0}" + ex.Message);
                Console.Write("\nHelpLink ---\n{0}" + ex.HelpLink);
                Console.Write("\nSource ---\n{0}" + ex.Source);
                Console.Write("\nStackTrace ---\n{0}" + ex.StackTrace);
                Console.Write("\nTargetSite ---\n{0}" + ex.TargetSite);
            }
            string result = feedData;
            return result;
        }

    }
}
