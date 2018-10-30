using System;
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
            Task.Run(async () =>
            {
                var client = new OurSpecialClient("dae299aeece1f1ddc881a20786b76137b2187aa4");

                var response = await client.TemplatesWithUpdate.Retrieve("overwrite-this");

                var template = new Template
                {
                    Content = response.TemplateContent,
                    Description = response.Description,
                    Id = response.Id,
                    Name = response.Name,
                    Options = response.Options,
                    Published = response.Published,
                };

                template.Content.Html = "new content";

                await client.TemplatesWithUpdate.Update(template);

            }).Wait();
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
            var dictionary = dataMapper.ToDictionary(template);
            dictionary.Remove("id");

            var request = new Request
            {
                Url = $"api/{client.Version}/templates/{template.Id}",
                Method = "PUT",
                Data = dictionary
            };

            var response = await requestSender.Send(request);
            if (response.StatusCode != HttpStatusCode.OK) throw new ResponseException(response);

            return response;
        }
    }
}
