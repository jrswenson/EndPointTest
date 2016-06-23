using ComponentSpace.SAML2;
using ComponentSpace.SAML2.Assertions;
using ComponentSpace.SAML2.Protocols;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.Xml;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;

namespace EndPointTest
{
    public class EndPointTest
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool enabled = true;
        private static TimeSpan defaultTimeSpan = new TimeSpan();
        private static IList<string> RequestMethods = new List<string> { "GET", "POST", "SAML", "CONTEXT" };

        private TimeSpan timeOutSpan;
        private string timeOut = "";
        private string requestMethod = "GET";
        private IList<KeyValuePair<string, string>> queryParameters = new List<KeyValuePair<string, string>>();

        private bool? success = null;

        private HttpClient httpClient = null;

        ~EndPointTest()
        {
            log.Info("~EndPointTest");
            if (httpClient != null)
            {
                httpClient.Dispose();
                httpClient = null;
            }
        }

        public bool Enabled { get { return enabled; } set { enabled = value; } }
        public string EndPoint { get; set; }
        public string OnFailUrl { get; set; }

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

        public bool? Success { get { return success; } set { success = value; } }

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

        private HttpClient GetHttpClient()
        {
            if (string.IsNullOrWhiteSpace(NetworkUserName) || string.IsNullOrWhiteSpace(NetworkPwd))
            {
                return new HttpClient { Timeout = timeOutSpan.CompareTo(defaultTimeSpan) > 0 ? timeOutSpan : new TimeSpan(0, 0, 0, 3, 0) };
            }
            else
            {
                return new HttpClient(new HttpClientHandler { Credentials = new NetworkCredential(NetworkUserName, NetworkPwd) }) { Timeout = timeOutSpan.CompareTo(defaultTimeSpan) > 0 ? timeOutSpan : new TimeSpan(0, 0, 0, 3, 0) };
            }
        }

