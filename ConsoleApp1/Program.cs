using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace HttpClientExample
{
	public class List
	{
		public string name { get; set; }
	}
	public class Email
	{
		public string email { get; set; }
	}
	public class Campaign
	{
		public string name { get; set; }
		public string fromName { get; set; }
		public string fromEmail { get; set; }
		public string subject { get; set; }
		public string preheader { get; set; }
		public string replyTo { get; set; }
		public string type { get; set; }
	}
	public class ListCampaign
	{
		public class Lista
		{
			public int id { get; set; }
		}

		public class RootObject
		{
			public List<Lista> lists { get; set; }
		}
	}
	public class Response
	{
		public class Link
		{
			public string href { get; set; }
			public string description { get; set; }
			public string rel { get; set; }
		}

		public class RootObject
		{
			public int createdResourceId { get; set; }
			public string message { get; set; }
			public List<Link> _links { get; set; }
		}
	}
	public class CampaignToSend
	{
		public long createdResourceId { get; set; }
		public string message { get; set; }
	}

	class Program
	{
		static HttpClient client = new HttpClient();


		static int GetResponse(HttpResponseMessage response)
		{
			string ResponseBody = response.Content.ReadAsStringAsync().Result;
			var Json = JsonConvert.DeserializeObject<Response.RootObject>(ResponseBody);
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine(response);
				Console.WriteLine("Message :" + Json.message);
				return Json.createdResourceId;
			}
			else
			{
				Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
				Console.WriteLine(ResponseBody);
				return 0;
			}
		}

		static async Task<int> CreateList(string AccountName, string ListName)
		{
			List content = new List
			{
				name = ListName
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + AccountName + "/lists", content);
			return GetResponse(response);
		}

		static async Task SubscriberToList(string AccountName, int ListId, string SubscriberEmail)
		{
			Email content = new Email
			{
				email = SubscriberEmail
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + AccountName + "/lists/" + ListId + "/subscribers", content);
			GetResponse(response);
		}

		static async Task<int> CreateCampaign(string AccountName, string Name, string FromName, string FromEmail, string Subject, string PreHeader, string ReplyTo)
		{
			Campaign content = new Campaign
			{
				name = Name,
				fromName = FromName,
				fromEmail = FromEmail,
				subject = Subject,
				preheader = PreHeader,
				replyTo = ReplyTo
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + AccountName + "/campaigns", content);
			return GetResponse(response);
		}

		static async Task ContentToCampaign(string AccountName, int CampaignId, string HtmlCode, string ApiKey)
		{
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
			HttpContent content = new StringContent(HtmlCode);
			HttpResponseMessage response = await client.PutAsync("accounts/" + AccountName + "/campaigns/" + CampaignId + "/content", content);
			GetResponse(response);
		}

		static async Task ListToCampaign(string AccountName, int CampaignId, int ListId)
		{
			ListCampaign.RootObject content = new ListCampaign.RootObject
			{
				lists = new List<ListCampaign.Lista>
					{
						new ListCampaign.Lista {id = ListId}
					}
			};
			HttpResponseMessage response = await client.PutAsJsonAsync("accounts/" + AccountName + "/campaigns/" + CampaignId + "/recipients", content);
			GetResponse(response);
		}

		static async Task SendCampaign(string AccountName, int CampaignId)
		{
			Campaign content = new Campaign
			{
				type = "immediate"
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + AccountName + "/campaigns/" + CampaignId + "/shippings", content);
			string ResponseBody = response.Content.ReadAsStringAsync().Result;
			var Json = JsonConvert.DeserializeObject<CampaignToSend>(ResponseBody);
			Console.WriteLine(response);
			Console.WriteLine("Message :" + Json.message);
		}

		static void Main()
		{
			RunAsync().GetAwaiter().GetResult();
		}

		static async Task RunAsync()
		{
			string AccountName = "Nombre de la cuenta";
			string ApiKey = "Api Key";

			client.BaseAddress = new Uri("https://restapi.fromdoppler.com/");
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", ApiKey);

			string ListName = "Lista creada por api";
			string email = "Email de subscriptor a asociar";
			string HtmlCode = "Codigo Html del contenido de la campaña";

			int ListId = await CreateList(AccountName, ListName);
			await SubscriberToList(AccountName, ListId, email);
			int CampaignId = await CreateCampaign(AccountName, "Nombre de la campaña", "Nombre del remitente", "Email del remitente", "Asunto", "Pre encabezado", "Email de respuesta de la campaña");
			await ContentToCampaign(AccountName, CampaignId, HtmlCode, ApiKey);
			await ListToCampaign(AccountName, CampaignId, ListId);
			await SendCampaign(AccountName, CampaignId);


			Console.ReadLine();
		}
	}
}