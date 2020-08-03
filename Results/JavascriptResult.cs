using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LogicReinc.Asp.Results
{
    /// <summary>
    /// Javascript ContentResult
    /// </summary>
    public class JavascriptResult : ContentResult
    {
        public JavascriptResult(string js)
        {
            Content = js;
            ContentType = "application/javascript";
        }
    }
}
