using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MagazineApiClientApp
{
    class Program
    {
        static TokenResponse tokenResponseObj = null;
        static CategoriesResponse categoriesResponseObj = null;
        static MagazineResponse magazineResponseObj = null;
        static SubscriberResponse subscriberResponseObj = null;
        static Magazines Magazines = new Magazines();
        static List<string> atleastonce = null;
        static SubscribersList subscribersList = null;
        static AnswerResponse answerResponse = null;
        static void Main(string[] args)
        {
            CallWebAPIAsync().Wait();

            Console.Write(answerResponse.Data.TotalTime);
            Console.ReadLine();
        }

        static async Task CallWebAPIAsync()
        {
            try { 
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://magazinestore.azurewebsites.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                //GET Method  
                HttpResponseMessage response = await client.GetAsync("/api/token");
                if (response.IsSuccessStatusCode)
                {
                    string tokenResponse = await response.Content.ReadAsStringAsync();
                    tokenResponseObj = JsonConvert.DeserializeObject<TokenResponse>(tokenResponse);
                }
                else
                {
                    Console.WriteLine("Internal server Error");
                }

                //get categories
                HttpResponseMessage categoriesResponse = await client.GetAsync("/api/categories/" + tokenResponseObj.Token + "");
                if (categoriesResponse.IsSuccessStatusCode)
                {
                    string tokenResponse = await categoriesResponse.Content.ReadAsStringAsync();
                    categoriesResponseObj = JsonConvert.DeserializeObject<CategoriesResponse>(tokenResponse);                    // Console.WriteLine("Id:{0}\tName:{1}", tokenResponse.Message, tokenResponse.Token);
                }
                else
                {
                    Console.WriteLine("Internal server Error");
                }


                foreach (var category in categoriesResponseObj.Data)
                {

                    //magazines for specified category
                    HttpResponseMessage magazineResponse = await client.GetAsync("/api/magazines/" + tokenResponseObj.Token + "/" + category + "");
                    if (magazineResponse.IsSuccessStatusCode)
                    {
                        string tokenResponse = await magazineResponse.Content.ReadAsStringAsync();
                        magazineResponseObj = JsonConvert.DeserializeObject<MagazineResponse>(tokenResponse);                    // Console.WriteLine("Id:{0}\tName:{1}", tokenResponse.Message, tokenResponse.Token);
                        if (Magazines.Data == null)
                            Magazines.Data = new List<Magazine>();

                        foreach (var item in magazineResponseObj.Data)
                        {
                            Magazines.Data.Add(item);
                        }

                    }
                    else
                    {
                        Console.WriteLine("Internal server Error");
                    }
                }

                //Subscriber for specified category
                HttpResponseMessage subscriberResponse = await client.GetAsync("/api/subscribers/" + tokenResponseObj.Token);
                if (subscriberResponse.IsSuccessStatusCode)
                {
                    string tokenResponse = await subscriberResponse.Content.ReadAsStringAsync();
                    subscriberResponseObj = JsonConvert.DeserializeObject<SubscriberResponse>(tokenResponse);


                    foreach (var sub in subscriberResponseObj.Data)
                    {
                        foreach (var mag in sub.MagazineIds)
                        {
                            var magazine = Magazines.Data.Find(m => m.Id == mag);
                            if (sub.Magazines == null)
                                sub.Magazines = new List<Magazine>();

                            sub.Magazines.Add(magazine);
                        }

                    }

                   
                    subscribersList = new SubscribersList();
                    subscribersList.subscribers = new List<string>();
                  
                    foreach (var subscriber in subscriberResponseObj.Data)
                    {
                        var catergories = subscriber.Magazines.Select(a => a.Category).ToList();

                        var res = catergories.Intersect(categoriesResponseObj.Data);

                        if (res.ToList().Count == categoriesResponseObj.Data.Count)
                        {
                           
                            subscribersList.subscribers.Add(subscriber.Id);
                        }


                    }
                }
                else
                {
                    Console.WriteLine("Internal server Error");
                }

            }
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://magazinestore.azurewebsites.net/");
                client.DefaultRequestHeaders.Accept.Clear();
                var myContent = JsonConvert.SerializeObject(subscribersList);
                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);
                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                var result = await client.PostAsync("/api/answer/" + tokenResponseObj.Token, byteContent);
                if (result.IsSuccessStatusCode)
                {
                    string ansresponseText = await result.Content.ReadAsStringAsync();
                    answerResponse = JsonConvert.DeserializeObject<AnswerResponse>(ansresponseText);
                }


            }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public  class SubscribersList
    {
        public List<string> subscribers { get; set; }
    }
    public class SubscriberResponse
    {
        public List<Subscriber> Data { get; set; }

        public string Token { get; set; }

        public bool Success { get; set; }       

    }

    public class Subscriber
    {
        public string Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }
        public List<int> MagazineIds { get; set; }

        public List<Magazine> Magazines { get; set; }
    }

    public  class TokenResponse
    {
        public string Token { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; }
    }

    public class CategoriesResponse
    {
        public List<string> Data { get; set; }

        public bool Success { get; set; }

        public string Token { get; set; }
    }

    public class MagazineResponse
    {
        public List<Magazine> Data { get; set; }

        public bool Success { get; set; }

        public string Token { get; set; }
    }


    public class AnswerResponse
    {
        public Answer Data { get; set; }

        public bool Success { get; set; }

        public string Token { get; set; }
    }

    public  class Answer
    {
        public string TotalTime { get; set; }

        public bool AnswerCorrect { get; set; }

        public List<string> ShouldBe { get; set; }
    }

    public class Magazines
    {
        public List<Magazine> Data { get; set; }      
    }

    public class Magazine
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Category { get; set; }
    }
}
