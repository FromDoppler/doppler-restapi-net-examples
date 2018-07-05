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
		public class List
		{
			public int id { get; set; }
		}

		public class RootObject
		{
			public List<List> lists { get; set; }
		}
	}
	public class ApiResponse
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


		static async Task<int> GetResponse(HttpResponseMessage response)
		{
			string responseBody = await response.Content.ReadAsStringAsync();
			var Json = JsonConvert.DeserializeObject<ApiResponse.RootObject>(responseBody);
			if (response.IsSuccessStatusCode)
			{
				Console.WriteLine(response);
				Console.WriteLine("Message :" + Json.message);
				return Json.createdResourceId;
			}
			else
			{
				Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
				Console.WriteLine(responseBody);
				return 0;
			}
		}

		static async Task<int> CreateList(string accountName, string listName)
		{
			List content = new List
			{
				name = listName
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + accountName + "/lists", content);
			return await GetResponse(response);
		}

		static async Task SubscriberToList(string accountName, int listId, string subscriberEmail)
		{
			Email content = new Email
			{
				email = subscriberEmail
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + accountName + "/lists/" + listId + "/subscribers", content);
			await GetResponse(response);
		}

		static async Task<int> CreateCampaign(string accountName, string name, string fromName, string fromEmail, string subject, string preHeader, string replyTo)
		{
			Campaign content = new Campaign
			{
				name = name,
				fromName = fromName,
				fromEmail = fromEmail,
				subject = subject,
				preheader = preHeader,
				replyTo = replyTo
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + accountName + "/campaigns", content);
			return await GetResponse(response);
		}

		static async Task ContentToCampaign(string accountName, int campaignId, string htmlCode)
		{
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
			HttpContent content = new StringContent(htmlCode);
			HttpResponseMessage response = await client.PutAsync("accounts/" + accountName + "/campaigns/" + campaignId + "/content", content);
			await GetResponse(response);
		}

		static async Task ListToCampaign(string accountName, int campaignId, int listId)
		{
			ListCampaign.RootObject content = new ListCampaign.RootObject
			{
				lists = new List<ListCampaign.List>
					{
						new ListCampaign.List {id = listId}
					}
			};
			HttpResponseMessage response = await client.PutAsJsonAsync("accounts/" + accountName + "/campaigns/" + campaignId + "/recipients", content);
			await GetResponse(response);
		}

		static async Task SendCampaign(string AccountName, int CampaignId)
		{
			Campaign content = new Campaign
			{
				type = "immediate"
			};
			HttpResponseMessage response = await client.PostAsJsonAsync("accounts/" + AccountName + "/campaigns/" + CampaignId + "/shippings", content);
			string responseBody = response.Content.ReadAsStringAsync().Result;
			var Json = JsonConvert.DeserializeObject<CampaignToSend>(responseBody);
			Console.WriteLine(response);
			Console.WriteLine("Message :" + Json.message);
		}

		static void Main()
		{
			RunAsync().GetAwaiter().GetResult();
		}

		static async Task RunAsync()
		{
			string accountName = "accountEmail@fromdoppler.com";
			string apiKey = " Api Key";

			client.BaseAddress = new Uri("https://restapi.fromdoppler.com/");
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", apiKey);

			string listName = "My C# list";
			string email = "subscriberEmail@fromdoppler.com";
			string htmlCode = "<div> My HTML content </div>";

			int listId = await CreateList(accountName, listName);
			await SubscriberToList(accountName, listId, email);
			int campaignId = await CreateCampaign(accountName, "My new campaign", "My Name", "myemail@fromdoppler.com", "Subject", "Preheader", "replyToThisEmail@fromdoppler.com");
			await ContentToCampaign(accountName, campaignId, htmlCode);
			await ListToCampaign(accountName, campaignId, listId);
			await SendCampaign(accountName, campaignId);
			
			Console.ReadLine();
		}
	}
}
