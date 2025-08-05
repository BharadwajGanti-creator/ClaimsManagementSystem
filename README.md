\# HelloWorldPlusApi



A minimal .NET 9 Web API project that demonstrates simple REST endpoints with validation.



\## Features



\- \*\*GET /api/helloworld\*\*  

&nbsp; Returns a simple "Hello World!" message.



\- \*\*GET /api/helloworld/{name}\*\*  

&nbsp; Returns a personalized greeting.  

&nbsp; - Name must be 2-32 characters, only letters and spaces allowed.



\- \*\*GET /api/status\*\*  

&nbsp; Returns the current UTC timestamp and API version.



\## Validation



\- The `{name}` parameter in `/api/helloworld/{name}`:

&nbsp; - Must not be empty or whitespace.

&nbsp; - Must be 2-32 characters long.

&nbsp; - Must contain only letters and spaces.



\## Getting Started



1\. \*\*Clone the repository:\*\*

