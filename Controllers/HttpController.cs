using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API_Console.Controllers
{
    class HttpController : ControllerBase
    {
        public HttpController(Engine engine)
            : base(engine)
        {

        }

        delegate object HTTPRequest(string path, params KeyValuePair<string, object>[] queries);
        delegate object HTTPContentRequest(string path, string body = null, string mimetype = null, params KeyValuePair<string, object>[] queries);

        protected override System.Dynamic.ExpandoObject Initialize()
        {
            dynamic api = new ExpandoObject();

            api.get = new HTTPRequest(Get);

            api.post = new HTTPContentRequest(Post);

            api.put = new HTTPContentRequest(Put);

            api.patch = new HTTPContentRequest(Patch);

            api.delete = new HTTPContentRequest(Delete);

            api.head = new HTTPContentRequest(Head);

            api.options = new HTTPContentRequest(Options);

            return api;
        }

        #region Methods

        object Get(string path, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.GET);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }

        object Post(string path, string body = null, string mimetype = null, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.POST);
            if (body != null)
                request.AddParameter(mimetype ?? "text/plain", body, RestSharp.ParameterType.RequestBody);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }
        
        object Patch(string path, string body = null, string mimetype = null, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.PATCH);
            if (body != null)
                request.AddParameter(mimetype ?? "text/plain", body, RestSharp.ParameterType.RequestBody);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }

        object Options(string path, string body = null, string type = null, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.OPTIONS);
            if (body != null)
                request.AddParameter(type ?? "text/plain", body, RestSharp.ParameterType.RequestBody);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }

        object Put(string path, string body = null, string mimetype = null, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.PUT);
            if (body != null)
                request.AddParameter(mimetype ?? "text/plain", body, RestSharp.ParameterType.RequestBody);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }

        object Delete(string path, string body = null, string mimetype = null, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.DELETE);
            if (body != null)
                request.AddParameter(mimetype ?? "text/plain", body, RestSharp.ParameterType.RequestBody);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }

        object Head(string path, string body = null, string mimetype = null, params KeyValuePair<string, object>[] parameters)
        {
            var request = new RestSharp.RestRequest(path, RestSharp.Method.HEAD);
            if (body != null)
                request.AddParameter(mimetype ?? "text/plain", body, RestSharp.ParameterType.RequestBody);

            foreach (var p in parameters)
                request.AddParameter(p.Key, p.Value);

            Engine.BeginRequest(request);

            return Engine.NoOutput;
        }

        #endregion
    }
}
