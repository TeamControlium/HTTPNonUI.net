using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using TeamControlium.Utilities;

namespace TeamControlium.HTTPNonUI
{
    /// <summary>Provides API for test scripts and libraries.  Encapsulates functionality for test scripts to interact with Non-UI HTTP based endpoints
    /// and apply errored protocol as required.
    /// </summary>
    /// <remarks>
    /// Class wraps requests in HTTP headers (valid, or errored) and performs request &amp; response at the TCP layer.  If SSL/TSL is being used, class performs all required
    /// wrapping and control as required. 
    /// </remarks>
    /// <example>
    /// The following examples demonstrate use of the class for performing interactions with an HTTP based endpoint.
    /// <para/><para/>
    /// <b>JSON query, using HTTP GET and Basic authentication (clear text).</b><br/>In this example, a list of
    /// document types is obtained, using JSON, from the Integration Microservices api.  Basic authentication is used with
    /// clear text username &amp; password.
    /// <code language="cs">
    /// namespace Example1
    /// {
    ///     class Program
    ///     {
    ///         static void Main(string[] args)
    ///         {
    ///             // Instantiate HTTPNonUI class
    ///             HTTPNonUI httpTest = new HTTPNonUI();
    /// 
    ///             // We want to get the list of document types.  So setup the domain and resource path.
    ///             // Using Basic Authentication, so using port 7080
    ///             string domainName = "mvIwsesb001.sunqld.com.au";
    ///             string resourcePath = "/v1.documents.members.api/types";
    ///             int port = 7080;
    /// 
    ///             // Construct the raw HTTP Header.  Using HTTP GET method.
    ///             // Need to tell it we want response in json, close to the connection as soon as the response is
    ///             // sent (otherwise we would hang until timeout expired) and the username password (Base64 encoded) as
    ///             // the basic authentication.  HTTP wants 2 Carriage Return Linefeeds after the header to indicate Header complete.
    ///             string header = "GET " + resourcePath + " HTTP/1.1\r\n" +
    ///                             "Host: "+ domainName +" \r\n" +
    ///                             "Accept: application/json\r\n" +
    ///                             "Connection: close\r\n" +
    ///                             "Authorization: Basic " + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("myusername:mypassword")) + "\r\n\r\n";
    /// 
    ///             // Do the Web Request and get the response as a list of Key/Value pairs/.
    ///             System.Collections.Generic.Dictionary&lt;string, string&gt; response = httpTest.SendWebRequest(domainName, port, header);
    /// 
    ///             // Just write raw response Key/Value pairs to console...
    ///             foreach (System.Collections.Generic.KeyValuePair&lt;string, string&gt; reponseLine in response)
    ///                 Console.WriteLine(reponseLine.Key + ": " + reponseLine.Value);
    ///         }
    ///     }
    /// }
    /// </code>
    /// The console output from this example;<br/>
    /// <c>sad: asd as a fd</c><br/>
    /// <c>sadf: asdfasdfasdf</c><br/>
    /// <para/>
    /// <b>This is abother</b><br/>
    /// sadfas
    /// df
    /// ds<br/>
    /// dsaf
    /// <code lang="cs">
    ///using System;
    ///using HTTPNonUI;
    ///
    ///namespace Example2
    ///{
    ///    class Program
    ///    {
    ///        static void Main(string[] args)
    ///        {
    ///            HTTPNonUI httpTest = new HTTPNonUI();
    ///
    ///            // We want to get the list of document types.  So, setup the domain and resource path.
    ///            // Using SSL, so on port 7600
    ///            string domainName = "mvIwsesb001.sunqld.com.au";
    ///            string resourcePath = "/v1.documents.members.api/types";
    ///            int port = 7600;
    ///            System.Security.Authentication.SslProtocols allowedSSLProtocols = System.Security.Authentication.SslProtocols.Default;
    ///
    ///            // Use the pfx certificate we have in out local folder - CertficateToUse.pfx  We sign the certificate with the password (in this case 'Computer1')
    ///            System.Security.Cryptography.X509Certificates.X509Certificate2 certificate = new System.Security.Cryptography.X509Certificates.X509Certificate2("CertificateToUse.pfx", "Computer1");
    ///
    ///            // Construct the raw HTTP Header.  Using HTTP GET method.
    ///            // Need to tell it we want response in json, to close to the connection as soon as the response is
    ///            // sent (otherwise we would hang until timeout expired) and the username password (Base64 encoded) as
    ///            // the basic authentication.  HTTP wants 2 Carriage Return Linefeeds after the header to indicate Header complete.
    ///            string header = "GET " + resourcePath + " HTTP/1.1\r\n" +
    ///                            "Host: " + domainName + " \r\n" +
    ///                            "Accept: application/json\r\n" +
    ///                            "Connection: close" + "\r\n\r\n";
    ///
    ///            // Do the Web Request and get the response as a list of Key/Value pairs/.
    ///            System.Collections.Generic.Dictionary&lt;string, string&gt; response = httpTest.SendSSLWebRequest(allowedSSLProtocols, certificate, domainName, port, header);
    ///
    ///            // Just write raw response Key/Value pairs to console...
    ///            foreach (System.Collections.Generic.KeyValuePair&lt;string, string&gt; reponseLine in response)
    ///                Console.WriteLine(reponseLine.Key + ": " + reponseLine.Value);
    ///        }
    ///    }
    ///}
    /// </code>
    /// </example>
    public class HTTPNonUI
    {
        /// <summary>
        /// Instantiates an instance of the HTTPNonUI class, used for testing an HTTP interface when used for Non-UI interaction (IE. WebServices, Json etc...)
        /// </summary>
        public HTTPNonUI()
        {
            RequestType = RequestTypes.Normal;
            RequestHeader = string.Empty;
            sendTimeout = null;
            receiveTimeout = null;
            ClientCertificate = null;
            sslProtocol = SslProtocols.None;
            useSSL = false;
            certificateValidationCallback = null;
        }

