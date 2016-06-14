using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;

namespace EndPointTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;

            IEnumerable<EndPointTest> items = null;
            using (StreamReader r = new StreamReader("EndPointTests.json"))
            {
                var json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<EndPointTest>>(json);
            }

            if (items != null)
            {
                var failures = new List<string>();
                foreach (var item in items)
                {
                    Debug.WriteLine(item.EndPoint);
                    if (item.Test() == false)
                    {
                        var msg = new StringBuilder();
                        msg.Append(item.EndPoint);

                        var stopResult = item.StopService();
                        if (stopResult != null)
                        {
                            var serverName = string.IsNullOrWhiteSpace(item.ServiceOnMachineName) ? String.Empty : $"on {item.ServiceOnMachineName} ";
                            if (stopResult == true)
                            {                                
                                msg.Append($" ({item.ServiceName} {serverName} is stopped.)");
                            }
                            else
                            {
                                msg.Append($" ({item.ServiceName} {serverName} is not stopped.)");
                            }
                        }

                        failures.Add(msg.ToString());
                    }
                }

                if (failures.Any())
                {
                    try
                    {
                        var mailMsg = new MailMessage();

                        // To
                        mailMsg.To.Add(new MailAddress(appSettings["toAddress"], appSettings["toName"]));

                        // From
                        mailMsg.From = new MailAddress(appSettings["fromAddress"], appSettings["fromName"]);

                        // Subject and multipart/alternative Body
                        mailMsg.Subject = "End Point Failures " + DateTime.Now.ToString(appSettings["dateFormat"]);
                        var buffer = new StringBuilder();
                        buffer.AppendLine("<h2>The following end points failed: </h2>");
                        buffer.AppendLine("<ul>");
                        failures.ForEach(i => buffer.AppendLine("<li>" + i + "</li>"));
                        buffer.AppendLine("<ul>");

                        mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(buffer.ToString(), null, MediaTypeNames.Text.Html));

                        using (var smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587)))
                        {
                            smtpClient.Credentials = new System.Net.NetworkCredential(appSettings["sendGridUser"], appSettings["sendGridPwd"]); ;
                            smtpClient.Send(mailMsg);
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
            }
        }
    }
}
