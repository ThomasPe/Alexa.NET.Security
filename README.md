
# Alexa.NET.Security
This is a library to authenticate requests sent to an Alexa .NET backend. It was [initially written](https://github.com/timheuer/alexa-skills-dotnet/pull/35) by [stoiveyp](https://github.com/stoiveyp) for the [Alexa Skills SDK for .NET by Tim Heuer](https://github.com/timheuer/alexa-skills-dotnet).

# Getting started
Install from [NuGet](https://www.nuget.org/packages/Alexa.NET.Security)

`Install-Package Alexa.NET.Security`

Verify a reqeust by using 
```csharp
using Alexa.NET.Security;
var isValid =  await RequestVerification.Verify(string encodedSignature, Uri certificatePath, string body);
```

## Add ASP.NET Core 2.0 Middleware
Install from NuGet 

`Install-Package Alexa.NET.Security.Middleware`

```csharp
// Startup.cs
using Alexa.NET.Security.Middleware;

public void Configure(IApplicationBuilder app, IHostingEnvironment env)
{
    //...
    app.UseAlexaRequestValidation();
    app.UseMvc();
}
```
