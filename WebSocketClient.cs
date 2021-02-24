using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace LogicReinc.Asp
{
    /// <summary>
    /// Baseclass for using WebSocket, descendent object is passed to AddWebSocket or AddWebSocketAuthenticated
    /// </summary>
    public class WebSocketClient
    {
        private CancellationTokenSource _cancelToken = new CancellationTokenSource();
        private AspServer _server = null;

        public HttpContext Context { get; set; }
        public WebSocket Socket { get; set; }

        public bool Active { get; set; }

        public string Name { get; set; }
        public string Address { get; set; }
        public WebSocketClient(string group, AspServer server, HttpContext req, WebSocket socket)
        {
            Context = req;
            Socket = socket;
            Name = group;
            _server = server;
        }

        /// <summary>
        /// Main WebSocket loop
        /// </summary>
        internal async Task Handle()
        {
            Active = true;
            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[4096]);

            OnConnected();

            while (Active)
            {
                try
                {
                    byte[] resultBinary = null;
                    string resultStr = null;
                    using (MemoryStream str = new MemoryStream())
                    {
                        WebSocketMessageType type = WebSocketMessageType.Binary;
                        WebSocketReceiveResult result = null;

                        //Retrieve all message parts
                        do
                        {
                            result = await Socket.ReceiveAsync(buffer, _cancelToken.Token);
                            type = result.MessageType;
                            str.Write(buffer.Array, buffer.Offset, result.Count);
                        }
                        while (!result.EndOfMessage);
                        //Construct Message
                        if (buffer.Count > 0)
                        {

                            switch (type)
                            {
                                case WebSocketMessageType.Text:
                                    str.Seek(0, SeekOrigin.Begin);
                                    using (StreamReader reader = new StreamReader(str))
                                        resultStr = reader.ReadToEnd();
                                    break;
                                case WebSocketMessageType.Binary:
                                    resultBinary = str.ToArray();
                                    break;
                                case WebSocketMessageType.Close:
                                    Active = false;
                                    OnDisconnected();
                                    await Socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", _cancelToken.Token);
                                    break;
                            }
                        }
                    }
                    //Call Handler
                    Task.Run(() =>
                    {
                        if (resultStr != null)
                            HandleString(resultStr);
                        else if (resultBinary != null)
                            HandleBytes(resultBinary);
                    });
                }
                catch(Exception ex)
                {
                    OnException(ex);
                    //Just in case.
                    Thread.Sleep(5);
                }
            }
            _server.RemoveWebSocketClient(Name, this);
        }

        /// <summary>
        /// Called on connection
        /// </summary>
        public virtual void OnConnected()
        {

        }

        /// <summary>
        /// Called on disconnection
        /// </summary>
        public virtual void OnDisconnected()
        {

        }
        
        /// <summary>
        /// Called on an exception (in loop)
        /// </summary>
        /// <param name="ex"></param>
        public virtual void OnException(Exception ex)
        {

        }

        /// <summary>
        /// Called on receiving a message(string)
        /// </summary>
        public virtual void HandleString(string msg)
        {

        }
        /// <summary>
        /// Called on receiving a message(binary)
        /// </summary>
        public virtual void HandleBytes(byte[] msg)
        {

        }

        /// <summary>
        /// To close socket
        /// </summary>
        public void Close()
        {
            Active = false;
            _cancelToken.Cancel();
        }
    }
}
