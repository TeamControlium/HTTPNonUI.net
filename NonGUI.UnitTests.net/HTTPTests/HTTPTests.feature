Feature: HTTPTests
	In order to test NonGUI.HTTPBased library
	As a test automation engineer
	I want to be able to use HTTP based APIs

	Scenario: Verify simple HTTP SOAP Post
	Given I will browse to (domain/resourcepath) "www.dataaccess.com/webservicesserver/numberconversion.wso"
	And I build a valid minimal HTTP Header closing the connection after the response
	And I build a valid XML SOAP Payload
	When I perform an HTTP "POST" request
	Then I get an HTTP 200 response back

	Scenario: Verify HTTP SOAP Post setting up properties and calling HTTPPost with no parameters
	Given I will browse to (domain/resourcepath) "www.dataaccess.com/webservicesserver/numberconversion.wso"
	And I build a valid minimal HTTP Header closing the connection after the response
	And I build a valid XML SOAP Payload
	When I setup and perform an HTTP "POST" request
	Then I get an HTTP 200 response back

	Scenario: Verify HTTP SOAP Post setting up properties and calling HTTPPost with no parameters (Header Items loaded as List)
	Given I will browse to (domain/resourcepath) "www.dataaccess.com/webservicesserver/numberconversion.wso"
	And I build a valid minimal HTTP Header, as List, closing the connection after the response
	And I build a valid XML SOAP Payload
	When I setup and perform an HTTP "POST" request
	Then I get an HTTP 200 response back

	Scenario: Verify simple HTTP SOAP Post with invalid header
	Given I will browse to (domain/resourcepath) "www.dataaccess.com/webservicesserver/numberconversion.wso"
	And I build an invalid minimal HTTP Header closing the connection after the response
	And I build a valid XML SOAP Payload
	When I perform an HTTP "POST" request
	Then I get an HTTP 415 response back

	Scenario: Verify simple HTTP SOAP Post to invalid resource
	Given I will browse to (domain/resourcepath) "www.dataaccess.com/doesnotexist/numberconversion.wso"
	And I build a valid minimal HTTP Header closing the connection after the response
	And I build a valid XML SOAP Payload
	When I perform an HTTP "POST" request
	Then I get an HTTP 404 response back

	Scenario: Verify simple HTTP SOAP Get
	Given I will browse to (domain/resourcepath) "postman-echo.com/get?foo1=bar1&foo2=bar2"
	And I build a valid minimal HTTP Header closing the connection after the response
	When I perform an HTTP "GET" request
	Then I get an HTTP 200 response back
	And the HTTP Body contains "postman-echo.com/get?foo1=bar1&foo2=bar2"

	Scenario: Verify HTTP SOAP Get setting up properties and calling HTTPGet with no parameters
	Given I will browse to (domain/resourcepath) "postman-echo.com/get?foo1=bar1&foo2=bar2"
	And I build a valid minimal HTTP Header closing the connection after the response
	When I setup and perform an HTTP "GET" request
	Then I get an HTTP 200 response back
	And the HTTP Body contains "postman-echo.com/get?foo1=bar1&foo2=bar2"

	Scenario: Verify simple HTTP SOAP Post with no Content-Length setting
	Given I will browse to (domain/resourcepath) "www.dataaccess.com/webservicesserver/numberconversion.wso"
	And I build a valid minimal HTTP Header closing the connection after the response
	And I build a valid XML SOAP Payload
	When I perform an HTTP "POST" request with Content-Length "not added"
	Then I get an HTTP 411 response back
	And the HTTP Body contains "http error 411. the request must be chunked or have a content length."