        /// <summary>
        /// Called when server certificate needs validation.  If not delegated certificates are automatically accepted irrelevant of correctness
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public delegate bool ValidateServerCertCallBack(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors);

        /// <summary>
        /// Posible HTTP Methods used for interaction with the HTTP portal being used.  Overridden if RequestType is set to InvalidHTTPMethod.
        /// </summary>
        public enum HTTPMethods
        {
            /// <summary>
            /// HTTP GET Method defined when building Request Header
            /// </summary>
            Post,
            /// <summary>
            /// HTTP POST Method defined when building Request Header
            /// </summary>
            Get
        };

        /// <summary>
        /// Test-Types of HTTP request.  Default is Normal. When not normal, area of Request is modified but ALL other aspects are unchanged allowing
        /// a targeted test to be applied.
        /// </summary>
        public enum RequestTypes
        {
            /// <summary>
            /// (Default) HTTP interaction is normal and should work at the HTTP level. 
            /// </summary>
            Normal,
            /// <summary>
            /// Requested HTTP Method is overridden with text INVALID.
            /// </summary>
            InvalidHTTPMethod,
            /// <summary>
            /// An HTTP version of 0.0 is used (usually this would be 1.1)
            /// </summary>
            InvalidHTTPVersion,
            /// <summary>
            /// The protocol is PHHT rather than HTTP
            /// </summary>
            BadProtocol,
            /// <summary>
            /// Instead of request header lines being Key: Value, they are Key; Value (IE. a Semicolon is used instead)
            /// </summary>
            BadlyFormedHeaderLine,
            /// <summary>
            /// Instead of CarriageReturn Linefeed, Linefeed CarriageReturn used in header lines seperators.
            /// </summary>
            HeaderLinesLFBeforeCR,
            /// <summary>
            /// Instead of CarriageReturn Linefeed, Linefeed characters only used in header lines seperators.
            /// </summary>
            HeaderLinesLFOnly,
            /// <summary>
            /// Instead of CarriageReturn Linefeed, CarriageReturn characters only used in header lines seperators.
            /// </summary>
            HeaderLinesCROnly,
            /// <summary>
            /// Fail the SSL Server certificate validation
            /// </summary>
            DoNotAcceptServerCertificate
        };


        /// <summary>
        /// X509 Certificate to use
        /// </summary>
        public X509Certificate2 ClientCertificate { private get; set; }

        /// <summary>
        /// The SSL Protocol negotiated and used in SSL based conversations.
        /// </summary>
        public SslProtocols SSLProtocol { get; private set; }

        private SslProtocols sslProtocol;

        private TimeSpan? sendTimeout;

        private TimeSpan? receiveTimeout;

        private bool useSSL;

        private RemoteCertificateValidationCallback certificateValidationCallback;

