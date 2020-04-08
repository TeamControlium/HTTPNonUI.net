// <copyright file="HTTPTestSteps.cs" company="TeamControlium Contributors">
//     Copyright (c) Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>
namespace TeamControlium.NonGUI.UnitTests
{
    using System;
    using System.Collections.Generic;
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
        /// Ensures Specflow context has a valid domain in the Domain parameter
        /// </summary>
        [Given(@"I have a valid domain")]
        public void GivenIHaveAValidDomain()
        {
            this.context["Domain"] = "www.dataaccess.com";
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
        /// Ensures Specflow context has a valid minimal HTTP Header in the Header parameter for the test to be performed
        /// </summary>
        [Given(@"I build a valid minimal HTTP Header closing the connection after the response")]
        public void GivenIBuildAValidMinimalHTTPHeaderClosingTheConnectionAfterTheResponse()
        {
            this.context["Header"] =
                   "Content-Type: text/xml\r\n" +
                   "Accept: */*\r\n" +
                   "Host: " + this.context["Domain"] + "\r\n" +
                   "Accept-Encoding: identity\r\n" +
                   "Connection: close\r\n";
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
        /// <remarks>Any exceptions fail the test</remarks>
        [When(@"I post the HTML to the domain")]
        public void WhenIPostTheHTMLToTheDomain()
        {
            HTTPBased http = new HTTPBased();
            try
            {
                this.context["Response"] = http.HttpPOST((string)this.context["Domain"], (string)this.context["ResourcePath"], (string)this.context["Header"], (string)this.context["Payload"]);
            }
            catch (Exception ex)
            {
                Assert.Fail("Exception thrown calling HttpPOST: " + ex.Message + "\r\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Validate that the Response has expected response code.
        /// </summary>
        /// <param name="expectedResponseCode">Expected Status code in Response</param>
        [Then(@"I get an HTTP (\d*) response back")]
        public void ThenIGetAnHTTPResponseBack(int expectedResponseCode)
        {
            Dictionary<string, string> result = (Dictionary<string, string>)this.context["Response"];

            Assert.IsTrue(result.ContainsKey("StatusCode"), "Response has a Status Code");
            Assert.AreEqual(expectedResponseCode.ToString(), result["StatusCode"]);
        }
    }
}
