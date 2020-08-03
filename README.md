# LogicReinc.Asp
A Wrapper framework around ASP.NET Core 3 that is less verbose, offering various build in features such as automatic javascript api binding, authentication and websockets.

## Purpose
I personally find ASP.Net very verbose at times. Which is why I initially made LogicReinc.WebServer for quick webservers.
However with the introduction of Blazor and the fact that LogicReinc.WebServer wasn't build to scale, I decided to build a new framework surrounding Asp.NET Core 3.
This includes various features I originally made for the previous framework and improved them.
The actual usage of controllers and such is near identical to ASP.NET Core 3.

## TL;DR Simple Server
```csharp
AspServer Server = new AspServer(SomePort);
//Adds current assembly for controller auto detection
Server.AddAssemblies(typeof(Program).Assembly);

//Optionally add Authentication framework (AuthService is a custom class inheriting AuthenticationService)
Server.SetAuthentication(new AuthService());

//Optionally add Sync framework (See Sync in readme)
Server.EnabledSync = true;

//Add a websocket on /ws
Server.AddWebSocket<SomeWebSocketImplementation>("/ws2", "SomeWebsocket");
//Add a websocket on /ws2 that requires authentication
Server.AddWebSocketAuthenticated<SomeWebSocketImplementation>("/ws2", "SomeWebsocket");

//Add Files directory relatively to executable on root path
Server.AddStaticDirectory("", "Files");

//If for whatever reason you wanna disable controller autodiscovery
//Server.EnabledControllerDiscovery = false;

//Actually start the server
Server.Start();
```

## Authentication
ASP.Net has an extensive authentication system, but you don't really wanna bother with most of it do you? 
By calling AspServer.SetAuthentiction(authservice) you enable the authentication framework, providing a controller on /Authentication endpoint.
This controller contains methods such as /Authentication/Login for token response or /Authentication/LoginSession for a http-only cookie.
The authservice parameter is an implementation you provide of LogicReinc.Asp.Authentication.AuthenticationService, telling the framework how to validate a user.
To make a specific api call authenticated only you can use the following:
```csharp
        //Any authentication
        [Authorize]
        [HttpGet]
        public bool DoSomething()
        {
            return true;
        }
        //Authentication of a user with "admin" or "moderator" as roles
        [Authorize("admin", "moderator")]
        [HttpGet]
        public bool DoSomething()
        {
            return true;
        }
```
Authentication is done through json web tokens, the framework will detect these tokens on either a "auth" header or as a "auth" cookie

## Sync
Sync is a feature that you can enable by calling AspServer.EnabledSync = true. It adds a controller on the /sync endpoint which provides a way to add a javascript binding to your controllers.
Instead of writing out all the requests by hand, you just include the following:
```html
    <script src="/Sync/Config"></script>
    <script src="/Sync/Script"></script>
    <script>
      var api = new SyncAPI(SYNC_CONFIG);
      
      api.YourController.YourEndpoint(parameter1,paramter2,(data)=>{
          //use data
      });
    </script>
```
As you may have noticed, the api is exposed as a config file (a notable change from the previous framework). 
You can also get the config manually using /Sync/Get which returns a json object and pass it manually to SyncAPI.
Sync works entirely together with the Authentication framework build in. Thus only endpoints you have access to will show up.
You can also update the SyncAPI object by calling api.updateConfig(cb); (Where cb is an optional callback).
This is useful to update your api after you have logged in, an example could be:
```html
    <script>
      var api = new SyncAPI(SYNC_CONFIG);
      api.Authentication.LoginSession({User:"Username",Pass:"Password"},(data)=>{
        if(data)
          api.updateConfig();
      });
    </script>
```
Sync also exposes any Websockets you have added to the server using similar authentication filters
```html
    <script>
      var api = new SyncAPI(SYNC_CONFIG);
      var socket = api.SomeWebSocketName({
          open: (ev)=>console.log("open"),
          message: (ev)=>console.log("msg"),
          close: (ev)=>console.log("close"),
          error: (ev)=>console.log("error")
      });
      socket.send("whatever");
    </script>
```

## WebSockets
To add a websocket to your server, just define a class inheriting LogicReinc.Asp.WebSocketClient and calling
```csharp
  //Add Websocket route
  AspServer.AddWebSocket<YourWebSocketImplementation>("/yourWebsocketEndpoint", "YourWebsocketName");
  //AddWebSocketAuthenticated for authenticated websockets only
  
  //Get all connected clients
  List<WebSocketClient> client = AspServer.GetWebSocketClients("YourWebsocketName");
```
Quite straightforward.
