using Microsoft.AspNetCore.Http;
using System;

namespace LogicReinc.Asp
{
    public class AspException : Exception
    {
        public int Status { get; set; }
        public AspException(int status, string msg) : base(msg)
        {
            Status = status;
        }

        public AspException(int status, Exception ex) : base(ex.Message, ex)
        {
            Status = status;
        }
    }
    public class BadRequest_AspException : AspException
    {
        public BadRequest_AspException(string msg) : base(StatusCodes.Status400BadRequest, msg) { }
        public BadRequest_AspException(Exception ex) : base(StatusCodes.Status400BadRequest, ex) { }
    }
    public class Unauthorized_AspException : AspException
    {
        public Unauthorized_AspException(string msg) : base(StatusCodes.Status401Unauthorized, msg) { }
        public Unauthorized_AspException(Exception ex) : base(StatusCodes.Status401Unauthorized, ex) { }
    }
    public class PaymentRequired_AspException : AspException
    {
        public PaymentRequired_AspException(string msg) : base(StatusCodes.Status402PaymentRequired, msg) { }
        public PaymentRequired_AspException(Exception ex) : base(StatusCodes.Status402PaymentRequired, ex) { }
    }
    public class Forbidden_AspException : AspException
    {
        public Forbidden_AspException(string msg) : base(StatusCodes.Status403Forbidden, msg) { }
        public Forbidden_AspException(Exception ex) : base(StatusCodes.Status403Forbidden, ex) { }
    }
    public class NotFound_AspException : AspException
    {
        public NotFound_AspException(string msg) : base(StatusCodes.Status404NotFound, msg) { }
        public NotFound_AspException(Exception ex) : base(StatusCodes.Status404NotFound, ex) { }
    }
    public class MethodNotAllowed_AspException : AspException
    {
        public MethodNotAllowed_AspException(string msg) : base(StatusCodes.Status405MethodNotAllowed, msg) { }
        public MethodNotAllowed_AspException(Exception ex) : base(StatusCodes.Status405MethodNotAllowed, ex) { }
    }
    public class NotAcceptable_AspException : AspException
    {
        public NotAcceptable_AspException(string msg) : base(StatusCodes.Status406NotAcceptable, msg) { }
        public NotAcceptable_AspException(Exception ex) : base(StatusCodes.Status406NotAcceptable, ex) { }
    }
    public class ProxyAuthenticationRequired_AspException : AspException
    {
        public ProxyAuthenticationRequired_AspException(string msg) : base(StatusCodes.Status407ProxyAuthenticationRequired, msg) { }
        public ProxyAuthenticationRequired_AspException(Exception ex) : base(StatusCodes.Status407ProxyAuthenticationRequired, ex) { }
    }
    public class RequestTimeout_AspException : AspException
    {
        public RequestTimeout_AspException(string msg) : base(StatusCodes.Status408RequestTimeout, msg) { }
        public RequestTimeout_AspException(Exception ex) : base(StatusCodes.Status408RequestTimeout, ex) { }
    }
    public class Conflict_AspException : AspException
    {
        public Conflict_AspException(string msg) : base(StatusCodes.Status409Conflict, msg) { }
        public Conflict_AspException(Exception ex) : base(StatusCodes.Status409Conflict, ex) { }
    }
}
