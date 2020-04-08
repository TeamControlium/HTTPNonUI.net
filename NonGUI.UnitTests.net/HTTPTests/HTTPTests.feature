Feature: HTTPTests
	In order to test NonGUI.HTTPBased library
	As a test automation engineer
	I want to be able to use HTTP based APIs

	Scenario: Verify simple HTTP SOAP Post
	Given I have a valid domain
	And I have a valid resource path
	And I build a valid minimal HTTP Header closing the connection after the response
	And I build a valid XML SOAP Payload
	When I post the HTML to the domain
	Then I get an HTTP 200 response back