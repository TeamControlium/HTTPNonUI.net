// <copyright file="HTTPTestSteps.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.NonGUI.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TeamControlium.NonGUI;
    using TechTalk.SpecFlow;

    /// <summary>
    /// Test-step definitions for steps using/validating the NonGUI HTTPBased class.
    /// </summary>
    [Binding]
    public sealed class HTTPTestSteps
    {
        /// <summary>
        /// Used to hold context information for current Scenario
        /// </summary>
        private readonly ScenarioContext context;

        /// <summary>
        /// Initialises a new instance of the <see cref="HTTPTestSteps" /> class.
        /// Stores console output when redirected by any test steps.
        /// </summary>
        /// <param name="scenarioContext">Scenario context</param>
        public HTTPTestSteps(ScenarioContext scenarioContext)
        {
            this.context = scenarioContext;
        }

        /// <summary>
        /// Set domain, resource and query that will be used in HTTP Call
        /// </summary>
        /// <param name="domainResource">Domain and resource text to be used</param>
        [Given(@"I will browse to \(domain/resourcepath\) ""(.*)""")]
        public void GivenIWillBrowseToDomainResource(string domainResource)
        {
            if (string.IsNullOrEmpty(domainResource))
            {
                this.context["Domain"] = string.Empty;
                this.context["ResourcePath"] = string.Empty;
                this.context["Query"] = string.Empty;
            }
            else
            {
                string[] splitDomainAndResource = domainResource.Split('/', 2);
                string[] splitDomainResourceAndQuery = domainResource.Split('?', 2);
                this.context["Domain"] = splitDomainAndResource[0];
                this.context["ResourcePath"] = splitDomainAndResource.Length == 1 ? "/" : "/" + splitDomainAndResource[1].Split("?")[0];
                this.context["Query"] = splitDomainResourceAndQuery.Length == 1 ? string.Empty : splitDomainResourceAndQuery[1];
            }
        }

        /// <summary>
        /// Ensures Specflow context has an invalid domain in the Domain parameter
        /// </summary>
        [Given(@"I have an invalid domain")]
        public void GivenIHaveANInvalidDomain()
        {
            this.context["Domain"] = "qwe.lkrb.bbb";
        }

        /// <summary>
        /// Ensures Specflow context has a valid resource in the ResourcePath parameter for the domain in the Domain parameter
        /// </summary>
        [Given(@"I have a valid resource path")]
        public void GivenIHaveAValidResourcePath()
        {
            this.context["ResourcePath"] = "/webservicesserver/numberconversion.wso";
        }

        /// <summary>
        /// Ensures Specflow context has an invalid resource in the ResourcePath parameter for the domain in the Domain parameter
        /// </summary>
        [Given(@"I have an invalid resource path")]
        public void GivenIHaveAnInvalidResourcePath()
        {
            this.context["ResourcePath"] = "/invalid/numberconversion.wso";
        }

        /// <summary>
        /// Ensures Specflow context has a valid minimal HTTP Header in the Header parameter for the test to be performed
        /// </summary>
        [Given(@"I build a valid minimal HTTP Header closing the connection after the response")]
        public void GivenIBuildAValidMinimalHTTPHeaderClosingTheConnectionAfterTheResponse()
        {
            this.context["Header"] =
                   "Content-Type: text/xml\r\n" +
                   "Host: " + this.context["Domain"] + "\r\n" +
                   "Accept-Encoding: identity\r\n" +
                   "Connection: close\r\n";
        }

        /// <summary>
        /// Ensures Specflow context has an invalid minimal HTTP Header in the Header parameter for the test to be performed.  Invalid due to invalid Content-Type
        /// </summary>
        [Given(@"I build an invalid minimal HTTP Header closing the connection after the response")]
        public void GivenIBuildAnInvalidMinimalHTTPHeaderClosingTheConnectionAfterTheResponse()
        {
            this.context["Header"] =
                  "Content-Type: text/wrong\r\n" +
                  "Host: " + this.context["Domain"] + "\r\n" +
                  "Accept-Encoding: identity\r\n" +
                  "Connection: close\r\n";
        }

        /// <summary>
        /// A valid HTTP Header list is built with just Content-Type, Host, Accept-Encoding and Connection
        /// </summary>
        /// <remarks>
        /// Content-Type set to text/xml, Host obtained from Specflow context "Domain", Accept-Encoding set to identity (IE. Clear text) and Connection set to close (
        /// </remarks>
        [Given(@"I build a valid minimal HTTP Header, as List, closing the connection after the response")]
        public void GivenIBuildAValidMinimalHTTPHeaderAsListClosingTheConnectionAfterTheResponse()
        {
            HTTPBased.ItemList headerItems = new HTTPBased.ItemList();
            headerItems.Add("Content-Type", "text/xml");
            headerItems.Add("Host", (string)this.context["Domain"]);
            headerItems.Add("Accept-Encoding", "identity");
            headerItems.Add("Connection", "close");
            this.context["Header"] = headerItems;
        }

        /// <summary>
        /// Ensures Specflow context has a valid SOAP payload in the Payload parameter
        /// </summary>
        [Given(@"I build a valid XML SOAP Payload")]
        public void GivenIBuildAValidXMLSOAPPayload()
        {
            this.context["Payload"] =
                   "<?xml version=\"1.0\"?>\r\n" +
                   "<s11:Envelope xmlns:s11='http://schemas.xmlsoap.org/soap/envelope/'>\r\n" +
                   "  <s11:Body>\r\n" +
                   "    <ns1:NumberToWords xmlns:ns1='http://www.dataaccess.com/webservicesserver/'>\r\n" +
                   "      <ns1:ubiNum>78</ns1:ubiNum>\r\n" +
                   "    </ns1:NumberToWords>\r\n" +
                   "  </s11:Body>\r\n" +
                   "</s11:Envelope>";
        }

        /// <summary>
        /// Use NonGUI.HTTPBased HttpPOST method with Specflow scenario context Domain, ResourcePath, Header and Payload to make an HTTP Post request.
        /// Response is stored in Specflow context Response.
        /// </summary>
        /// <param name="httpTransactionType">Post or Get</param>
        /// <remarks>Any exceptions fail the test</remarks>
        [When(@"I perform an HTTP ""(.*)"" request")]
        public void WhenITryToPostTheHTMLToTheDomain(string httpTransactionType)
        {
            HTTPBased http = new HTTPBased();
            HTTPBased.ItemList response = new HTTPBased.ItemList();

            switch (httpTransactionType.ToLower())
            {
                case "post":
                    this.context["Response"] = http.HttpPOST(
                                                               this.context.Keys.Any(key => key == "Domain") ? (string)this.context["Domain"] : null,
                                                               this.context.Keys.Any(key => key == "ResourcePath") ? (string)this.context["ResourcePath"] : null,
                                                               this.context.Keys.Any(key => key == "Query") ? (string)this.context["Query"] : null,
                                                               this.context.Keys.Any(key => key == "Header") ? (string)this.context["Header"] : null,
                                                               this.context.Keys.Any(key => key == "Payload") ? (string)this.context["Payload"] : null);
                    break;
                case "get":
                    this.context["Response"] = http.HttpGET(
                                                               this.context.Keys.Any(key => key == "Domain") ? (string)this.context["Domain"] : null,
                                                               this.context.Keys.Any(key => key == "ResourcePath") ? (string)this.context["ResourcePath"] : null,
                                                               this.context.Keys.Any(key => key == "Query") ? (string)this.context["Query"] : null,
                                                               this.context.Keys.Any(key => key == "Header") ? (string)this.context["Header"] : null,
                                                               this.context.Keys.Any(key => key == "Payload") ? (string)this.context["Payload"] : null);
                    break;
                default:
                    throw new ArgumentException($"Only supporting POST and GET.  Got [{httpTransactionType}]", "httpTransactionType");
            }

            this.context["TryException"] = http.TryException;
        }

        /// <summary>
        /// Uses Http method to make an HTTP request with ability to set whether Content-Length header is added or not
        /// </summary>
        /// <param name="httpTransactionType">Post or Get</param>
        /// <param name="addContentLength">Text 'added' or 'not added'</param>
        /// <remarks>
        /// The success status of the call is stored in Specflow context 'HTTPRequestSuccess' and response in 'Response'.<br/>
        /// Exceptions are unhandled.
        /// </remarks>
        [When(@"I perform an HTTP ""(.*)"" request with Content-Length ""(.*)""")]
        public void WhenIPerformAnHTTPRequestWithContent_Length(string httpTransactionType, string addContentLength)
        {
            HTTPBased http = new HTTPBased();

            switch (httpTransactionType.ToLower())
            {
                case "post":
                    this.context["Response"] = http.Http(
                                                               HTTPBased.HTTPMethods.Post,
                                                               this.context.Keys.Any(key => key == "Domain") ? (string)this.context["Domain"] : null,
                                                               this.context.Keys.Any(key => key == "ResourcePath") ? (string)this.context["ResourcePath"] : null,
                                                               this.context.Keys.Any(key => key == "Query") ? (string)this.context["Query"] : null,
                                                               this.context.Keys.Any(key => key == "Header") ? (string)this.context["Header"] : null,
                                                               this.context.Keys.Any(key => key == "Payload") ? (string)this.context["Payload"] : null,
                                                               addContentLength.Trim().ToLower() == "added" ? true : false);
                    break;
                case "get":
                    this.context["Response"] = http.Http(
                                                               HTTPBased.HTTPMethods.Get,
                                                               this.context.Keys.Any(key => key == "Domain") ? (string)this.context["Domain"] : null,
                                                               this.context.Keys.Any(key => key == "ResourcePath") ? (string)this.context["ResourcePath"] : null,
                                                               this.context.Keys.Any(key => key == "Query") ? (string)this.context["Query"] : null,
                                                               this.context.Keys.Any(key => key == "Header") ? (string)this.context["Header"] : null,
                                                               this.context.Keys.Any(key => key == "Payload") ? (string)this.context["Payload"] : null,   
                                                               addContentLength.Trim().ToLower() == "added" ? true : false);
                    break;
                default:
                    throw new ArgumentException($"Only supporting POST and GET.  Got [{httpTransactionType}]", "httpTransactionType");
            }
        }

        /// <summary>
        /// Uses TryHttp to make an HTTP request with ability to set whether Content-Length header is added or not
        /// </summary>
        /// <param name="httpTransactionType">Post or Get</param>
        /// <param name="addContentLength">Text 'added' or 'not added'</param>
        /// <remarks>The success status of the call is stored in Specflow context 'HTTPRequestSuccess' and response in 'Response'.</remarks>
        [When(@"I try an HTTP ""(.*)"" request with Content-Length ""(.*)""")]
        public void WhenITryAnHTTPRequestWithContent_Length(string httpTransactionType, string addContentLength)
        {
            HTTPBased http = new HTTPBased();
            HTTPBased.ItemList response = new HTTPBased.ItemList();

            switch (httpTransactionType.ToLower())
            {
                case "post":
                    this.context["HTTPRequestSuccess"] = http.TryHttp(
                                                               HTTPBased.HTTPMethods.Post,
                                                               this.context.Keys.Any(key => key == "Domain") ? (string)this.context["Domain"] : null,
                                                               this.context.Keys.Any(key => key == "ResourcePath") ? (string)this.context["ResourcePath"] : null,
                                                               this.context.Keys.Any(key => key == "Query") ? (string)this.context["Query"] : null,
                                                               this.context.Keys.Any(key => key == "Header") ? (string)this.context["Header"] : null,
                                                               this.context.Keys.Any(key => key == "Payload") ? (string)this.context["Payload"] : null,
                                                               out response,
                                                               addContentLength.Trim().ToLower() == "added" ? true : false);
                    this.context["Response"] = response;
                    break;
                case "get":
                    this.context["HTTPRequestSuccess"] = http.TryHttp(
                                                               HTTPBased.HTTPMethods.Get,
                                                               this.context.Keys.Any(key => key == "Domain") ? (string)this.context["Domain"] : null,
                                                               this.context.Keys.Any(key => key == "ResourcePath") ? (string)this.context["ResourcePath"] : null,
                                                               this.context.Keys.Any(key => key == "Query") ? (string)this.context["Query"] : null,
                                                               this.context.Keys.Any(key => key == "Header") ? (string)this.context["Header"] : null,
                                                               this.context.Keys.Any(key => key == "Payload") ? (string)this.context["Payload"] : null,
                                                               out response,
                                                               addContentLength.Trim().ToLower() == "added" ? true : false);
                    this.context["Response"] = response;
                    break;
                default:
                    throw new ArgumentException($"Only supporting POST and GET.  Got [{httpTransactionType}]", "httpTransactionType");
            }

            this.context["TryException"] = http.TryException;
        }

        /// <summary>
        /// Loads HTTPBased type's HTTP properties from Specflow context data and calls HttpPOST or HttpGET as required.
        /// </summary>
        /// <param name="httpTransactionType">Post or Get</param>
        /// <remarks>Loads Specflow context 'Response' with HTTP response.  Any Exceptions are not handled.</remarks>
        [When(@"I setup and perform an HTTP ""(.*)"" request")]
        public void WhenISetupAndPerformAnHTTPT(string httpTransactionType)
        {
            HTTPBased http = new HTTPBased();
            http.Domain = (string)this.context["Domain"];
            http.ResourcePath = (string)this.context["ResourcePath"];

            if (this.context.ContainsKey("Query"))
            {
                if (this.context["Query"] is HTTPBased.ItemList)
                {
                    http.QueryList = (HTTPBased.ItemList)this.context["Query"];
                }
                else
                {
                    http.QueryString = (string)this.context["Query"];
                }
            }
            else
            {
                http.QueryList = new HTTPBased.ItemList();
            }

            if (this.context.ContainsKey("Header"))
            {
                if (this.context["Header"] is HTTPBased.ItemList)
                {
                    http.HeaderList = (HTTPBased.ItemList)this.context["Header"];
                }
                else
                {
                    http.HeaderString = (string)this.context["Header"];
                }
            }
            else
            {
                http.HeaderList = new HTTPBased.ItemList();
            }

            http.Body = this.context.ContainsKey("Payload") ? (string)this.context["Payload"] : null;

            switch (httpTransactionType.ToLower())
            {
                case "post":
                    this.context["Response"] = http.HttpPOST();
                    break;
                case "get":
                    this.context["Response"] = http.HttpGET();
                    break;
                default:
                    throw new ArgumentException($"Only supporting POST and GET.  Got [{httpTransactionType}]", "httpTransactionType");
            }
        }

        /// <summary>
        /// Validate that the Response has expected response code.
        /// </summary>
        /// <param name="expectedResponseCode">Expected Status code in Response</param>
        [Then(@"I get an HTTP (\d*) response back")]
        public void ThenIGetAnHTTPResponseBack(int expectedResponseCode)
        {
            HTTPBased.ItemList result = (HTTPBased.ItemList)this.context["Response"];

            Assert.IsTrue(result.ContainsKey("StatusCode"), "Response has a Status Code");

            Assert.AreEqual(expectedResponseCode.ToString(), result["StatusCode"]);
        }

        /// <summary>
        /// Validates passed text matches text in Specflow context 'Response'.
        /// </summary>
        /// <param name="expectedBodyPart">Expected text</param>
        /// <remarks>Spaces are ignored in comparison</remarks>
        [Then(@"the HTTP Body contains ""(.*)""")]
        public void ThenTheHTTPBodyContains(string expectedBodyPart)
        {
            HTTPBased.ItemList result = (HTTPBased.ItemList)this.context["Response"];

            string actualBody = result.ContainsKey("Body") ? result["Body"].Trim().ToLower() : null;

            Assert.IsTrue(actualBody.ToLower().Replace(" ", string.Empty).Contains(expectedBodyPart.ToLower().Replace(" ", string.Empty)), $"Body of HTTP response contains expected text.  Actual [{actualBody}], Expected [{expectedBodyPart}]");
        }

        /// <summary>
        /// Verifies Specflow context 'HTTPRequestSuccess' is false and that text in 'TryException' matches passed text
        /// </summary>
        /// <param name="expectedPartExceptionText">Text to compare Specflow context 'TryException' against.</param>
        /// <remarks>Comparison ignores non alphanumerics</remarks>
        [Then(@"and Exception contains text ""(.*)""")]
        public void ThenTransactionFailsAndExceptionContainsText(string expectedPartExceptionText)
        {
            Assert.IsFalse((bool)this.context["HTTPRequestSuccess"], $"Verify Try... call returned false (Actual={((bool)this.context["HTTPRequestSuccess"]?"True!! Try... call returned success!":"False")})");
            if (!((bool)this.context["HTTPRequestSuccess"]))
            {
                Exception exception = (Exception)this.context["TryException"];

                string expectedText = (new string(expectedPartExceptionText.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", string.Empty).Trim().ToLower();
                string actualText = (new string(exception.Message.Where(c => char.IsLetter(c) || char.IsDigit(c)).ToArray())).Replace(" ", string.Empty).Trim().ToLower();

                Assert.IsTrue(actualText.Contains(expectedText), exception.Message + exception.StackTrace);
            }
        }
    }
}
