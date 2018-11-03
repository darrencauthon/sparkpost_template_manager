using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SparkPostCore;
using SparkPostCore.RequestSenders;

namespace SparkPostTemplateManager
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (args.Any() == false)
            {
                Console.Write("no arguments");
                return;
            }

            var apiKey = args.FirstOrDefault(x => x.StartsWith("--api-key="))?.Split('=')[1];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Write($"An API Key is required");
                return;
            }

            if (System.IO.File.Exists(args[0]) == false)
            {
                Console.Write($"{args[0]} does not exist");
                return;
            }

            if (args[0].EndsWith(".html") == false)
            {
                Console.Write($"{args[0]} is not a html file");
                return;
            }

            Task.Run(async () =>
            {
                var templateId = args[0].Split('.')[0];
                var html = System.IO.File.ReadAllText(args[0]);

                await PushThisTemplateHtmlToSparkPost(apiKey, templateId, html);
            }).Wait();
        }

        private static async Task PushThisTemplateHtmlToSparkPost(string apiKey, string templateId, string html)
        {
            var client = new OurSpecialClient(apiKey);

            var response = await client.TemplatesWithUpdate.Retrieve(templateId);
            response.TemplateContent.Html = html;

            await client.TemplatesWithUpdate.Update(templateId, new {Content = response.TemplateContent});
        }
    }

    public class OurSpecialClient : Client
    {
        public TemplatesWithUpdate TemplatesWithUpdate { get; }

        public OurSpecialClient(string apiKey) : base(apiKey)
        {
            var dataMapper = new DataMapper(Version);
            var asyncRequestSender = new AsyncRequestSender(this, dataMapper);
            var syncRequestSender = new SyncRequestSender(asyncRequestSender);
            var requestSender = new RequestSender(asyncRequestSender, syncRequestSender, this);

            TemplatesWithUpdate = new TemplatesWithUpdate(this, requestSender, dataMapper);
        }

        public OurSpecialClient(string apiKey, string apiHost) : base(apiKey, apiHost)
        {
        }

        public OurSpecialClient(string apiKey, long subAccountId) : base(apiKey, subAccountId)
        {
        }

        public OurSpecialClient(string apiKey, string apiHost, long subAccountId) : base(apiKey, apiHost, subAccountId)
        {
        }
    }

    public class TemplatesWithUpdate : Templates
    {
        private readonly Client client;
        private readonly RequestSender requestSender;
        private readonly DataMapper dataMapper;

        public TemplatesWithUpdate(Client client, RequestSender requestSender, DataMapper dataMapper)
            : base(client, requestSender, dataMapper)
        {
            this.client = client;
            this.requestSender = requestSender;
            this.dataMapper = dataMapper;
        }

        public async Task<Response> Update(Template template)
        {
            return await Update(template.Id, template);
        }

        public async Task<Response> Update(string templateId, object data)
        {
            var dictionary = dataMapper.CatchAll(data);

            var request = new Request
            {
                Url = $"api/{client.Version}/templates/{templateId}",
                Method = "PUT",
                Data = dictionary
            };

            var response = await requestSender.Send(request);
            if (response.StatusCode != HttpStatusCode.OK) throw new ResponseException(response);

            return response;
        }
    }
}
