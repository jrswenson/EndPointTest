using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;

namespace EndPointTest
{
    public class EndPointTest
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static TimeSpan defaultTimeSpan = new TimeSpan();
        private static IList<string> RequestMethods = new List<string> { "GET", "POST" };

        private TimeSpan timeOutSpan;
        private string timeOut = "";
        private string requestMethod = "GET";
        private IList<KeyValuePair<string, string>> queryParameters = new List<KeyValuePair<string, string>>();

        private HttpClient httpClient = null;

        ~EndPointTest()
        {
            log.Info("~EndPointTest");
            if (httpClient == null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
        }


        public string EndPoint { get; set; }

        public string TimeOut
        {
            get
            {
                return timeOut;
            }
            set
            {
                timeOut = value;
                if (TimeSpan.TryParse(timeOut, out timeOutSpan) == false)
                {
                    timeOutSpan = new TimeSpan();
                }

            }
        }

        public string RequestMethod
        {
            get
            {
                return requestMethod;
            }
            set
            {
                var tmpVal = value.ToUpper();
                if (RequestMethods.Contains(tmpVal))
                {
                    requestMethod = tmpVal;
                }
                else
                {
                    requestMethod = "GET";
                }
            }
        }

        public IList<KeyValuePair<string, string>> QueryParameters
        {
            get
            {
                return queryParameters;
            }
            set
            {
                if (value != null)
                    queryParameters = value;
            }
        }

        public string NetworkUserName { get; set; }

        public string NetworkPwd { get; set; }

        public string ServiceName { get; set; }

        public string ServiceOnMachineName { get; set; }

        private string GetQueryString()
        {
            var result = new StringBuilder();
            var delimiter = "?";
            foreach (var item in QueryParameters)
            {
                result.Append($"{delimiter}{item.Key}={item.Value}");
                delimiter = "&";
            }

            return result.ToString();
        }

        public bool Test()
        {
            if (httpClient == null)
            {
                if (string.IsNullOrWhiteSpace(NetworkUserName) || string.IsNullOrWhiteSpace(NetworkPwd))
                {
                    httpClient = new HttpClient { Timeout = timeOutSpan.CompareTo(defaultTimeSpan) > 0 ? timeOutSpan : new TimeSpan(0, 0, 0, 3, 0) };
                }
                else
                {
                    httpClient = new HttpClient(new HttpClientHandler { Credentials = new NetworkCredential(NetworkUserName, NetworkPwd) }) { Timeout = timeOutSpan.CompareTo(defaultTimeSpan) > 0 ? timeOutSpan : new TimeSpan(0, 0, 0, 3, 0) };
                }
            }

            try
            {
                switch (RequestMethod)
                {
                    case "POST":
                        break;
                    case "GET":
                    default:
                        var request = $"{this.EndPoint}{GetQueryString()}";
                        log.Debug(request);
                        var response = httpClient.GetAsync(request).Result;
                        response.EnsureSuccessStatusCode();
                        break;
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
                while(ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    log.Error(ex);
                }

                return false;
            }

            return true;
        }

        public bool? StopService()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
                return null;

            ServiceController service = null;
            try
            {
                service = new ServiceController(ServiceName, string.IsNullOrWhiteSpace(ServiceOnMachineName) ? ".": ServiceOnMachineName);
                if (service.Status == ServiceControllerStatus.Stopped)
                    return true;

                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped);
            }
            catch (InvalidEnumArgumentException ex)
            {
                return null;
                throw;
            }
            catch (ArgumentException ex)
            {
                return null;
                throw;
            }
            catch (System.ServiceProcess.TimeoutException ex)
            {
                return service == null ? null : (bool?)(service.Status == ServiceControllerStatus.Stopped);
                throw;
            }
            catch (Exception ex)
            {
                return null;
                throw;
            }

            return service.Status == ServiceControllerStatus.Stopped;
        }
    }
}
