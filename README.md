# TeamControlium - NonGUI .NET Library

Library providing an API to allow Test Frameworks & Tests to interact with TCP and HTTP endpoints.  Library allows configuration of the HTTP document/s in errored ways as well as syntactially correct ways.  SSL (HTTPS) can be handled with server certificates automatically handled in any custom way (certificate and HTTPS server information can also be easily obtained for test purposes).

Error handling also allows tests to validate negative functionality as well as positive.

## Getting Started

Library is available on NuGet (TeamControlium NonGUI). 

[Full API documentation](https://teamcontrolium.github.io/NonGUI.net)

### Dependencies

.Net Core 3.1
TeamControlium Utilities library

## Unit tests

Library uses Specflow/MSTest for it's unit tests and must always run to pass before merging to develop branch.  Tests use www.dataaccess.com/webservicesserver and postman-echo.com as target sample endpoints for tests.

## Coding Style

Vanilla Stylecop is used for policing of coding style with zero violations allowed.

## Built With

* Visual Studio Community 2019 with Sandcastle for online usage documentation

## Contributing

Contact TeamControlium contributors for possible contributions

## Versioning

We use [SemVer](http://semver.org/) for versioning. For the versions available, see the [tags on this repository](https://github.com/your/project/tags). 

## Authors

* **Mat Walker** - *Initial work and maintenance* - [v-walk](https://github.com/v-mwalk)
* **K8** - *Maintenance and work on original HTTPNonUI project* - [K8coder](https://github.com/K8coder)

See also the list of [people](https://github.com/TeamControlium/NonGUI.net/people) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* Selenium Contributors
