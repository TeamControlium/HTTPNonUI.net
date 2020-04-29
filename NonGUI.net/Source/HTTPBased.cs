// <copyright file="HTTPBased.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.NonGUI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Net.Security;
    using System.Runtime.Remoting;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using TeamControlium.Utilities;
    using static TeamControlium.Utilities.Log;
    using static TeamControlium.Utilities.Repository;

    /// <summary>
    /// Provides test oriented functionality for interacting with HTTP based protocols
    /// </summary>
    /// <remarks>
    /// <see cref="HTTPBased"/> uses a number of Repository items for setting of execution aspects.  The items (Category/Item-name) and their default values are listed below.  To change the default, set the item/s as
    /// required.
    /// EG.
    /// <code>Repository.Item[""]</code>
    /// <list type="table">
    /// <listheader>
    /// <term>Category</term><term>Item</term><term>Type</term><term>Default Value</term><term>Comments</term>
    /// </listheader>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term>TCP_TransactionsLogFile</term>string<term></term><term>null</term><term>If transaction logging required, contains full path &amp; filename for logging of HTTP/TCP transactions.</term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term>SSLProtocol</term><term>SslProtocols</term><term>SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls</term><term>SSL Protocol to be used.  We default to allowing any TLS</term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term>SSLPort</term><term>int</term><term>443</term><term>Port used for SSL tunnelled HTTP (HTTPS) communication</term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term>HTTPPort</term><term>int</term><term>80</term><term>Port used for unsecure HTTP communication</term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term>HTTPHeader_PostText</term><term>string</term><term>POST</term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term>string</term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term></term><term></term><term></term>
    /// </item>
    /// <item>
    /// <term>TeamControlium.NonGUI</term><term></term><term></term><term></term><term></term>
    /// </item>
    /// </list>
    /// </remarks>
    public class HTTPBased
    {
        /// <summary>
        /// TCP object representing TCB layer of connection for TCP based interactions.
        /// </summary>
        private TCPBased tcpRequest;

        /// <summary>
        /// Stores instantiated (by constructor) FullHTTPRequest object
        /// </summary>
        private FullHTTPRequest httpRequest;

        /// <summary>
        /// Initialises a new instance of the <see cref="HTTPBased" /> class. Used for testing an HTTP interface when used for Non-UI interaction (IE. WebServices, JSON etc...)
        /// </summary>
        public HTTPBased()
        {
            if (!string.IsNullOrEmpty(this.TransactionsLogFile))
            {
                General.WriteTextToFile(this.TransactionsLogFile, General.WriteMode.Append, $"NonGUI Instantiated at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}");
                LogWriteLine(LogLevels.FrameworkInformation, $"Writing NonGUI transactions to LogFile > {this.TransactionsLogFile}");
            }

            this.UseSSL = false;
            this.Domain = string.Empty;
            this.httpRequest = new FullHTTPRequest();
            this.tcpRequest = new TCPBased();
            this.tcpRequest.ClientCertificate = this.ClientCertificate;
        }

        /// <summary>
        /// Possible HTTP Methods
        /// </summary>
        public enum HTTPMethods
        {
            /// <summary>
            /// HTTP GET Method defined when building Request Header.
            /// The POST method is used to submit an entity to the specified resource, often causing a change in state or side effects on the server.
            /// </summary>
            Post,

            /// <summary>
            /// HTTP POST Method defined when building Request Header.
            /// The GET method requests a representation of the specified resource. Requests using GET should only retrieve data
            /// </summary>
            Get,

            /// <summary>
            /// HTTP HEAD Method defined when building Request Header.
            /// The HEAD method asks for a response identical to that of a GET request, but without the response body.
            /// </summary>
            Head,

            /// <summary>
            /// HTTP PUT Method defined when building Request Header.
            /// The PUT method replaces all current representations of the target resource with the request payload.
            /// </summary>
            Put,

            /// <summary>
            /// HTTP DELETE Method defined when building Request Header.
            /// The DELETE method deletes the specified resource.
            /// </summary>
            Delete,

            /// <summary>
            /// HTTP CONNECT Method defined when building Request Header.
            /// The CONNECT method establishes a tunnel to the server identified by the target resource.
            /// </summary>
            Connect,

            /// <summary>
            /// HTTP OPTIONS Method defined when building Request Header.
            /// The OPTIONS method is used to describe the communication options for the target resource.
            /// </summary>
            Options,

            /// <summary>
            /// HTTP TRACE Method defined when building Request Header.
            /// The TRACE method performs a message loop-back test along the path to the target resource.
            /// </summary>
            Trace,

            /// <summary>
            /// HTTP PATCH Method defined when building Request Header.
            /// The PATCH method is used to apply partial modifications to a resource.
            /// </summary>
            Patch
        }

        /// <summary>
        /// If transaction logging required, contains full path &amp; filename for logging of HTTP/TCP transactions.
        /// </summary>
        public string TransactionsLogFile => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "TCP_TransactionsLogFile", null);

        /// <summary>
        /// SSL Protocol to use.  We default to allowing any TLS
        /// </summary>
        public SslProtocols SSLProtocol => GetItemLocalOrDefault<SslProtocols>("TeamControlium.NonGUI", "SSLProtocol", SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls);

        /// <summary>
        /// TCP Port used for SSL (https://) based communications.  Is usually port 443.
        /// </summary>
        public int SSLPort => GetItemLocalOrDefault<int>("TeamControlium.NonGUI", "SSLPort", 443);

        /// <summary>
        /// TCP Port used for unsecure HTTP (http://) based communications.  Is usually port 80.
        /// </summary>
        public int HTTPPort => GetItemLocalOrDefault<int>("TeamControlium.NonGUI", "HTTPPort", 80);

        /// <summary>
        /// Gets last exception thrown in a Try... method.
        /// </summary>
        public Exception TryException { get; private set; } = null;

        /// <summary>
        /// Gets or sets X509 Certificate to use with SSL based communications (If required)
        /// </summary>
        public X509Certificate2 ClientCertificate { get; set; } = null;

        /// <summary>
        /// Gets or sets Call-back delegate for custom/test-based validations of Server certificates.  Can be used for Server Certificate logging, negative testing etc...
        /// </summary>
        public RemoteCertificateValidationCallback CertificateValidationCallback
        {
            get
            {
                return this.tcpRequest?.CertificateValidationCallback ?? null;
            }

            set
            {
                this.tcpRequest.CertificateValidationCallback = value;
            }
        }

        /// <summary>
        /// Gets full URL used in last HTTP Request
        /// </summary>
        public string ActualURL { get; private set; } = null;

        /// <summary>
        /// Gets payload (HTTP Header and body) used in last HTTP Request
        /// </summary>
        public string ActualPayload { get; private set; } = null;

        /// <summary>
        /// Gets raw HTTP Response string from last HTTP Request 
        /// </summary>
        public string ActualRawResponse { get; private set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether a secure SSL (HTTPS) connection is to be used
        /// </summary>
        public bool UseSSL { get; set; }

        /// <summary>
        /// Sets signed (if required) X509Certificate to be used in an HTTPS Request if required.
        /// </summary>
        /// <remarks>
        /// If Certificate requires signing (IE. With a Password), it is the responsibility of caller to do this Prior to usage. <see cref="CertificatePassword"/> is ONLY
        /// used for signing <see cref="CertificateFilename"/>.<br/>
        /// If both <see cref="Certificate"/> and <see cref="CertificateFilename"/> are defined, <see cref="Certificate"/> is used.</remarks>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Get is private and not documented")] 
        public X509Certificate2 Certificate { private get; set; } = null;

        /// <summary>
        /// Sets full path and Filename of X509 Certificate to be used in an HTTPS Request if required.
        /// </summary>
        /// <remarks>If both <see cref="Certificate"/> and <see cref="CertificateFilename"/> are defined, <see cref="Certificate"/> is used.</remarks>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Get is private and not documented")]
        public string CertificateFilename { private get; set; } = null;

        /// <summary>
        /// Sets password to be used with defined <see cref="CertificateFilename"/> if required.
        /// </summary>
        /// <remarks>If <see cref="Certificate"/> is defined (which takes precedence over <see cref="CertificateFilename"/>) <see cref="CertificateFilename"/> is
        /// NOT used - it is callers responsibility to sign <see cref="Certificate"/> if being used.</remarks>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:PropertySummaryDocumentationMustMatchAccessors", Justification = "Get is private and not documented")]
        public string CertificatePassword { private get; set; } = null;

        /// <summary>
        /// Gets or sets the HTTP Domain
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Gets or sets the HTTP Method
        /// </summary>
        public HTTPMethods? HTTPMethod
        {
            get
            {
                return this.httpRequest.HTTPMethod;
            }

            set
            {
                this.httpRequest.HTTPMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP Resource Path
        /// </summary>
        public string ResourcePath
        {
            get
            {
                return this.httpRequest.ResourcePath;
            }

            set
            {
                this.httpRequest.ResourcePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP Query string that is used on top line of HTTP Header
        /// </summary>
        public string QueryString
        {
            get
            {
                return this.httpRequest.GetQueryAsString();
            }

            set
            {
                this.httpRequest.SetQueryParameters(value);
            }
        }

        /// <summary>
        /// Gets or sets the HTTP Header as a string
        /// </summary>
        public string HeaderString
        {
            get
            {
                return this.httpRequest.GetHeaderAsString();
            }

            set
            {
                this.httpRequest.SetHeader(value);
            }
        }

        /// <summary>
        /// Gets or sets the HTTP body
        /// </summary>
        public string Body
        {
            get
            {
                return this.httpRequest.Body;
            }

            set
            {
                this.httpRequest.Body = value;
            }
        }

        /// <summary>
        /// Gets raw Response text of HTTP Request.  Data is raw HTTP response.
        /// </summary>
        public string ResponseRaw { get; private set; }

        /// <summary>
        /// Performs an HTTP GET to the required Domain/ResourcePath containing given HTTP Query-string Header and Body (if given)
        /// </summary>
        /// <param name="domain">Domain to POST to.  IE. postman-echo.com</param>
        /// <param name="resourcePath">Resource path at domain.  IE. /get</param>
        /// <param name="query">Query string.  IE. foo1=bar1&amp;foo2=bar2</param>
        /// <param name="header">HTTP Header items, not including top line (IE. HTTP Method, resource, version</param>
        /// <param name="body">HTTP Body - Usually null for an HTTP GET but can be populated for testing purposes etc.</param>
        /// <returns>Processed HTTP Response</returns>
        /// <remarks>
        /// Content-Length header item is automatically added (or, if exists in given header, modified) during building of Request.
        /// If Connection keep-alive is used, currently request will time-out on waiting for response.  Intelligent and async functionality needs building in.
        /// Aspects of request (such as port, header/request layout etc.) can be modified using settings stored in Repository.  See <see cref="HTTPBased"/>
        /// and documentation for details of Repository items referenced.
        /// <br/>
        /// Processed HTTP Response is passed back as a collection of Name/Value pairs.  The following raw HTTP response is converted;
        /// <code>
        /// HTTP/1.1 200 OK<br/>
        /// Cache-Control: private, max-age=0<br/>
        /// Content-Length: 240<br/>
        /// Content-Type: application/json; charset=utf-8<br/>
        /// Server: nginx<br/>
        /// ETag: W/"f0-EYtfNu+sVmscSzVVghi5p8EfJsA"<br/>
        /// Vary: Accept-Encoding<br/>
        /// Access-Control-Allow-Methods: GET, POST<br/>
        /// Access-Control-Allow-Headers: content-type<br/>
        /// Access-Control-Allow-Credentials: true<br/>
        /// Strict-Transport-Security: max-age=31536000<br/>
        /// Date: Thu, 16 Apr 2020 01:08:29 GMT<br/>
        /// Connection: close<br/>
        /// <br/>
        /// {<br/>
        ///   "args":{<br/>
        ///     "foo1":"bar1",<br/>
        ///     "foo2":"bar2"<br/>
        ///   },<br/>
        ///   "headers":{<br/>
        ///     "x-forwarded-proto":"https",<br/>
        ///     "host":"postman-echo.com",<br/>
        ///     "accept-encoding":"identity",<br/>
        ///     "content-type":"text/xml",<br/>
        ///     "x-forwarded-port":"80"<br/>
        ///   },<br/>
        ///   "url":"https://postman-echo.com/get?foo1=bar1&amp;foo2=bar2"<br/>
        /// }
        /// </code>
        /// Most items are self-explanatory - See <see cref="HttpPOST(string, string, string, string, string)"/> for details.
        /// Note.  HTTP Content-Length header item in request will automatically be added (or updated if already in header)
        /// </remarks>
        public ItemList HttpGET(string domain, string resourcePath, string query, string header, string body = null)
        {
            return this.Http(HTTPMethods.Get, domain, resourcePath, query, header, body);
        }

        /// <summary>
        /// Performs an HTTP POST to the required Domain/ResourcePath containing given HTTP Header and Body
        /// </summary>
        /// <param name="domain">Domain to POST to.</param>
        /// <param name="resourcePath">Resource path at domain.</param>
        /// <param name="query">Query string.  For a POST this should be empty or null.  Here for test purposes.</param>
        /// <param name="header">HTTP Header items, not including top line (IE. HTTP Method, resource, version</param>
        /// <param name="body">HTTP Body</param>
        /// <returns>Processed HTTP Response</returns>
        /// <remarks>
        /// Content-Length header item is automatically added (or, if exists in given header, modified) during building of Request.
        /// If Connection keep-alive is used, currently request will time-out on waiting for response.  Intelligent and async functionality needs building in.
        /// Aspects of request (such as port, header/request layout etc.) can be modified using settings stored in Repository.  See <see cref="HTTPBased"/>
        /// and documentation for details of Repository items referenced.
        /// <list type="table">
        /// Processed HTTP Response is passed back as a collection of Name/Value pairs.  The following raw HTTP response is converted;
        /// <code>
        /// HTTP/1.1 200 OK<br/>
        /// Cache-Control: private, max-age=0<br/>
        /// Content-Length: 346<br/>
        /// Content-Type: text/xml; charset=utf-8<br/>
        /// Server: Server<br/>
        /// Web-Service: DataFlex 18.1<br/>
        /// Access-Control-Allow-Origin: http://www.dataaccess.com<br/>
        /// Access-Control-Allow-Methods: GET, POST<br/>
        /// Access-Control-Allow-Headers: content-type<br/>
        /// Access-Control-Allow-Credentials: true<br/>
        /// Strict-Transport-Security: max-age=31536000<br/>
        /// Date: Tue, 07 Apr 2020 22:16:05 GMT<br/>
        /// Connection: close<br/>
        /// <br/>
        /// &lt;?xml version="1.0" encoding="utf-8"?&gt;<br/>
        /// &lt;soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"&gt;<br/>
        ///   &lt;soap:Body&gt;<br/>
        ///     &lt;m:NumberToWordsResponse xmlns:m="http://www.dataaccess.com/webservicesserver/"&gt;<br/>
        ///       &lt;m:NumberToWordsResult&gt;seventy eight &lt;/m:NumberToWordsResult&gt;<br/>
        ///     &lt;/m:NumberToWordsResponse&gt;<br/>
        ///   &lt;/soap:Body&gt;<br/>
        /// &lt;/soap:Envelope&gt;
        /// </code>
        /// Most items are self-explanatory;
        /// <listheader>
        /// <term>Item Name</term><term>Example</term><term>Comments</term>
        /// </listheader>
        /// <item>
        /// <term>HTTPVersion</term><term>1.1</term><term>HTTP Version - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>StatusCode</term><term>200</term><term>Status code - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>StatusText</term><term>OK</term><term>Status text - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Cache-Control</term><term>private; max-age=0</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Content-Length</term><term>346</term><term>Number of characters in Body - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Content-Type</term><term>text/xml; charset=utf-8</term><term>Type of body data - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Server</term><term>Server</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Web-Service</term><term>DataFlex 18.1</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Origin</term><term>http</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Methods</term><term>GET</term><term>POST</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Headers</term><term>content-type</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Credentials</term><term>true</term><term></term>
        /// </item>
        /// <item>
        /// <term>Strict-Transport-Security</term><term>max-age=31536000</term><term></term>
        /// </item>
        /// <item>
        /// <term>Date</term><term>Tue, 07 Apr 2020 22</term><term>Server date</term>
        /// </item>
        /// <item>
        /// <term>Connection</term><term>close</term><term>Indicates what state the Server will hold connection at end of response - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Body</term><term>&lt;http&gt;&lt;body&gt;hello&lt;/body&gt;&lt;/http&gt;</term><term>Raw body of HTTP Response</term>
        /// </item>
        /// </list>
        /// Note.  HTTP Content-Length header item in request will automatically be added (or updated if already in header)
        /// </remarks>
        public ItemList HttpPOST(string domain, string resourcePath, string query, string header, string body)
        {
            return this.Http(HTTPMethods.Post, domain, resourcePath, query, header, body);
        }

        /// <summary>
        /// Performs an HTTP POST to the required Domain/ResourcePath containing given HTTP Header and Body
        /// </summary>
        /// <param name="domain">Domain to POST to.</param>
        /// <param name="resourcePath">Resource path at domain.</param>
        /// <param name="header">HTTP Header items, not including top line</param>
        /// <param name="body">HTTP Body</param>
        /// <param name="response">Processed HTTP Response if successful, null if not</param>
        /// <returns>True if success or false if not.  If successful exception thrown can be got from <see cref="TryException"/> </returns>
        /// <remarks>
        /// Content-Length header item is automatically added (or, if exists in given header, modified) during building of Request.
        /// If Connection keep-alive is used, currently request will time-out on waiting for response.  Intelligent and async functionality needs building in.
        /// Aspects of request (such as port, header/request layout etc.) can be modified using settings stored in Repository.  See <see cref="HTTPBased"/>
        /// and documentation for details of Repository items referenced.
        /// <list type="table">
        /// Processed HTTP Response is passed back as a collection of Name/Value pairs.  The following raw HTTP response is converted;
        /// <code>
        /// HTTP/1.1 200 OK<br/>
        /// Cache-Control: private, max-age=0<br/>
        /// Content-Length: 346<br/>
        /// Content-Type: text/xml; charset=utf-8<br/>
        /// Server: Server<br/>
        /// Web-Service: DataFlex 18.1<br/>
        /// Access-Control-Allow-Origin: http://www.dataaccess.com<br/>
        /// Access-Control-Allow-Methods: GET, POST<br/>
        /// Access-Control-Allow-Headers: content-type<br/>
        /// Access-Control-Allow-Credentials: true<br/>
        /// Strict-Transport-Security: max-age=31536000<br/>
        /// Date: Tue, 07 Apr 2020 22:16:05 GMT<br/>
        /// Connection: close<br/>
        /// <br/>
        /// &lt;?xml version="1.0" encoding="utf-8"?&gt;<br/>
        /// &lt;soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"&gt;<br/>
        ///   &lt;soap:Body&gt;<br/>
        ///     &lt;m:NumberToWordsResponse xmlns:m="http://www.dataaccess.com/webservicesserver/"&gt;<br/>
        ///       &lt;m:NumberToWordsResult&gt;seventy eight &lt;/m:NumberToWordsResult&gt;<br/>
        ///     &lt;/m:NumberToWordsResponse&gt;<br/>
        ///   &lt;/soap:Body&gt;<br/>
        /// &lt;/soap:Envelope&gt;
        /// </code>
        /// Most items are self-explanatory;
        /// <listheader>
        /// <term>Item Name</term><term>Example</term><term>Comments</term>
        /// </listheader>
        /// <item>
        /// <term>HTTPVersion</term><term>1.1</term><term>HTTP Version - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>StatusCode</term><term>200</term><term>Status code - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>StatusText</term><term>OK</term><term>Status text - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Cache-Control</term><term>private; max-age=0</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Content-Length</term><term>346</term><term>Number of characters in Body - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Content-Type</term><term>text/xml; charset=utf-8</term><term>Type of body data - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Server</term><term>Server</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Web-Service</term><term>DataFlex 18.1</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Origin</term><term>http</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Methods</term><term>GET</term><term>POST</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Headers</term><term>content-type</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Credentials</term><term>true</term><term></term>
        /// </item>
        /// <item>
        /// <term>Strict-Transport-Security</term><term>max-age=31536000</term><term></term>
        /// </item>
        /// <item>
        /// <term>Date</term><term>Tue, 07 Apr 2020 22</term><term>Server date</term>
        /// </item>
        /// <item>
        /// <term>Connection</term><term>close</term><term>Indicates what state the Server will hold connection at end of response - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Body</term><term>&lt;http&gt;&lt;body&gt;hello&lt;/body&gt;&lt;/http&gt;</term><term>Raw body of HTTP Response</term>
        /// </item>
        /// </list>
        /// Note.  HTTP Content-Length header item in request will automatically be added (or updated if already in header)
        /// </remarks>
        public bool TryHttpPOST(string domain, string resourcePath, string header, string body, out ItemList response)
        {
            try
            {
                FullHTTPRequest httpPayload = new FullHTTPRequest(HTTPMethods.Post, resourcePath, (string)null, header, body);
                response = this.DecodeResponse(this.DoHTTPRequest(domain, this.UseSSL ? this.SSLPort : this.HTTPPort, httpPayload, true));
                return true;
            }
            catch (Exception ex)
            {
                this.TryException = ex;
                response = null;
                return false;
            }
        }

        /// <summary>
        /// Sends an HTTP Request with the given request method and set properties then returns response.
        /// </summary>
        /// <param name="setContentLength">Optional parameter (default true) - Indicates if HTTP Content-Length header item should automatically be added/updated or not</param>
        /// <returns>Response returned from request</returns>
        /// <remarks>
        /// If there is a timeout an Exception will be logged and thrown.<br/>
        /// If the header Connection: keep-alive is used this WILL currently result in a timeout.<br/>
        /// </remarks>
        public ItemList Http(bool setContentLength = true)
        {
            string responseString = string.Empty;

            if (string.IsNullOrWhiteSpace(this.Domain))
            {
                throw new Exception($" HTTP {this.httpRequest.HTTPMethod}: Invalid Domain.  Expect xxx.xxx.xxx etc..  Have [{this.Domain}]");
            }

            return this.DecodeResponse(this.DoHTTPRequest(this.Domain, this.UseSSL ? this.SSLPort : this.HTTPPort, this.httpRequest, setContentLength));
        }

        /// <summary>
        /// Sends an HTTP Request with the and set properties then returns response.
        /// </summary>
        /// <param name="response">If successful, Response returned from request otherwise null. </param>
        /// <param name="setContentLength">Optional parameter (default true) - Indicates if HTTP Content-Length header item should automatically be added/updated or not</param>
        /// <returns>True if successful, otherwise false.  If false, <see cref="TryException"/> contains exception thrown</returns>
        /// <remarks>
        /// If there is a timeout an Exception will be logged and false returned.<br/>
        /// If the header Connection: keep-alive is used this WILL currently result in a timeout.<br/>
        /// The Header item 'Content-Length' will automatically be added (or updated with actual Body length if set already)
        /// </remarks>
        public bool TryHttp(out ItemList response, bool setContentLength = true)
        {
            try
            {
                response = this.Http(setContentLength);
                return true;
            }
            catch (Exception ex)
            {
                this.TryException = ex;
                response = null;
                return false;
            }
        }

        /// <summary>
        /// Sends an HTTP Request with the given request method and parameters
        /// </summary>
        /// <param name="method">Required method.  See <see cref="HTTPMethods"/></param>
        /// <param name="domain">Domain to send required to</param>
        /// <param name="resourcePath">Resource path</param>
        /// <param name="query">Query-string to use</param>
        /// <param name="header">HTTP Header part of request</param>
        /// <param name="body">HTTP Body part of request</param>
        /// <param name="setContentLength">Optional parameter (default true) - Indicates if HTTP Content-Length header item should automatically be added/updated or not</param>
        /// <returns>Response returned from request</returns>
        /// <remarks>
        /// If there is a timeout an Exception will be logged and thrown.<br/>
        /// If the header Connection: keep-alive is used this WILL currently result in a timeout.<br/>
        /// The Header item 'Content-Length' will automatically be added (or updated with actual Body length if set already)
        /// </remarks>
        public ItemList Http(HTTPMethods method, string domain, string resourcePath, string query, string header, string body, bool setContentLength = true)
        {
            FullHTTPRequest httpPayload = new FullHTTPRequest(method, resourcePath, query, header, body);
            return this.DecodeResponse(this.DoHTTPRequest(domain, this.UseSSL ? this.SSLPort : this.HTTPPort, httpPayload, setContentLength));
        }

        /// <summary>
        /// Sends an HTTP Request with the given request method and parameters
        /// </summary>
        /// <param name="method">Required method.  See <see cref="HTTPMethods"/></param>
        /// <param name="domain">Domain to send required to</param>
        /// <param name="resourcePath">Resource path</param>
        /// <param name="query">Query-string to use</param>
        /// <param name="header">HTTP Header part of request</param>
        /// <param name="body">HTTP Body part of request</param>
        /// <param name="response">If successful, Response returned from request otherwise null. </param>
        /// <param name="setContentLength">Optional parameter (default true) - Indicates if HTTP Content-Length header item should automatically be added/updated or not</param>
        /// <returns>True if successful, otherwise false.  If false, <see cref="TryException"/> contains exception thrown</returns>
        /// <remarks>
        /// If there is a timeout an Exception will be logged and false returned.<br/>
        /// If the header Connection: keep-alive is used this WILL currently result in a timeout.<br/>
        /// The Header item 'Content-Length' will automatically be added (or updated with actual Body length if set already)
        /// </remarks>
        public bool TryHttp(HTTPMethods method, string domain, string resourcePath, string query, string header, string body, out ItemList response, bool setContentLength = true)
        {
            try
            {
                response = this.Http(method, domain, resourcePath, query, header, body, setContentLength);
                return true;
            }
            catch (Exception ex)
            {
                this.TryException = ex;
                response = null;
                return false;
            }
        }

        /// <summary>
        /// Performs an HTTP POST to the required Domain/ResourcePath containing given HTTP Header and Body
        /// </summary>
        /// <param name="response">Processed HTTP Response if successful or null if not</param>
        /// <returns>True if successful, false if not.  See <see cref="TryException"/> for exception in case of false.</returns>
        /// <remarks>
        /// <list type="table">
        /// POST Parameters are obtained from properties.  Examples are based on an HTTP Post to http://www.mypostexample.com/path/to/resource.wso?para1=data1#param1=data2
        /// <listheader>
        /// <term>Property</term><term>Example</term><term>Comments</term>
        /// </listheader>
        /// <item>
        /// <term><see cref="Domain"/></term><term>www.mypostexample.com</term><term>Domain to Post to</term>
        /// </item>
        /// <item>
        /// <term><see cref="ResourcePath"/></term><term>/path/to/resource.wso</term><term>Resource Path</term>
        /// </item>
        /// <item>
        /// <term><see cref="QueryString"/></term><term>para1=data1&amp;param1=data2</term><term>Query String</term>
        /// </item>
        /// <item>
        /// <term><see cref="HeaderString"/></term><term>Accept: */*\r\nHost: www.mypostexample.com\r\nAccept-Encoding: identity\r\nConnection: close\r\n</term><term>Header items String</term>
        /// </item>
        /// <item>
        /// <term><see cref="Body"/></term><term>&lt;?xml version=\"1.0\"?&gt;&lt;s11:Env...oWords&gt;&lt;/s11:Body&gt;&lt;/s11:Envelope&gt;";</term><term>Body String</term>
        /// </item>
        /// </list>
        /// Notes.<br/>
        /// Content-Length header item is automatically added (or, if exists in given header, modified) during building of Request.
        /// If Connection keep-alive is used, currently request will time-out on waiting for response.  Intelligent and async functionality needs building in.
        /// Aspects of request (such as port, header/request layout etc.) can be modified using settings stored in Repository.  See <see cref="HTTPBased"/>
        /// and documentation for details of Repository items referenced.
        /// <list type="table">
        /// Processed HTTP Response is passed back as a collection of Name/Value pairs.  The following raw HTTP response is converted;
        /// <code>
        /// HTTP/1.1 200 OK<br/>
        /// Cache-Control: private, max-age=0<br/>
        /// Content-Length: 346<br/>
        /// Content-Type: text/xml; charset=utf-8<br/>
        /// Server: Server<br/>
        /// Web-Service: DataFlex 18.1<br/>
        /// Access-Control-Allow-Origin: http://www.dataaccess.com<br/>
        /// Access-Control-Allow-Methods: GET, POST<br/>
        /// Access-Control-Allow-Headers: content-type<br/>
        /// Access-Control-Allow-Credentials: true<br/>
        /// Strict-Transport-Security: max-age=31536000<br/>
        /// Date: Tue, 07 Apr 2020 22:16:05 GMT<br/>
        /// Connection: close<br/>
        /// <br/>
        /// &lt;?xml version="1.0" encoding="utf-8"?&gt;<br/>
        /// &lt;soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"&gt;<br/>
        ///   &lt;soap:Body&gt;<br/>
        ///     &lt;m:NumberToWordsResponse xmlns:m="http://www.dataaccess.com/webservicesserver/"&gt;<br/>
        ///       &lt;m:NumberToWordsResult&gt;seventy eight &lt;/m:NumberToWordsResult&gt;<br/>
        ///     &lt;/m:NumberToWordsResponse&gt;<br/>
        ///   &lt;/soap:Body&gt;<br/>
        /// &lt;/soap:Envelope&gt;
        /// </code>
        /// Most items are self-explanatory;
        /// <listheader>
        /// <term>Item Name</term><term>Example</term><term>Comments</term>
        /// </listheader>
        /// <item>
        /// <term>HTTPVersion</term><term>1.1</term><term>HTTP Version - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>StatusCode</term><term>200</term><term>Status code - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>StatusText</term><term>OK</term><term>Status text - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Cache-Control</term><term>private; max-age=0</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Content-Length</term><term>346</term><term>Number of characters in Body - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Content-Type</term><term>text/xml; charset=utf-8</term><term>Type of body data - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Server</term><term>Server</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Web-Service</term><term>DataFlex 18.1</term><term>Dependant on server and application</term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Origin</term><term>http</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Methods</term><term>GET</term><term>POST</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Headers</term><term>content-type</term><term></term>
        /// </item>
        /// <item>
        /// <term>Access-Control-Allow-Credentials</term><term>true</term><term></term>
        /// </item>
        /// <item>
        /// <term>Strict-Transport-Security</term><term>max-age=31536000</term><term></term>
        /// </item>
        /// <item>
        /// <term>Date</term><term>Tue, 07 Apr 2020 22</term><term>Server date</term>
        /// </item>
        /// <item>
        /// <term>Connection</term><term>close</term><term>Indicates what state the Server will hold connection at end of response - Always in Processed response</term>
        /// </item>
        /// <item>
        /// <term>Body</term><term>&lt;http&gt;&lt;body&gt;hello&lt;/body&gt;&lt;/http&gt;</term><term>Raw body of HTTP Response</term>
        /// </item>
        /// </list>
        /// Note.  HTTP Content-Length header item in request will automatically be added
        /// </remarks>
        public bool TryHttpPOST(out ItemList response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.Domain))
                {
                    throw new Exception($"Invalid Domain.  Expect xxx.xxx.xxx etc..  Have [{this.Domain}]");
                }

                this.HTTPMethod = HTTPMethods.Post;
                response = this.DecodeResponse(this.DoHTTPRequest(this.Domain, this.UseSSL ? this.SSLPort : this.HTTPPort, this.httpRequest, true));
                return true;
            }
            catch (Exception ex)
            {
                this.TryException = ex;
                response = null;
                return false;
            }
        }

        /// <summary>
        /// Sets the Query-string used Item-List passed
        /// </summary>
        /// <param name="list">List of Name/Value items to set Query list to</param>
        /// <remarks>Uses Repository data items (or defaults) for item and name/value delimiters<br/>
        /// Overwrites any previously set Query list</remarks>
        public void SetQueryStringFromItemList(ItemList list)
        {
            this.httpRequest.SetQueryParameters(list);
        }

        /// <summary>
        /// Sets the header-string used Item-List passed
        /// </summary>
        /// <param name="list">List of Name/Value items to set Header list to</param>
        /// <remarks>Uses Repository data items (or defaults) for item and name/value delimiters<br/>
        /// Overwrites any previously set Header list</remarks>
        public void SetHeaderStringFromItemList(ItemList list)
        {
            this.httpRequest.SetHeader(list);
        }

        /// <summary>
        /// Perform as an HTTP Request, using given parameters
        /// </summary>
        /// <param name="domain">Domain name of host.  IE. <![CDATA[testdomain.com]]></param>
        /// <param name="port">TCP Port to be used.  Usually 80 for http or 443 for https</param>
        /// <param name="httpRequest">Details of HTTP Request.  <see cref="FullHTTPRequest"/></param>
        /// <param name="addContentLength">If true, Content-Length header item is added (or updated if already present) with actual byte length of HTTP body.</param>
        /// <returns>Raw string containing HTTP Response</returns>
        private string DoHTTPRequest(string domain, int port, FullHTTPRequest httpRequest, bool addContentLength)
        {
            string httpRequestString = httpRequest.ToString(addContentLength);
            string responseString = string.Empty;

            this.ActualURL = domain + ":" + port.ToString();
            this.ActualPayload = httpRequestString;

            if (this.UseSSL == true)
            {
                this.ActualURL = "https://" + this.ActualURL;
                if (this.Certificate == null && this.CertificateFilename != null)
                {
                    responseString = this.tcpRequest.DoTCPRequest(this.SSLProtocol, this.CertificateFilename, this.CertificatePassword ?? string.Empty, domain, port, httpRequestString);
                }
                else
                {
                    responseString = this.tcpRequest.DoTCPRequest(this.SSLProtocol, this.Certificate, domain, port, httpRequestString);
                }
            }
            else
            {
                this.ActualURL = "http://" + this.ActualURL;
                responseString = this.tcpRequest.DoTCPRequest(null, null, domain, port, httpRequestString);
            }

            this.ActualRawResponse = responseString;
            return responseString;
        }

        /// <summary>
        /// Decode HTTP Response into easily read Name/Value pair details within List type
        /// </summary>
        /// <param name="rawData">Raw HTTP Response string</param>
        /// <returns>Processed HTTP Response</returns>
        private ItemList DecodeResponse(string rawData)
        {
            ItemList returnData = new ItemList();
            this.ResponseRaw = rawData;

            try
            {
                // Do First line (IE. HTTP/1.1 200 OK)
                if (string.IsNullOrWhiteSpace(rawData))
                {
                    returnData.Add("HTTPVersion", "Unknown - Empty Response");
                    returnData.Add("StatusCode", "Unknown - Empty Response");
                    return returnData;
                }

                // We have something.....  Is it HTTP?
                if (!rawData.StartsWith("HTTP"))
                {
                    string firstLine = rawData.Split('\r')[0];
                    firstLine = (firstLine.Length >= 20) ? firstLine.Substring(0, 17) + "..." : firstLine;
                    returnData.Add("HTTPVersion", string.Format("Unknown - Response not HTTP: FirstLine = [{0}]", firstLine));
                    returnData.Add("StatusCode", "Unknown - Response not HTTP");
                    return returnData;
                }

                // Get the header out first....
                string headerArea = rawData.Substring(0, rawData.IndexOf("\r\n\r\n"));

                // And the HTML body
                string bodyArea = rawData.Substring(rawData.IndexOf("\r\n\r\n") + 4, rawData.Length - rawData.IndexOf("\r\n\r\n") - 4);

                // Split & check first line
                string[] firstLineSplit = headerArea.Split('\r')[0].Split(' ', 3);
                if (firstLineSplit.Length < 3 || !firstLineSplit[0].Contains('/'))
                {
                    string firstLine = headerArea.Split('\r')[0];
                    firstLine = (firstLine.Length >= 20) ? firstLine.Substring(0, 17) + "..." : firstLine;
                    returnData.Add("HTTPVersion", string.Format("Unknown - Response header top line not in correct format: [{0}]", firstLine));
                    returnData.Add("StatusCode", "Unknown - Response not formatted correctly");
                    return returnData;
                }

                // Finally, we can process the first line....
                returnData.Add("HTTPVersion", firstLineSplit[0].Split('/')[1]);
                returnData.Add("StatusCode", firstLineSplit[1]);
                string statusText = firstLineSplit[2].Trim();

                returnData.Add("StatusText", statusText);

                // And do the rest of the header...  We do a for loop as we want to ignore the top line; it is just the HTTP protocol and version info
                string[] headerSplit = headerArea.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                for (int index = 1; index < headerSplit.Length; index++)
                {
                    if (!headerSplit[index].Contains(':'))
                    {
                        throw new InvalidHTTPResponse("Response contained invalid header line [{0}]. No colon (:) present: [{1}]", index.ToString(), headerSplit[index]);
                    }
                    else
                    {
                        string[] headerItemKeyValue = headerSplit[index].Split(':', 2);
                        returnData.Add(headerItemKeyValue[0], headerItemKeyValue[1]);
                    }
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
                    bool dechunkingFinished = false;
                    string workingBody = string.Empty;
                    string chunkHex;
                    int chunkLength;
                    while (!dechunkingFinished)
                    {
                        // Itterates through the chunked body area
                        // Get the Chunk HEX
                        chunkHex = bodyArea.Substring(0, bodyArea.IndexOf("\r\n"));
                        bodyArea = bodyArea.Substring(chunkHex.Length + 2, bodyArea.Length - (chunkHex.Length + 2));

                        if (!int.TryParse(chunkHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out chunkLength))
                        {
                            throw new InvalidHTTPResponse("[HTTP]DecodeResponse: Fatal error decoding chunked html body. Parsing Hex [{0}] failed)", chunkHex);
                        }

                        if (chunkLength == 0)
                        {
                            break;
                        }

                        workingBody += bodyArea.Substring(0, chunkLength);
                        bodyArea = bodyArea.Substring(chunkLength, bodyArea.Length - chunkLength);

                        if (!bodyArea.StartsWith("\r\n"))
                        {
                            InvalidHTTPResponse ex = new InvalidHTTPResponse("[HTTP]DecodeResponse: Fatal error decoding chunked html body. End of chunk length not CRLF!)", chunkLength);
                            ex.Data.Add("Chunk Length", chunkLength);
                            ex.Data.Add("Chunk Data", bodyArea);
                            throw ex;
                        }

                        bodyArea = bodyArea.Substring(2, bodyArea.Length - 2);
                    }

                    returnData.Add("Body", workingBody);
                }
                else
                {
                    // No chunked so just grab the body
                    returnData.Add("Body", bodyArea);
                    return returnData;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidHTTPResponse("[HTTP]DecodeResponse: Fatal error decoding raw response string header)", ex);
            }

            return returnData;
        }

        /// <summary>
        /// Lists items as Key Value pairs allowing multiple items with same key
        /// </summary>
        public class ItemList : List<KeyValuePair<string, string>>
        {
            /// <summary>
            /// Initialises a new instance of the <see cref="ItemList" /> class. Prepopulates with the List of Key-Value pairs passed. 
            /// </summary>
            /// <param name="initialList">List of Key-Value pairs to pre-populate with</param>
            public ItemList(List<KeyValuePair<string, string>> initialList) : base(initialList)
            {
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="ItemList" /> class.
            /// </summary>
            public ItemList() : base()
            {
            }

            /// <summary>
            /// Gets value of first instance of an item matching the given key
            /// </summary>
            /// <param name="key">Key text to search for in <see cref="ItemList"/></param>
            /// <returns>Value of first matching item</returns>
            /// <remarks>If no items found an exception is thrown</remarks>
            public string this[string key]
            {
                get
                {
                    return this.Where(item => item.Key == key).GroupBy(item => item.Key).Select(item => item.First()).First().Value;
                }
            }

            /// <summary>
            /// Adds new item to the list.
            /// </summary>
            /// <param name="key">Key of added item</param>
            /// <param name="value">Value of added item</param>
            public void Add(string key, string value)
            {
                base.Add(new KeyValuePair<string, string>(key, value));
            }

            /// <summary>
            /// Indicates if <see cref="ItemList"/> contains a key matching the given key text
            /// </summary>
            /// <param name="key">Key text to search for in <see cref="ItemList"/></param>
            /// <returns>True is matching key found else false</returns>
            public bool ContainsKey(string key)
            {
                return this.Any(item => item.Key == key);
            }
        }

        /// <summary>
        /// Stores and formats the Header and/or Body of an HTTP request
        /// </summary>
        /// <remarks>
        /// Uses Local <see cref="TeamControlium.Utilities.Repository"/> data items to configure how the HTTP request is built.
        /// <list type="table">
        /// <listheader>
        /// <term>Category</term><term>Item</term><term>Type</term><term>Default Value</term><term>Comments</term>
        /// </listheader>
        /// <item>
        /// <term>TeamControlium.NonGUI</term><term>HTTPHeader_ItemDelimiter</term><term>string</term><term>:</term><term>Character(s) between Header title and value</term>
        /// </item>
        /// <item>
        /// <term>TeamControlium.NonGUI</term><term>HTTPHeader_SpaceAfterItemDelimiter</term><term>bool</term><term>true</term><term>Defines whether a space should follow the Header Item delimiter</term>
        /// </item>
        /// <item>
        /// <term>TeamControlium.NonGUI</term><term>HTTPHeader_ItemsLineTerminator</term><term>string</term><term>\r\n</term><term>Character(s) at end of each header item (IE. Item delimiter in header)</term>
        /// </item>
        /// <item>
        /// <term>TeamControlium.NonGUI</term><term>HeaderItemText_ContentLength</term><term>string</term><term>Content-Length</term><term>Title of Header item determining content length.  Should always be Content-Length but tests can change this for negative testing if required.</term>
        /// </item>
        /// <item>
        /// <term>TeamControlium.NonGUI</term><term>HeaderBodyDelimiter</term><term>string</term><term>\r\n</term><term>Character(s) delimiting between HTTP Header and Body. Should always be CRLF (Specification states a CRLF without preceding characters) but tests can change this for negative testing if required.</term>
        /// </item>
        /// </list>
        /// </remarks>
        private class FullHTTPRequest
        {
            /// <summary>
            /// We store the header as a string as this is Test oriented.  Although the Header of an HTTP document is Name: Value pairs the test may have loaded
            /// an invalid string for negative testing.  In which case we need to preserve that.  If we converted to a dictionary it may not work or it may remove the
            /// customisation the test has added.
            /// </summary>
            private string header;

            /// <summary>
            /// We store the query as a string as this is Test oriented.  Although the query of an HTTP URL is Name=Value pairs the test may have loaded
            /// an invalid string for negative testing.  In which case we need to preserve that.  If we converted to a dictionary it may not work or it may remove the
            /// customisation the test has added.
            /// </summary>
            private string query;

            /// <summary>
            /// Initialises a new instance of the <see cref="FullHTTPRequest" /> class.
            /// </summary>
            public FullHTTPRequest()
            {
                this.HTTPMethod = null;
                this.ResourcePath = string.Empty;
                this.SetHeader((string)null);
                this.SetQueryParameters((string)null);
                this.Body = string.Empty;
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="FullHTTPRequest" /> class.  Creates HTTP Request data containing given header and body.
            /// </summary>
            /// <param name="header">Header details of HTTP Request.</param>
            /// <param name="body">Body part of HTTP request to be sent</param>
            /// <remarks>
            /// If header is null, repository data item [TeamControlium.NonGUI,HTTPHeader] is checked for and used if set.  See <see cref="HeaderDefault"/>
            /// </remarks>
            public FullHTTPRequest(string? header, string? body)
            {
                this.HTTPMethod = null;
                this.ResourcePath = string.Empty;
                this.SetHeader(header);
                this.SetQueryParameters((string)null);
                this.Body = (body == null) ? string.Empty : body;
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="FullHTTPRequest" /> class.  Creates HTTP Request data containing given header and body.
            /// </summary>
            /// <param name="httpMethod">HTTP Method to use in Header</param>
            /// <param name="resourcePath">Resource Path of this HTTP call</param>
            /// <param name="queryParameters">Query Parameters to be appended to Resource Path (Note delimiter is NOT needed)</param>
            /// <param name="header">Header details of HTTP Request.</param>
            /// <param name="body">Body part of HTTP request to be sent</param>
            /// <remarks>
            /// If header is null, repository data item [TeamControlium.NonGUI,HTTPHeader] is checked for and used if set.  See <see cref="HeaderDefault"/>.
            /// It is the caller's responsibility to ensure header top line is NOT part of the header string as this will result in it being used twice!
            /// </remarks>
            public FullHTTPRequest(HTTPMethods httpMethod, string resourcePath, string queryParameters, string? header, string? body)
            {
                this.HTTPMethod = httpMethod;
                this.ResourcePath = resourcePath ?? string.Empty;
                this.SetHeader(header);
                this.SetQueryParameters(queryParameters);
                this.Body = (body == null) ? string.Empty : body;
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="FullHTTPRequest" /> class.  Creates HTTP Request data containing given header and body.
            /// </summary>
            /// <param name="httpMethod">HTTP Method to use in Header</param>
            /// <param name="resourcePath">Resource Path of this HTTP call</param>
            /// <param name="queryParameters">Query Parameters to be appended to Resource Path (Note delimiter is NOT needed)</param>
            /// <param name="header">Header items to be used in request. List is unwrapped and converted to string.</param>
            /// <param name="body">Body part of HTTP request to be sent</param>
            /// <remarks>
            /// If header is null, repository data item [TeamControlium.NonGUI,HTTPHeader] is checked for and used if set.  See <see cref="HeaderDefault"/>.
            /// It is the caller's responsibility to ensure header top line is NOT part of the header string as this will result in it being used twice!
            /// Note.  Aspects of the query string (delimiters etc) can be modified using Repository data items.  See <see cref="FullHTTPRequest"/> documentation for details.
            /// </remarks>
            public FullHTTPRequest(HTTPMethods httpMethod, string resourcePath, string queryParameters, ItemList? header, string? body)
            {
                this.HTTPMethod = httpMethod;
                this.ResourcePath = resourcePath ?? string.Empty;
                this.SetHeader(header);
                this.SetQueryParameters(queryParameters);
                this.Body = (body == null) ? string.Empty : body;
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="FullHTTPRequest" /> class.  Creates HTTP Request data containing given header and body.
            /// </summary>
            /// <param name="httpMethod">HTTP Method to use in Header</param>
            /// <param name="resourcePath">Resource Path of this HTTP call</param>
            /// <param name="queryParameters">Query Parameters to be appended to Resource Path. List is unwrapped and converted to string.</param>
            /// <param name="header">Header details of HTTP Request.</param>
            /// <param name="body">Body part of HTTP request to be sent</param>
            /// <remarks>
            /// If header is null, repository data item [TeamControlium.NonGUI,HTTPHeader] is checked for and used if set.  See <see cref="HeaderDefault"/>.
            /// It is the caller's responsibility to ensure header top line is NOT part of the header string as this will result in it being used twice!
            /// Note.  Aspects of the query string (delimiters etc) can be modified using Repository data items.  See <see cref="FullHTTPRequest"/> documentation for details.
            /// </remarks>
            public FullHTTPRequest(HTTPMethods httpMethod, string resourcePath, ItemList? queryParameters, string header, string body)
            {
                this.HTTPMethod = httpMethod;
                this.ResourcePath = resourcePath ?? string.Empty;
                this.SetHeader(header);
                this.SetQueryParameters(queryParameters);
                this.Body = (body == null) ? string.Empty : body;
            }

            /// <summary>
            /// Initialises a new instance of the <see cref="FullHTTPRequest" /> class.  Creates HTTP Request data containing given header and body.
            /// </summary>
            /// <param name="httpMethod">HTTP Method to use in Header</param>
            /// <param name="resourcePath">Resource Path of this HTTP call</param>
            /// <param name="queryParameters">Query Parameters to be appended to Resource Path. List is unwrapped and converted to string.</param>
            /// <param name="header">Header items to be used in request. List is unwrapped and converted to string.</param>
            /// <param name="body">Body part of HTTP request to be sent</param>
            /// <remarks>
            /// If header is null, repository data item [TeamControlium.NonGUI,HTTPHeader] is checked for and used if set.  See <see cref="HeaderDefault"/>.
            /// It is the caller's responsibility to ensure header top line is NOT part of the header string as this will result in it being used twice!
            /// Note.  Aspects of the query string (delimiters etc) can be modified using Repository data items.  See <see cref="FullHTTPRequest"/> documentation for details.
            /// </remarks>
            public FullHTTPRequest(HTTPMethods httpMethod, string resourcePath, ItemList? queryParameters, ItemList? header, string? body)
            {
                this.HTTPMethod = httpMethod;
                this.ResourcePath = resourcePath ?? string.Empty;
                this.SetHeader(header);
                this.SetQueryParameters(queryParameters);
                this.Body = (body == null) ? string.Empty : body;
            }

            /// <summary>
            /// First Text in an HTTP request to denote an HTTP POST Method.  Should be POST
            /// </summary>
            public string HttpTypePostText => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPHeader_PostText", "POST");

            /// <summary>
            /// First Text in an HTTP request to denote an HTTP GET Method.  Should be GET
            /// </summary>
            public string HttpTypeGetText => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPHeader_PostText", "GET");

            /// <summary>
            /// Separator between URL Resource Path and Query string.  Should be ?
            /// </summary>
            public string HttpURLQuerySeparator => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPURL_QuerySeparator", "?");

            /// <summary>
            /// Separator between query items in URL.  Should be &amp;
            /// </summary>
            public string HttpURLQueryParameterSeparator => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPURL_ParameterSeparator", "&");

            /// <summary>
            /// Separator between name and value of each query parameter.  Should be =
            /// </summary>
            public string HttpURLQueryParameterNameValueSeparator => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPURL_ParameterNameValueSeparator", "=");

            /// <summary>
            /// Text in HTTP request top line to indicate HTTP version document is compliant with.  Should be HTTP/1.1
            /// </summary>
            public string HttpVersion => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPHeader_Version", "HTTP/1.1");

            /// <summary>
            /// Name/Value delimiter for HPP Request header items.  Should be :
            /// </summary>
            public string HeaderItemDelimiter => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPHeader_ItemDelimiter", ":");

            /// <summary>
            /// Flag indicates if a space character should follow <see cref="HeaderItemDelimiter"/>.  Should be true
            /// </summary>
            public bool SpaceAfterHeaderItemDelimiter => GetItemLocalOrDefault<bool>("TeamControlium.NonGUI", "HTTPHeader_SpaceAfterItemDelimiter", true);

            /// <summary>
            /// HTTP Request header line termination characters.  Should be \r\n
            /// </summary>
            public string HeaderItemLineTerminator => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HTTPHeader_ItemsLineTerminator", "\r\n");

            /// <summary>
            /// Text to use for HTTP Header Content Length item.  Should be Content-Length
            /// </summary>
            public string HeaderContentLengthTitle => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HeaderItemText_ContentLength", "Content-Length");

            /// <summary>
            /// Delimiter between HTTP Request header items and the body.  Specification states this must be a CRLF with no preceding characters  
            /// </summary>
            public string HeaderBodyDelimiter => GetItemLocalOrDefault<string>("TeamControlium.NonGUI", "HeaderBodyDelimiter", "\r\n");

            /// <summary>
            /// Gets or sets HTTP Request body
            /// </summary>
            public string Body
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets HTTP Request body
            /// </summary>
            public string ResourcePath
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the HTTP Method to be used in header when building a request.  May be null in which case it is ignored.  This may be the case when the user has explicitly put the HPP Method in the request string.
            /// </summary>
            public HTTPMethods? HTTPMethod
            {
                get;
                set;
            }

            /// <summary>
            /// Gets Header from Repository data. If null Header is passed in to a call (or a public Web method with no option to set a Header is used), this property is used.  Property is populated from
            /// the local Repository item [TeamControlium.NonGUI,HTTPHeader] which can be a string or List&lt;KeyValuePair&lt;string, string&gt;&gt;.  If this has not been set an empty
            /// header is used.  If Repository item [TeamControlium.NonGUI,HTTPHeader] contains a type NOT string or List&lt;KeyValuePair&lt;string, string&gt;&gt; an exception is thrown.
            /// </summary>
            private dynamic HeaderDefault
            {
                get
                {
                    dynamic head = GetItemLocalOrDefault("TeamControlium.NonGUI", "HTTPHeader", new ItemList());
                 
                    if (head is string)
                    {
                        return (string)head;
                    }
                    else if (head is ItemList)
                    {
                        return (ItemList)head;
                    }
                    else
                    {
                        throw new Exception("Local repository [TeamControlium.NonGUI,HTTPHeader] not stored as ItemList or string.  Cannot use!");
                    }
                }
            }

            /// <summary>
            /// Gets Query string from Repository. If null Query Parameters is passed in to a call (or a public Web method with no option to set a Header is used), this property is used.  Property is populated from
            /// the local Repository item [TeamControlium.NonGUI,HTTPQuery] which can be a string or List&lt;KeyValuePair&lt;string, string&gt;&gt;.  If this has not been set an empty
            /// header is used.  If Repository item [TeamControlium.NonGUI,HTTPQuery] contains a type NOT string or List&lt;KeyValuePair&lt;string, string&gt;&gt; an exception is thrown. If populated,
            /// the query part of the URL Resource Path is populated IRRELEVANT of the HTTP Method used - this is to ensure testing (and negative testing ) is possible.  It is the
            /// responsibility of the test code to ensure correct state of the Query string.
            /// </summary>
            private dynamic QueryParametersDefault
            {
                get
                {
                    dynamic param = GetItemLocalOrDefault("TeamControlium.NonGUI", "HTTPQuery", new ItemList());

                    if (param is string)
                    {
                        return (string)param;
                    }
                    else if (param is ItemList)
                    {
                        return (ItemList)param;
                    }
                    else
                    {
                        throw new Exception("Local repository [TeamControlium.NonGUI,HTTPQuery] not stored as ItemList or string.  Cannot use!");
                    }
                }
            }

            /// <summary>
            /// Processes passed header List and sets <see cref="header"/> field.
            /// </summary>
            /// <param name="header">Collection of HTTP Request header items</param>
            /// <remarks>
            /// If null, <see cref="HeaderDefault"/> used to obtain Repository defined header if set.  If not set, Header is set to an empty string.
            /// </remarks>
            public void SetHeader(ItemList? header)
            {
                if (header == null)
                {
                    if (this.HeaderDefault.GetType() == typeof(string))
                    {
                        this.header = (string)this.HeaderDefault;
                    }
                    else
                    {
                        this.SetHeader((ItemList)this.HeaderDefault);
                    }
                }
                else
                {
                    this.header = string.Join(this.HeaderItemLineTerminator, header.Select(eachHeader => $"{eachHeader.Key}{this.HeaderItemDelimiter}{(this.SpaceAfterHeaderItemDelimiter ? " " : "")}{eachHeader.Value}"));
                    this.header += this.HeaderItemLineTerminator;
                }
            }

            /// <summary>
            /// Sets <see cref="header"/> field to passed header if not null
            /// </summary>
            /// <param name="header">Header string to use</param>
            /// <remarks>
            /// If null, <see cref="HeaderDefault"/> used to obtain Repository defined header if set.  If not set, Header is set to an empty string.
            /// </remarks>
            public void SetHeader(string? header)
            {
                if (header == null)
                {
                    if (this.HeaderDefault.GetType() == typeof(string))
                    {
                        this.header = (string)this.HeaderDefault;
                    }
                    else
                    {
                        this.SetHeader((ItemList)this.HeaderDefault);
                    }
                }
                else
                {
                    this.header = header;
                }
            }

            /// <summary>
            /// Processes passed query parameters List and sets <see cref="query"/> field.
            /// </summary>
            /// <param name="queryParameters">Collection of HTTP Resource query items.</param>
            /// <remarks>
            /// If null, <see cref="QueryParametersDefault"/> used to obtain Repository defined query string/items if set.  If not set, query field is set to an empty string.
            /// </remarks>
            public void SetQueryParameters(ItemList? queryParameters)
            {
                if (queryParameters == null)
                {
                    if (this.QueryParametersDefault.GetType() == typeof(string))
                    {
                        this.query = (string)this.QueryParametersDefault;
                    }
                    else
                    {
                        this.SetQueryParameters((ItemList)this.QueryParametersDefault);
                    }
                }
                else
                {
                    this.query = string.Join(this.HttpURLQueryParameterSeparator, queryParameters.Select(eachParameter => $"{eachParameter.Key}{this.HttpURLQueryParameterNameValueSeparator}{eachParameter.Value}"));
                }
            }

            /// <summary>
            /// Sets <see cref="query"/> field to passed query if not null
            /// </summary>
            /// <param name="query">Query string to use (does not require Resource Path Delimiter.  This is added automatically)</param>
            /// <remarks>
            /// If null, <see cref="QueryParametersDefault"/> used to obtain Repository defined query if set.  If not set, query is set to an empty string.
            /// </remarks>
            public void SetQueryParameters(string? query)
            {
                if (query == null)
                {
                    if (this.QueryParametersDefault.GetType() == typeof(string))
                    {
                        this.query = (string)this.QueryParametersDefault;
                    }
                    else
                    {
                        this.SetQueryParameters((ItemList)this.QueryParametersDefault);
                    }
                }
                else
                {
                    this.query = query;
                }
            }

            /// <summary>
            /// Returns current HTTP request header as a dictionary of Name, Value pairs
            /// </summary>
            /// <returns>Current HTTP request header</returns>
            /// <remarks>
            /// <see cref="HeaderItemLineTerminator"/> and <see cref="HeaderItemDelimiter"/> are used for name/value and line delimiting.  If Request string is not using
            /// these an invalid dictionary will be returned or a possible Exception thrown.
            /// </remarks>
            public ItemList GetHeaderAsList()
            {
                var headerList = new ItemList();

                if (string.IsNullOrEmpty(this.header))
                {
                    return headerList;
                }

                foreach (string headerItem in this.header.Split(this.HeaderItemLineTerminator))
                {
                    // We split each item into Name and Value based on the HeaderItemDelimiter.  A line may have TWO delimiters, so we max on 2.
                    var itemNameValue = headerItem.Split(this.HeaderItemDelimiter, 2);
                    var itemName = string.IsNullOrEmpty(itemNameValue[0]) ? string.Empty : itemNameValue[0];
                    var itemValue = (itemNameValue.Length == 1) ? string.Empty : string.IsNullOrEmpty(itemNameValue[1]) ? string.Empty : itemNameValue[1];
                    headerList.Add(new KeyValuePair<string, string>(itemName, itemValue));
                }

                return headerList;
            }

            /// <summary>
            /// Returns current HTTP request header string.
            /// </summary>
            /// <returns>Current header (including Top Line) as would be using is HTTP Request </returns>
            public string GetHeaderAsString()
            {
                string topLine = this.BuildTopLine();
                return ((topLine == null) ? string.Empty : topLine + this.HeaderItemLineTerminator) + this.header;
            }

            /// <summary>
            /// Returns current HTTP query as a dictionary of Name, Value pairs
            /// </summary>
            /// <returns>Current HTTP request query</returns>
            /// <remarks>
            /// <see cref="HttpURLQueryParameterNameValueSeparator"/> and <see cref="HttpURLQueryParameterSeparator"/> are used for name/value and query delimiting.  If Query string is not using
            /// these an invalid dictionary will be returned or a possible Exception thrown.
            /// </remarks>
            public ItemList GetQueryAsList()
            {
                var queryList = new ItemList();

                if (string.IsNullOrEmpty(this.query))
                {
                    return queryList;
                }

                foreach (string queryItem in this.query.Split(this.HttpURLQueryParameterSeparator))
                {
                    // We split each item into Name and Value based on the HeaderItemDelimiter.  A line may have TWO delimiters, so we max on 2.
                    var itemNameValue = queryItem.Split(this.HttpURLQueryParameterNameValueSeparator, 2);
                    var itemName = string.IsNullOrEmpty(itemNameValue[0]) ? string.Empty : itemNameValue[0];
                    var itemValue = (itemNameValue.Length == 1) ? string.Empty : string.IsNullOrEmpty(itemNameValue[1]) ? string.Empty : itemNameValue[1];
                    queryList.Add(new KeyValuePair<string, string>(itemName, itemValue));
                }

                return queryList;
            }

            /// <summary>
            /// Returns current HTTP request query string.
            /// </summary>
            /// <returns>Current HTTP query string</returns>
            public string GetQueryAsString()
            {
                return this.query ?? string.Empty;
            }

            /// <summary>
            /// Returns full HTTP Request header without automatically adding or updating Content Length header item
            /// </summary>
            /// <returns>Full HTTP Request header without Content Length added/updated (May already have it)</returns>
            /// <remarks>
            /// HTTP Request header may already contain a Content Length item.  When not automatically added/updated <see cref="FullHTTPRequest"/>
            /// does no checking or adding/updating of Content Length header item.
            /// </remarks>
            public new string ToString()
            {
                return this.ToString(false);
            }

            /// <summary>
            /// Returns full HTTP Request header with or without automatically adding or updating Content Length header item
            /// </summary>
            /// <param name="withContentLengthAddedOrUpdated">If true, Content Length item is added or updated with length of <see cref="Body"/></param>
            /// <returns>Full HTTP Request header with or without Content Length added/updated.</returns>
            /// <remarks>
            /// HTTP Request header may already contain a Content Length item.  When not automatically added/updated <see cref="FullHTTPRequest"/>
            /// does no checking or adding/updating of Content Length header item.  If Content Length is to be added or updated <see cref="FullHTTPRequest"/>
            /// counts number of characters in <see cref="Body"/> and checks <see cref="header"/>.  If no text matching <see cref="HeaderContentLengthTitle"/>
            /// exists in header, it is added using name/value delimiter <see cref="HeaderItemDelimiter"/> and terminated using <see cref="HeaderItemLineTerminator"/>.
            /// If <see cref="HeaderContentLengthTitle"/> exists, the associated value is changed to actual Body length.  Note that when updating, a best-attempt
            /// model is used; if header string is (perhaps deliberately) corrupt update may not work correctly.
            /// </remarks>
            public string ToString(bool withContentLengthAddedOrUpdated)
            {
                // Build the top line first - Starts with the HTTP Method is we have it/  If we dont, forget the top line and hope it is in the header string
                string topLine = this.BuildTopLine();

                if (withContentLengthAddedOrUpdated)
                {
                    if (this.header.Contains(this.HeaderContentLengthTitle))
                    {
                        // If we already have a Content-Length header, replace the value.
                        // We dont want to convert it to a List, replace value then convert back to a string as the header may contain delibertately invalid
                        // items/layout.  So, we dont it the nasty way;
                        // 1. Find headerContentLengthText
                        // 2. Find the location of the first digit (or line terminator) following it.
                        // 3. Find location of character after last digit after first digit (or, again, line terminator)
                        // 4. Build the new Header from all characters leading to item (2) + our length digits + all characters (inclusive) from item (3)
                        // So, first get the index of the the Content Length title first character.
                        int indexOfTitleStart = this.header.IndexOf(this.HeaderContentLengthTitle);

                        // Get the index of the last character preceding a digit or the line terminator starting from the Content Length title first character index
                        int start = this.header.IndexOfAny(("0123456789" + this.HeaderItemLineTerminator).ToCharArray(), indexOfTitleStart) - 1;
                        if (start == -1)
                        {
                            // We could not find a digit/s or line terminator.  So get the index of the last character of the Content Length title.
                            start = indexOfTitleStart + this.HeaderContentLengthTitle.Length - 1;
                            if (this.header.Length - 1 > start && this.header.Substring(start).Contains(this.HeaderItemDelimiter))
                            {
                                // If a Header Item delimiter follows the Content Length title get the index of its last character.
                                start += this.HeaderItemDelimiter.Length;
                            }
                        }

                        // Start now points to character before first digit or line terminator, or last char of title, or last char of delimiter after title. 
                        int end;
                        if (this.header.Length - 1 > start)
                        {
                            // There are charaters after start index, so see if they are digits
                            if (char.IsDigit(this.header[start + 1]))
                            {
                                // They are indeed digits.  So set end index to last digit
                                end = this.header.LastIndexOfAny("0123456789".ToCharArray(), start + 1);
                            }
                            else
                            {
                                // No, they are not digits. So set end index same as start.
                                end = start;
                            }
                        }
                        else
                        {
                            // No characters after the start index so end index is same as start
                            end = start;
                        }

                        this.header = this.header.Substring(start) + this.Body.Length.ToString() + ((this.header.Length - 1 > end) ? this.header.Substring(end + 1, this.header.Length - end) : string.Empty);
                    }
                    else
                    {
                        // We dont already have a Content-Length header so add.
                        this.header += this.HeaderContentLengthTitle + this.HeaderItemDelimiter + (this.SpaceAfterHeaderItemDelimiter ? " " : string.Empty) + (string.IsNullOrEmpty(this.Body) ? "0" : this.Body.Length.ToString() + this.HeaderItemLineTerminator);
                    }
                }

                // Bring top line (if we have one), header and body together in harmony.  With nice delimiters between them
                return ((topLine == null) ? string.Empty : topLine + this.HeaderItemLineTerminator) + this.header + this.HeaderBodyDelimiter + (string.IsNullOrEmpty(this.Body) ? this.HeaderBodyDelimiter : this.Body);
            }

            /// <summary>
            /// Builds HTTP Request Header top line using <see cref="HTTPMethod"/>, <see cref="ResourcePath"/> and <see cref="QueryString"/>.  Query string
            /// is delimited from Resource path using <see cref="HttpURLQuerySeparator"/>.
            /// </summary>
            /// <returns>Built top line of required HTTP Request</returns>
            private string BuildTopLine()
            {
                string topLine = string.Empty;
                if (this.HTTPMethod != null)
                {
                    switch (this.HTTPMethod)
                    {
                        case HTTPMethods.Post:
                            topLine = $"{this.HttpTypePostText}";
                            break;
                        case HTTPMethods.Get:
                            topLine = $"{this.HttpTypeGetText}";
                            break;
                        default: throw new ArgumentException("Must be POST or GET.  Others not yet implemented", "HTTPMethod");
                    }

                    // Add resource path (if we have one)
                    topLine += " " + ((this.ResourcePath == string.Empty) ? string.Empty : this.ResourcePath);
                    
                    // Add query parameters (if we have any)
                    topLine += (this.query == string.Empty) ? string.Empty : this.HttpURLQuerySeparator + this.query;
                    
                    // And finally, the HTTP version
                    topLine += " " + this.HttpVersion;
                }

                return topLine;
            }
        }
    }
}