        private bool GetResponse(HttpClient client, string request, out HttpResponseMessage response)
        {
            try
            {
                log.Debug(request);
                response = httpClient.GetAsync(request).Result;
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                response = null;

                log.Error(ex);
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    log.Error(ex);
                }

                return false;
            }
        }

        private bool PostResponse(HttpClient client, string request, FormUrlEncodedContent content, out HttpResponseMessage response)
        {
            try
            {
                response = httpClient.PostAsync(request, content).Result;
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                response = null;

                log.Error(ex);
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    log.Error(ex);
                }

                return false;
            }
        }

        public bool Test()
        {
            httpClient = httpClient ?? GetHttpClient();
            HttpResponseMessage response;
            switch (RequestMethod)
            {
                case "SAML":
                    var saml = BuildSAML();
                    var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("SAMLResponse", saml) });
                    success = PostResponse(httpClient, EndPoint, content, out response);
                    if (success != null && success == true)
                    {
                        var resultContent = response.Content.ReadAsStringAsync().Result;
                        var ssoUrl = ParseSAMLResponse(resultContent);

                        var t = new Thread(() =>
                        {
                            var wb = new WebBrowser();
                            wb.ScrollBarsEnabled = false;
                            wb.ScriptErrorsSuppressed = true;
                            wb.Navigate(ssoUrl);
                            while (wb.ReadyState != WebBrowserReadyState.Complete) { Application.DoEvents(); }
                            var bodyHtml = wb.Document.Body.OuterHtml.ToString();
                        });

                        t.SetApartmentState(ApartmentState.STA);
                        t.Start();
                        t.Join(10000);
                    }

                    break;
                case "CONTEXT":
                    success = GetResponse(httpClient, $"{EndPoint}{GetQueryString()}", out response);
                    if (success != null && success == true)
                    {
                        var blah = response.Content.ReadAsStringAsync();
                        log.Debug(blah.Result);
                    }

                    break;
                case "GET":
                default:
                    success = GetResponse(httpClient, $"{EndPoint}{GetQueryString()}", out response);
                    break;
            }

            return success == true ? true : false;
        }

        private string BuildSAML()
        {
            var strIssuer = queryParameters.FirstOrDefault(i => i.Key == "issuer").Value;
            var member = queryParameters.FirstOrDefault(i => i.Key == "member").Value;
            var userEmail = queryParameters.FirstOrDefault(i => i.Key == "userEmail").Value;
            var cn = queryParameters.FirstOrDefault(i => i.Key == "cn").Value;
            var uid = queryParameters.FirstOrDefault(i => i.Key == "uid").Value;
            var pfxLocation = queryParameters.FirstOrDefault(i => i.Key == "pfxLocation").Value;
            var pfxPwd = queryParameters.FirstOrDefault(i => i.Key == "pfxPwd").Value;

            var samlResponse = new SAMLResponse();
            samlResponse.Issuer = new Issuer(strIssuer);
            samlResponse.Destination = strIssuer;

            var samlAssertion = new SAMLAssertion();
            samlAssertion.Issuer = new Issuer(strIssuer);
            samlAssertion.Subject = new Subject(new NameID(userEmail, null, null, SAMLIdentifiers.NameIdentifierFormats.EmailAddress, null));
            samlAssertion.Conditions = new Conditions(new TimeSpan(1, 0, 0));

            var authnStatement = new AuthnStatement();
            authnStatement.AuthnContext = new AuthnContext();
            authnStatement.AuthnContext.AuthnContextClassRef = new AuthnContextClassRef(SAMLIdentifiers.AuthnContextClasses.PasswordProtectedTransport);
            samlAssertion.Statements.Add(authnStatement);

            var attributeStatement = new AttributeStatement();
            attributeStatement.Attributes.Add(new SAMLAttribute("member", SAMLIdentifiers.AttributeNameFormats.Basic, null, member));
            samlAssertion.Statements.Add(attributeStatement);

            attributeStatement = new AttributeStatement();
            attributeStatement.Attributes.Add(new SAMLAttribute("mail", SAMLIdentifiers.AttributeNameFormats.Basic, null, userEmail));
            samlAssertion.Statements.Add(attributeStatement);

            attributeStatement = new AttributeStatement();
            attributeStatement.Attributes.Add(new SAMLAttribute("cn", SAMLIdentifiers.AttributeNameFormats.Basic, null, cn));
            samlAssertion.Statements.Add(attributeStatement);

            attributeStatement = new AttributeStatement();
            attributeStatement.Attributes.Add(new SAMLAttribute("uid", SAMLIdentifiers.AttributeNameFormats.Basic, null, uid));
            samlAssertion.Statements.Add(attributeStatement);

            samlResponse.Assertions.Add(samlAssertion);

            if (true)
            {
                var x509Certificate = Util.LoadSignKeyAndCertificate(pfxLocation, pfxPwd);
                var signedXml = new SignedXml(samlResponse.ToXml());
                signedXml.SigningKey = x509Certificate.PrivateKey;

                var keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(x509Certificate));
                signedXml.KeyInfo = keyInfo;

                // Create a reference to be signed.
                var reference = new Reference();
                reference.Uri = "#" + samlAssertion.ID;

                var env = new XmlDsigEnvelopedSignatureTransform();
                reference.AddTransform(env);
                signedXml.AddReference(reference);
                signedXml.ComputeSignature();

                samlResponse.Signature = signedXml.GetXml();

            }

            var result = samlResponse.ToXml().OuterXml.ToString();
            File.WriteAllText("SAMLPayload.xml", result);
            return Util.EncodeToBase64(result);
        }

        private string ParseSAMLResponse(string strResponse)
        {
            var strPatientURL = queryParameters.FirstOrDefault(i => i.Key == "patientUrl").Value;
            var sb = new StringBuilder();
            var xml = new XmlDocument();
            xml.LoadXml(Util.DecodeBase64(strResponse));
            var samlResponse = new SAMLResponse(xml.DocumentElement);

            File.WriteAllText("SAMLResponse.xml", samlResponse.ToString());

            foreach (SAMLAssertion samlAssertion in samlResponse.Assertions)
            {
                foreach (var attributeStatement in samlAssertion.GetAttributeStatements())
                {
                    foreach (SAMLAttribute samlAttribute in attributeStatement.Attributes)
                    {
                        if (samlAttribute.Name != "idptoken") continue;

                        sb.Append(strPatientURL);
                        sb.Append("&idptoken=");
                        sb.Append(samlAttribute.Values.FirstOrDefault());
                    }
                }
            }

            return sb.ToString();
        }

        public void OnFail()
        {
            httpClient = httpClient ?? GetHttpClient();

            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("setStatus", "false") });
            HttpResponseMessage response = httpClient.PostAsync(OnFailUrl, content).Result;
        }

        public void OnSuccess()
        {
            httpClient = httpClient ?? GetHttpClient();

            var content = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("setStatus", "true") });
            HttpResponseMessage response = httpClient.PostAsync(OnFailUrl, content).Result;
        }
    }
}