        /// <summary>
        /// Request Type used in the HTTP Request.  Default is RequestTypes.Normal indicating a normal HTTP request.
        /// </summary>
        public RequestTypes RequestType { get; set; }

        /// <summary>
        /// Request Header used in the HTTP Request.  Can be manually set, or built using BuildRequestHeader.
        /// </summary>
        public string RequestHeader { get; set; }

        /// <summary>
        /// Body of HTTP Request
        /// </summary>
        public string RequestBody { get; set; }

        /// <summary>
        /// Raw Response of HTTP Request
        /// </summary>
        public string ResponseRaw { get; private set; }

        /// <summary>
        /// Encodes a plain text string into Base64.
        /// </summary>
        /// <param name="plainText">Text to be converted</param>
        /// <returns>Equivalent string Base64 encoded</returns>
        public static string Base64Encode(string plainText)
        {
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plainText));
        }

        /// <summary>
        /// Builds and returns an HTTP Request header.  Header is well-formed unless  RequestType is set to RequestTypes.InvalidHTTPMethod, RequestTypes.BadProtocol, RequestTypes.InvalidHTTPVersion or RequestTypes.BadlyFormedHeaderLine.
        /// Built Header is also stored locally and used in Web Request/s if not passed explicitly.</summary>
        /// <param name="HTTPMethod">HTTP Method to be used.  GET or POST are currentlt supported.  Overridden if RequestType is set to RequestTypes.InvalidHTTPMethod</param>
        /// <param name="ResourcePath">Path to resource on the server</param>
        /// <param name="RequestHeaders">Request header lines to be added</param>
        /// <param name="Parameters">Parameters appended to the HTTP Resource path (If set, this is appended irrelevant of HTTP Method.</param>
        /// <returns>HTTP Header to be used in request</returns>
        public string BuildRequestHeader(HTTPMethods HTTPMethod, string ResourcePath, Dictionary<string, string> RequestHeaders, Dictionary<string, string> Parameters)
        {
            List<string> returnHeader = new List<string>();
            string returnString = null;

            //
            // Do the first line first....
            //
            // Top line looks something like this:-
            // GET /path/to/resource/page.html?param1=hello&param2=another HTTP/1.0
            //

            // HTTP Method
            string TopLine = null;
            if (RequestType == RequestTypes.InvalidHTTPMethod)
            {
                TopLine = "INVALID ";
            }
            else
            {
                switch (HTTPMethod)
                {
                    case HTTPMethods.Post: TopLine = "POST "; break;
                    case HTTPMethods.Get: TopLine = "GET "; break;
                    default: throw new ArgumentException("Must be POST or GET", "HTTPMethod");
                }
            }

            // ResourcePath - Is the path that defines the resource being accessed
            TopLine += ResourcePath;

            // If Parameters have been passed in use them (Irrelevant of Method - test may be doing this deliberately...)
            if (Parameters.Count > 0)
            {
                char Joiner = '?';
                foreach (KeyValuePair<string, string> Parameter in Parameters)
                {
                    TopLine += Joiner + Parameter.Key + "=" + Parameter.Value;
                    Joiner = '&';
                }
            }
            TopLine += ' ';

            // HTTP Version - This is always HTTP/1.1.  However, test may want to use bad protocol and/or version
            TopLine += (RequestType == RequestTypes.BadProtocol) ? "PHHT/" : "HTTP/";
            TopLine += (RequestType == RequestTypes.InvalidHTTPVersion) ? "0.0" : "1.1";

            // Add top line to list of header lines
            returnHeader.Add(TopLine);

            // And the rest of the header lines.  These are in the format of:-
            // Key:Value although the test may want badly formed lines so we use ; instead of :
            if ((RequestHeaders != null) && RequestHeaders.Count > 0)
            {
                string Joiner = (RequestType == RequestTypes.BadlyFormedHeaderLine) ? "; " : ": ";
                foreach (KeyValuePair<string, string> RequestHeader in RequestHeaders)
                {
                    returnHeader.Add(RequestHeader.Key + Joiner + RequestHeader.Value);
                }
            }

            // Finally, build the actual Header as a single string delimted with CR-LFs...  Test may want to use
            // different line endings, so allow for that....
            string HeaderLinesSeperator = "\r\n";
            switch (RequestType)
            {
                case RequestTypes.HeaderLinesCROnly: HeaderLinesSeperator = "\r"; break;
                case RequestTypes.HeaderLinesLFOnly: HeaderLinesSeperator = "\n"; break;
                case RequestTypes.HeaderLinesLFBeforeCR: HeaderLinesSeperator = "\n\r"; break;
            }

            foreach (string currentLine in returnHeader)
                returnString += currentLine + HeaderLinesSeperator;

            this.RequestHeader = returnString;

            return returnString;
        }

        /// <summary>
        /// Builds and returns an HTTP Request header.  Header is well-formed unless  RequestType is set to RequestTypes.InvalidHTTPMethod, RequestTypes.BadProtocol, RequestTypes.InvalidHTTPVersion or RequestTypes.BadlyFormedHeaderLine.
        /// </summary>
        /// <param name="HTTPMethod">HTTP Method to be used.  GET or POST are currentlt supported.  Overridden if RequestType is set to RequestTypes.InvalidHTTPMethod</param>
        /// <param name="ResourcePath">Path to resource on the server</param>
        /// <param name="RequestHeaders">Request header lines to be added</param>
        /// <returns>HTTP Header to be used in request</returns>
        public string BuildRequestHeader(HTTPMethods HTTPMethod, string ResourcePath, Dictionary<string, string> RequestHeaders)
        {
            // No parameters being used
            return BuildRequestHeader(HTTPMethod, ResourcePath, RequestHeaders, new Dictionary<string, string>());
        }

        /// <summary>
        /// Builds and returns an HTTP Request header.  Header is well-formed unless  RequestType is set to RequestTypes.InvalidHTTPMethod, RequestTypes.BadProtocol, RequestTypes.InvalidHTTPVersion or RequestTypes.BadlyFormedHeaderLine.
        /// </summary>
        /// <param name="HTTPMethod">HTTP Method to be used.  GET or POST are currentlt supported.  Overridden if RequestType is set to RequestTypes.InvalidHTTPMethod</param>
        /// <param name="ResourcePath">Resource Path for use in HTTP Header top-line</param>
        /// <returns>HTTP Header to be used in request</returns>
        public string BuildRequestHeader(HTTPMethods HTTPMethod, string ResourcePath)
        {
            // No request header lines or parameters
            return BuildRequestHeader(HTTPMethod, ResourcePath, new Dictionary<string, string>(), new Dictionary<string, string>());
        }

        /// <summary>
        /// Sends a Web Request using the RequestHeader and RequestBody properties
        /// returning a Key/Value pair collection of the raw response.
        /// </summary>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendWebRequest(string url, int Port)
        {
            string Response = SendTCPRequest(url, Port, BuildRequest(null, null));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader properties and passed RequestBody, sends the request data using plain text to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>If passed RequestBody is empty or null, RequestBody property is used.</remarks>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendWebRequest(string url, int Port, string RequestBody)
        {
            string Response = SendTCPRequest(url, Port, BuildRequest(null, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the passed RequestHeader and RequestBody, sends the request data using plain text to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</remarks>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendWebRequest(string url, int Port, string RequestHeader, string RequestBody)
        {
            string Response = SendTCPRequest(url, Port, BuildRequest(RequestHeader, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the passed RequestHeader and RequestBody, sends the request data using plain text to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendWebRequest(string url, int Port, string RequestHeader, string RequestBody, TimeSpan ReceiveTimeout)
        {
            string Response = SendTCPRequest(url, Port, BuildRequest(RequestHeader, RequestBody), ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the passed RequestHeader and RequestBody, sends the request data using plain text to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If request data is not fully sent within SendTimeout an Exception is thrown.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="SendTimeout">Maximum time allowed for request data to be fully sent.</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendWebRequest(string url, int Port, string RequestHeader, string RequestBody, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            string Response = SendTCPRequest(url, Port, BuildRequest(RequestHeader, RequestBody), SendTimeout, ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>No certificate is used in Request</remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string url, int Port)
        {
            string Response = SendTCPRequest(SSLProtocol, url, Port, BuildRequest(null, null));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="CertificateFile">Certificate to use in transaction</param>
        /// <param name="CertificatePassword">Password for Certificate authentication.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string url, int Port)
        {
            string Response = SendTCPRequest(SSLProtocol, CertificateFile, CertificatePassword, url, Port, BuildRequest(null, null));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string url, int Port)
        {
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(null, null));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>If passed RequestBody is empty or null, RequestBody property is used.</remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="CertificateFile">Certificate to use in transaction</param>
        /// <param name="CertificatePassword">Password for Certificate authentication.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string url, int Port, string RequestBody)
        {
            string Response = SendTCPRequest(SSLProtocol, CertificateFile, CertificatePassword, url, Port, BuildRequest(null, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>If passed RequestBody is empty or null, RequestBody property is used.</remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string url, int Port, string RequestBody)
        {
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(null, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="CertificateFile">Certificate to use in transaction</param>
        /// <param name="CertificatePassword">Password for Certificate authentication.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string url, int Port, string RequestHeader, string RequestBody)
        {
            string Response = SendTCPRequest(SSLProtocol, CertificateFile, CertificatePassword, url, Port, BuildRequest(RequestHeader, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string url, int Port, string RequestHeader, string RequestBody)
        {
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(RequestHeader, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="CertificateFile">Certificate to use in transaction</param>
        /// <param name="CertificatePassword">Password for Certificate authentication.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string url, int Port, string RequestHeader, string RequestBody, TimeSpan ReceiveTimeout)
        {
            string Response = SendTCPRequest(SSLProtocol, CertificateFile, CertificatePassword, url, Port, BuildRequest(RequestHeader, RequestBody), ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string url, int Port, string RequestHeader, string RequestBody, TimeSpan ReceiveTimeout)
        {
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(RequestHeader, RequestBody), ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If request data is not fully sent within SendTimeout an Exception is thrown.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="CertificateFile">Certificate to use in transaction</param>
        /// <param name="CertificatePassword">Password for Certificate authentication.</param>
        /// <param name="CertificateValidationCallback">Certificate Validation Callback if internal callback not used. Internal callback always accepts server certificate unless RequestTypes.DoNotAcceptServerCertificate is active.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="SendTimeout">Maximum time allowed for request data to be fully sent.</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, RemoteCertificateValidationCallback CertificateValidationCallback, string url, int Port, string RequestHeader, string RequestBody, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            certificateValidationCallback = CertificateValidationCallback;
            string Response = SendTCPRequest(SSLProtocol, CertificateFile, CertificatePassword, url, Port, BuildRequest(RequestHeader, RequestBody), SendTimeout, ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If request data is not fully sent within SendTimeout an Exception is thrown.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// <para>Default send and receive timeouts are used</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="CertificateValidationCallback">Certificate Validation Callback if internal callback not used. Internal callback always accepts server certificate unless RequestTypes.DoNotAcceptServerCertificate is active.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, RemoteCertificateValidationCallback CertificateValidationCallback, string url, int Port, string RequestHeader, string RequestBody)
        {
            certificateValidationCallback = CertificateValidationCallback;
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(RequestHeader, RequestBody));
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If request data is not fully sent within SendTimeout an Exception is thrown.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="CertificateValidationCallback">Certificate Validation Callback if internal callback not used. Internal callback always accepts server certificate unless RequestTypes.DoNotAcceptServerCertificate is active.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="SendTimeout">Maximum time allowed for request data to be fully sent.</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, RemoteCertificateValidationCallback CertificateValidationCallback, string url, int Port, string RequestHeader, string RequestBody, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            certificateValidationCallback = CertificateValidationCallback;
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(RequestHeader, RequestBody), SendTimeout, ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If request data is not fully sent within SendTimeout an Exception is thrown.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="CertificateFile">Certificate to use in transaction</param>
        /// <param name="CertificatePassword">Password for Certificate authentication.</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="SendTimeout">Maximum time allowed for request data to be fully sent.</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string url, int Port, string RequestHeader, string RequestBody, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            certificateValidationCallback = null;
            string Response = SendTCPRequest(SSLProtocol, CertificateFile, CertificatePassword, url, Port, BuildRequest(RequestHeader, RequestBody), SendTimeout, ReceiveTimeout);
            return DecodeResponse(Response);
        }

        /// <summary>
        /// Builds an HTTP request using the RequestHeader and RequestBody properties, sends the request data using secure sockets to the defined socket (URL:Port), receives and decodes the response,
        /// returning a Key/Value pair collection of the response.
        /// </summary>
        /// <remarks>
        /// <para>If passed RequestBody and/or RequestHeader are empty or null, RequestBody &amp; RequestHeader properties are used.</para>
        /// <para>If request data is not fully sent within SendTimeout an Exception is thrown.</para>
        /// <para>If response data is not fully received within ReceiveTimeout an Exception is thrown.</para>
        /// </remarks>
        /// <param name="SSLProtocol">SSL/TSL Protocols to be allowed in request</param>
        /// <param name="ClientCertificate">X509 Certificate to use in transaction</param>
        /// <param name="url">URL (IP Address) to send the request to.</param>
        /// <param name="Port">Port to send request to</param>
        /// <param name="RequestHeader">Header of HTTP Request to be sent</param>
        /// <param name="RequestBody">Body of HTTP request to be sent</param>
        /// <param name="SendTimeout">Maximum time allowed for request data to be fully sent.</param>
        /// <param name="ReceiveTimeout">Maximum time allowed for response data to be fully received.</param>
        /// <returns>Decodes response from HTTP request.</returns>
        public Dictionary<string, string> SendSSLWebRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string url, int Port, string RequestHeader, string RequestBody, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            certificateValidationCallback = null;
            string Response = SendTCPRequest(SSLProtocol, ClientCertificate, url, Port, BuildRequest(RequestHeader, RequestBody), SendTimeout, ReceiveTimeout);
            return DecodeResponse(Response);
        }

        private string DoTCPRequest(string URL, int Port, string request)
        {
            // Wrap the TCP Client in a using to ensure GC tears down the TCP port when we have finished.  Bit messy otherwise as we would
            // not be able to guarantee the port being closed.
            using (TcpClient tcpClient = new TcpClient())
            {
                // If we are logging information to a file, do it....  And add a line to the log so we can see the filename
                if (General.IsValueTrue(TestData.Repository["HTTPNonUI", "LogTransactions"]))
                {
                    string LogFileName = Path.Combine(Environment.CurrentDirectory, Path.GetFileName("request_" + ConvertURLToValidFilename(URL) + "_" + DateTime.Now.ToString("yy-MM-dd_HH-mm-ss-ff") + ".txt"));
                    Logger.WriteTextToFile(LogFileName, true, URL + ":" + Port.ToString() + "\r\n" + request);
                    Logger.WriteLine(Logger.LogLevels.TestInformation, string.Format("Send to [{0}] logged in [{1}]", URL, LogFileName));
                }

                // Connect to the TCP listener (We could get a TCP related exception thrown but we hope the caller will deal with it....) and
                // set the timeouts.
                tcpClient.Connect(URL, Port);

                if (sendTimeout.HasValue) tcpClient.SendTimeout = Convert.ToInt32(sendTimeout.Value.TotalMilliseconds);

                if (receiveTimeout.HasValue) tcpClient.ReceiveTimeout = Convert.ToInt32(receiveTimeout.Value.TotalMilliseconds);

                // Setup the Server certification callback incase this is an SSL call.  If caller has not setup its own callback handler (certificateValidationCallback is null)
                // we will deal with the callback using the ValidateServerCert delegate.
                RemoteCertificateValidationCallback validationCallback = certificateValidationCallback ?? new RemoteCertificateValidationCallback(ValidateServerCert);

                // We could be doing SSL OR HTTP.  We create an SSL stream or standard Network stream depending whether we are using ssl or not.
                dynamic stream;
                if (useSSL)
                    stream = (SslStream)new SslStream(tcpClient.GetStream(), false, validationCallback, null);
                else
                    stream = (NetworkStream)tcpClient.GetStream();

                // We wrap use of the stream in a using to be sure the stream is properly flushed when finished with.
                using (var IOStream = stream)
                using (var sw = new StreamWriter(IOStream))
                using (var sr = new StreamReader(IOStream))
                {
                    // If using SSL we need to do the authentication
                    if (useSSL)
                    {
                        X509Certificate2Collection xc = new X509Certificate2Collection();
                        if (ClientCertificate != null) xc.Add(ClientCertificate);
                        IOStream.AuthenticateAsClient(URL, xc, sslProtocol, false);
                        SSLProtocol = IOStream.SslProtocol;
                    }

                    sw.Write(request);
                    sw.Flush();

                    string response = sr.ReadToEnd();
                    if (General.IsValueTrue(TestData.Repository["HTTPNonUI", "LogTransactions"]))
                    {
                        string LogFileName = Path.Combine(Environment.CurrentDirectory, Path.GetFileName("response_" + ConvertURLToValidFilename(URL) + "_" + DateTime.Now.ToString("yy-MM-dd_HH-mm-ss-ff") + ".txt"));

                        Logger.WriteTextToFile(LogFileName, true, response);
                        Logger.WriteLine(Logger.LogLevels.TestInformation, string.Format("Reply from [{0}] logged in [{1}]", URL, LogFileName));
                    }

                    return response;
                }
            }
        }

        private string ConvertURLToValidFilename(string URL)
        {
            List<string> X = new List<string>();
            string rt = "";
            Regex r = new Regex(@"[a-z]+", RegexOptions.IgnoreCase);
            foreach (Match m in r.Matches(URL))
            {
                X.Add(m.Value);
            }
            for (int i = 0; i < X.Count; i++)
            {
                rt += X[i];
                rt += "_";
            }
            return rt;
        }

        private string SendTCPRequest(string URL, int Port, string request)
        {
            this.useSSL = false;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string URL, int Port, string request)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            ClientCertificate = new X509Certificate2(CertificateFile, CertificatePassword);
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string URL, int Port, string request)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            this.ClientCertificate = ClientCertificate;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, string URL, int Port, string request)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            this.ClientCertificate = null;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(string URL, int Port, string request, TimeSpan ReceiveTimeout)
        {
            this.useSSL = false;
            receiveTimeout = ReceiveTimeout;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string URL, int Port, string request, TimeSpan ReceiveTimeout)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            ClientCertificate = new X509Certificate2(CertificateFile, CertificatePassword);
            receiveTimeout = ReceiveTimeout;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string URL, int Port, string request, TimeSpan ReceiveTimeout)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            this.ClientCertificate = ClientCertificate;
            receiveTimeout = ReceiveTimeout;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(string URL, int Port, string request, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            useSSL = false;
            sendTimeout = SendTimeout;
            receiveTimeout = ReceiveTimeout;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, string CertificateFile, string CertificatePassword, string URL, int Port, string request, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            ClientCertificate = new X509Certificate2(CertificateFile, CertificatePassword);
            sendTimeout = SendTimeout;
            receiveTimeout = ReceiveTimeout;
            return DoTCPRequest(URL, Port, request);
        }

        private string SendTCPRequest(SslProtocols SSLProtocol, X509Certificate2 ClientCertificate, string URL, int Port, string request, TimeSpan SendTimeout, TimeSpan ReceiveTimeout)
        {
            useSSL = true;
            this.sslProtocol = SSLProtocol;
            this.ClientCertificate = ClientCertificate;
            sendTimeout = SendTimeout;
            receiveTimeout = ReceiveTimeout;
            return DoTCPRequest(URL, Port, request);
        }

        private string BuildRequest(string RequestHeader, string RequestBody)
        {
            // Stitch the HTTP Request Body to the HTTP Request Header.  HTTP requires a double CRLF between the header and the body
            // If the Header we have been passed is empty, use the class RequestHeader property (which itself may be empty...)  This way, a user may build the HTTP Header
            // themselves...
            string request = string.IsNullOrEmpty(RequestHeader) ? this.RequestHeader : RequestHeader;

            if (string.IsNullOrEmpty(request))
            {
                // If the request has no header, we can just use the passed Request Body.
                request = (string.IsNullOrEmpty(RequestBody) ? "" : RequestBody);
            }
            else
            {
                // If we do have an HTTP header, ensure there is a double CRLF before the Body (if any...)
                if (!request.EndsWith("\n") & !request.EndsWith("\r")) request += "\r\n";
                request += "\r\n" + (string.IsNullOrEmpty(RequestBody) ? this.RequestBody : RequestBody);
            }

            return request;
        }

        private Dictionary<string, string> DecodeResponse(string rawData)
        {
            var returnData = new Dictionary<string, string>();

            try
            {
                // Do First line (IE. HTTP/1.1 200 OK)
                if (string.IsNullOrEmpty(rawData) || string.IsNullOrWhiteSpace(rawData))
                {
                    returnData.Add("HTTPVersion", "Unknown - Empty Response");
                    returnData.Add("StatusCode", "Unknown - Empty Response");
                    return returnData;
                }

                // We have something.....  Is it HTTP?
                if (!rawData.StartsWith("HTTP"))
                {
                    string FirstLine = rawData.Split('\r')[0];
                    FirstLine = (FirstLine.Length >= 20) ? FirstLine.Substring(0, 17) + "..." : FirstLine;
                    returnData.Add("HTTPVersion", string.Format("Unknown - Response not HTTP: FirstLine = [{0}]", FirstLine));
                    returnData.Add("StatusCode", "Unknown - Response not HTTP");
                    return returnData;
                }

                // Get the header out first....
                string HeaderArea = rawData.Substring(0, rawData.IndexOf("\r\n\r\n"));

                // And the HTML body
                string BodyArea = rawData.Substring(rawData.IndexOf("\r\n\r\n") + 4, rawData.Length - rawData.IndexOf("\r\n\r\n") - 4);

                // Split & check first line
                string[] FirstLineSplit = HeaderArea.Split('\r')[0].Split(' ');
                if (FirstLineSplit.Length < 3 || !FirstLineSplit[0].Contains('/'))
                {
                    string FirstLine = HeaderArea.Split('\r')[0];
                    FirstLine = (FirstLine.Length >= 20) ? FirstLine.Substring(0, 17) + "..." : FirstLine;
                    returnData.Add("HTTPVersion", string.Format("Unknown - Response header top line not in correct format: [{0}]", FirstLine));
                    returnData.Add("StatusCode", "Unknown - Response not formatted correctly");
                    return returnData;
                }

                // Finally, we can process the first line....
                returnData.Add("HTTPVersion", FirstLineSplit[0].Split('/')[1]);
                returnData.Add("StatusCode", FirstLineSplit[1]);
                string StatusText = null;
                for (int index = 2; index < FirstLineSplit.Length; index++) StatusText = StatusText + " " + FirstLineSplit[index];
                StatusText = StatusText.Trim();
                returnData.Add("StatusText", StatusText);

                // And do the rest of the header...  We do a for loop as we want to ignore the top line; it is just the HTTP protocol and version info
                string[] HeaderSplit = HeaderArea.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for (int index = 1; index < HeaderSplit.Length; index++)
                {
                    if (!HeaderSplit[index].Contains(':'))
                        throw new InvalidHTTPResponse("Response contained invalid header line [{0}]. No colon (:) present: [{1}]", index.ToString(), HeaderSplit[index]);
                    else
                        returnData.Add(HeaderSplit[index].Split(':')[0].Trim(), HeaderSplit[index].Split(':')[1].Trim());
                }

                // And finally the body...
                // First, we need to know if the body is chunked. It if is we need to de-chunk it....
                if (returnData.ContainsKey("Transfer-Encoding") && returnData["Transfer-Encoding"] == "chunked")
                {
                    // So, we need to dechunk the data.....
                    // Data is chunked as follows
                    // <Number of characters in hexaecimal>\r\n
                    // <Characters in chunk>\r\n
                    // this repeats until;
                    // 0\r\n
                    // \r\n
                    bool bDechunkingFinished = false;
                    string workingBody = string.Empty;
                    string chunkHex;
                    int iChunkLength;
                    while (!bDechunkingFinished)
                    {
                        // Itterates through the chunked body area
                        // Get the Chunk HEX
                        chunkHex = BodyArea.Substring(0, BodyArea.IndexOf("\r\n"));
                        BodyArea = BodyArea.Substring(chunkHex.Length + 2, BodyArea.Length - (chunkHex.Length + 2));

                        if (!int.TryParse(chunkHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out iChunkLength))
                        {
                            throw new InvalidHTTPResponse("[HTTP]DecodeResponse: Fatal error decoding chunked html body. Parsing Hex [{0}] failed)", chunkHex);
                        }

                        if (iChunkLength == 0) break;

                        workingBody += BodyArea.Substring(0, iChunkLength);
                        BodyArea = BodyArea.Substring(iChunkLength, BodyArea.Length - iChunkLength);

                        if (!BodyArea.StartsWith("\r\n"))
                        {
                            InvalidHTTPResponse ex = new InvalidHTTPResponse("[HTTP]DecodeResponse: Fatal error decoding chunked html body. End of chunk length not CRLF!)", iChunkLength);
                            ex.Data.Add("Chunk Length", iChunkLength);
                            ex.Data.Add("Chunk Data", BodyArea);
                            throw ex;
                        }

                        BodyArea = BodyArea.Substring(2, BodyArea.Length - 2);
                    }
                    returnData.Add("Body", workingBody);
                }
                else
                {
                    // No chunked so just grab the body
                    returnData.Add("Body", BodyArea);
                }                
            }
            catch (Exception ex)
            {
                throw new InvalidHTTPResponse("[HTTP]DecodeResponse: Fatal error decoding raw response string header)", ex);
            }

            return returnData;
        }
        private bool ValidateServerCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return (RequestType != RequestTypes.DoNotAcceptServerCertificate);
        }
    }
}
