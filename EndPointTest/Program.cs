using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Newtonsoft.Json;
using log4net;

namespace EndPointTest
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            var appSettings = ConfigurationManager.AppSettings;

            IEnumerable<EndPointTest> items = null;
            using (StreamReader r = new StreamReader(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EndPointTests.json")))
            {
                var json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<EndPointTest>>(json);
            }

            if (items != null)
            {
                var failures = new List<string>();
                foreach (var item in items)
                {
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

                        log.Info(msg.ToString());
                        failures.Add(msg.ToString());
                    }
                }

                if (failures.Any() && Boolean.Parse(appSettings["sendEmail"]))
                {
                    try
                    {
                        var buffer = new StringBuilder();
                        buffer.AppendLine("<h2>The following end points failed: </h2>");
                        buffer.AppendLine("<ul>");
                        failures.ForEach(i => buffer.AppendLine("<li>" + i + "</li>"));
                        buffer.AppendLine("<ul>");

                        var mailMsg = new MailMessage(new MailAddress(appSettings["fromAddress"], appSettings["fromName"]),
                            new MailAddress(appSettings["toAddress"], appSettings["toName"]));
                        mailMsg.Subject = $"End Point Failures {DateTime.Now.ToString(appSettings["dateFormat"])}";
                        mailMsg.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(buffer.ToString(), null, MediaTypeNames.Text.Html));

                        using (var smtpClient = new SmtpClient("smtp.sendgrid.net", Convert.ToInt32(587)))
                        {
                            smtpClient.Credentials = new System.Net.NetworkCredential(appSettings["sendGridUser"], appSettings["sendGridPwd"]); ;
                            smtpClient.Send(mailMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                    }
                }
            }
        }
    }
}
