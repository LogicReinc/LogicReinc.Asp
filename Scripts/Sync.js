

function SyncAPI(config, token) {
    var me = this;
    var token = token;

    this.Authenticated = false;

    function applyArguments(act, args) {
        var url = act.Url;
        for (var i = 0; i < act.Arguments.length; i++) {
            if (i < args.length) {
                if (i == 0)
                    url += "?" + act.Arguments[i] + "=" + args[i];
                else
                    url += "&" + act.Arguments[i] + "=" + args[i];
            }
        }
        return url;
    }

    function requestWithoutBody(act) {
        return function () {
            var args = [].slice.call(arguments);
            var cb = args.pop();
            if (typeof cb !== "function") {
                args.push(cb);
                cb = undefined;
            }

            var headers = {};
            if (me.token)
                headers.auth = me.token;

            fetch(applyArguments(act, args), {
                method: act.Method,
                headers: headers
            })
                .then(response => response.json())
                .then(data => {
                    if (cb)
                        cb(data);
                })
                .catch(error => {
                    if (cb)
                        cb(undefined, error);
                });
        };
    }
    function requestWithBody(act, cb) {
        return function () {
            var args = [].slice.call(arguments);
            var cb = args.pop();
            if (typeof cb !== "function") {
                args.push(cb);
                cb = undefined;
            }

            var body = args.pop();

            if ((typeof body) != "string")
                body = JSON.stringify(body);

            var headers = {
                "Content-Type": "application/json"
            };

            if (me.token)
                headers.auth = me.token;
            fetch(applyArguments(act, args), {
                method: act.Method,
                headers: headers,
                body: body
            })
                .then(response => response.json())
                .then(data => {
                    if (cb)
                        cb(data);
                })
                .catch(error => {
                    if (cb)
                        cb(undefined, error);
                });
        };
    }

    this.setToken = (t) => {
        this.token = t;
    }

    this.applyConfig = (config) => {
        if (!config)
            return;
        me.Authenticated = !!config.Authenticated;

        if (config.Controllers) {
            for (var i = 0; i < config.Controllers.length; i++) {
                var controller = config.Controllers[i];

                var controllerProp = {
                    _Name: controller.ControllerName
                };
                me[controller.ControllerName] = controllerProp;

                for (var x = 0; x < controller.Actions.length; x++) {
                    var act = controller.Actions[x];

                    switch (act.Method) {
                        case "GET":
                            controllerProp[act.Name] = requestWithoutBody(act);
                            break;
                        case "POST":
                            controllerProp[act.Name] = requestWithBody(act);
                            break;
                        case "PUT":
                            controllerProp[act.Name] = requestWithBody(act);
                            break;
                    }
                }
            }
        }
        if (config.WebSockets) {
            for (var i = 0; i < config.WebSockets.length; i++) {
                var websocket = config.WebSockets[i];
                var loc = window.location
                var url = undefined;
                if (loc.protocol === "https:")
                    url = "wss:";
                else
                    url = "ws:";
                url += "//" + loc.host + websocket.Url

                me[websocket.Name] = (obj) => {
                    var socket = undefined;
                    if (!me.token)
                        socket = new WebSocket(url);
                    else
                        socket = new WebSocket(url, ["auth_" + me.token]);
                    if (!obj)
                        obj = {};
                    obj.socket = socket;
                    socket.addEventListener("open", function (ev) {
                        if (obj.open)
                            obj.open(ev);
                    });
                    socket.addEventListener("message", function (ev) {
                        if (obj.message)
                            obj.message(ev);
                    });
                    socket.addEventListener("error", function (ev) {
                        if (obj.error)
                            obj.error(ev);
                    });
                    socket.addEventListener("close", function (ev) {
                        if (obj.close)
                            obj.close(ev);
                    });
                    obj.send = (m) => {
                        socket.send(m);
                    };
                    return obj;
                };
                me[websocket.Name + "Q"] = (msg, open, close, err) => {
                    return me[websocket.Name]({
                        message: msg,
                        open: open,
                        close: close,
                        error: err
                    });
                };
            }
        }
    }

    this.updateConfig = (t,cb) => {
        var headers = {};
        if (t)
            headers.auth = t;
        fetch("/sync/get", {
            method: "GET",
            headers: headers
        })
            .then((resp) => resp.json())
            .then((data) => {
                this.token = t;
                this.applyConfig(data);
                if (cb)
                    cb(true);
            })
            .catch((x) => {
                if (cb)
                    cb(false, x);
                else
                    console.log(x);
            });
    };
    if(config)
        this.applyConfig(config);
}
