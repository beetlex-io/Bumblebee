using BeetleX.FastHttpApi;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gateway.OverrideHttpRequest
{
    class MyGateway : Bumblebee.Gateway
    {

        protected override void OnHttpRequest(object sender, EventHttpRequestArgs e)
        {
            if (e.Request.Url.IndexOf("/admin", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                e.Response.Result(new JsonResult($"无权访问{e.Request.Url}"));
                e.Cancel = true;
            }
            else
            {
                base.OnHttpRequest(sender, e);
            }
        }
    }
}
