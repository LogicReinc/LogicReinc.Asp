

function SyncAPI(config, token) {
    var me = this;
    var token = token;

    this.Authenticated = false;

    this.wrapper = function (call, data, err, act) { call(data, err); };
    this.$wrapper = function (call, data, err, act) { call(data, err); };

    this.beforeRequest = function (act, url, args) { };
    this.afterRequest = function (act, data) { };

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

    function requestWithoutBody(act, settings) {
        return function () {
            var args = [].slice.call(arguments);
            var cb = args.pop();
            if (typeof cb !== "function") {
                args.push(cb);
                cb = undefined;
            }

            cb = patchCallback(cb, settings, act);

            var headers = {};
            if (me.token)
                headers.auth = me.token;

            return handleFetch(applyArguments(act, args), {
                method: act.Method,
                headers: headers
            }, cb, act);
        };
    }
    function requestWithBody(act, settings) {
        return function () {
            var args = [].slice.call(arguments);
            var cb = args.pop();
            if (typeof cb !== "function") {
                args.push(cb);
                cb = undefined;
            }

            cb = patchCallback(cb, settings, act);

            var body = args.pop();

            //if ((typeof body) != "string")
            if (body && body instanceof FormData)
                body = body;
            else if(body)
                body = JSON.stringify(body);

            var headers = {
                "Content-Type": "application/json"
            };
            if (body && body instanceof FormData)
                headers = undefined;

            if (me.token)
                headers.auth = me.token;

            return handleFetch(applyArguments(act, args), {
                method: act.Method,
                headers: headers,
                body: body
            }, cb, act);
        };
    }
    function patchCallback(cb, settings, act) {
        if (!cb)
            return cb;
        if (settings?.$wrap)
            return (data, err) => me.$wrapper(cb, data, err, act);
        else
            return (data, err) => me.wrapper(cb, data, err, act);
    }

    function handleFetch(url, settings, cb, act) {
        return new Promise((resolve, reject) => {
            if (me.beforeRequest)
                me.beforeRequest(act, url, settings);
            if (act?.beforeRequest)
                act.beforeRequest(url, settings);

            fetch(url, settings)
                .then(resp => {
                    if (resp.ok)
                        return resp.text()
                            .then(txt => {
                                return {
                                    success: true,
                                    data: (txt.length > 0) ? JSON.parse(txt) : txt,
                                    resp: resp
                                };
                            });
                    else
                        return resp.text()
                            .then(txt => {
                                return {
                                    success: false,
                                    data: txt,
                                    resp: resp
                                }
                            });
                })
                .then(result => {
                    if (result.success) {
                        if (me.afterRequest)
                            me.afterRequest(act, result.data);
                        if (act.afterRequest)
                            act.afterRequest(result.data);

                        if (act.modifier)
                            result.data = act.modifier(result.data);

                        if (cb)
                            cb(result.data);
                        resolve(result.data);
                    }
                    else {
                        var error = {
                            status: result.resp.status,
                            content: result.data
                        };
                        if (me.onRequestError)
                            me.onRequestError(act, error);
                        if (act.onRequestError)
                            act.onRequestError(error);
                        if (cb)
                            cb(undefined, error);
                        reject(error);
                    }
                })
                .catch(x => {
                    var error = {
                        status: -1,
                        content: "Handling Error:" + x
                    };
                    if (cb) cb(undefined, error);
                    reject(error);
                });
        });
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

                    if (act.HasBody) {
                        controllerProp[act.Name] = requestWithBody(act);
                        controllerProp["$" + act.Name] = requestWithBody(act, { $wrap: true })
                    }
                    else {
                        controllerProp[act.Name] = requestWithoutBody(act);
                        controllerProp["$" + act.Name] = requestWithoutBody(act, { $wrap: true });
                    }
                    controllerProp[act.Name + "_Method"] = act;
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
