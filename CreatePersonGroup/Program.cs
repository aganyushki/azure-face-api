using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace CreatePersonGroup
{
    class Program
    {
        static readonly string KEY = "18981a28639e40c492ad866ad93cb1d2";
        static readonly string PREFIX = "https://westeurope.api.cognitive.microsoft.com/face/v1.0";

        static void Main(string[] args)
        {
            string groupId = "testpersongroup1";
            var createPersonGroup = new PG {
                name = "group name #1",
                userData = "user-provided data attached to the person group.",
                recognitionModel = "recognition_02"
            };

            // CreatePGRequest(groupId, createPersonGroup).Wait();
            GetPGListRequest().Wait();
            
            // CreatePersonRequest(groupId, new Person { name = "Andrey Ganyushkin", userData ="Developer"}).Wait();
            // CreatePersonRequest(groupId, new Person { name = "Victor Nabatov", userData = "Guru" }).Wait();

            Console.WriteLine("\n------------------ ListPersonRequest --------------------\n");
            ListPersonRequest(groupId).Wait();

            string location = @"E:\photos\VictorNabatov";

            /*foreach (var fileName in Directory.GetFiles(location))
            {
                Console.WriteLine($"{fileName}");
                AddFaceRequest(groupId, "663eeb9d-b9a2-49e9-8464-1446384761f3", Path.Combine(location, fileName)).Wait();
            }*/

            Console.WriteLine("\n------------------ TRAIN --------------------\n");

            //TrainRequest(groupId).Wait();
            TrainStatusRequest(groupId).Wait();
        }

        static async Task TrainRequest(string personGroupId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            var uri = $"{PREFIX}/persongroups/{personGroupId}/train?" + queryString;

            HttpResponseMessage response;

            byte[] byteData = Encoding.UTF8.GetBytes("");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);

                string respBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{respBody}");
            }

        }

        static async Task TrainStatusRequest(string personGroupId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            var uri = $"{PREFIX}/persongroups/{personGroupId}/training?" + queryString;

            var response = await client.GetAsync(uri);

            string respBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{respBody}");
        }

        static async Task AddFaceRequest(string personGroupId, string personId, string fileName)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            queryString["userData"] = "more data details";
            queryString["detectionModel"] = "detection_01";
            var uri = PREFIX + $"/persongroups/{personGroupId}/persons/{personId}/persistedFaces?" + queryString;

            HttpResponseMessage response;

            // byte[] byteData = Encoding.UTF8.GetBytes(File.ReadAllBytes(fileName));
            byte[] byteData = File.ReadAllBytes(fileName);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);

                string respBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{respBody}");
            }
        }

        static async Task ListPersonRequest(string personGroupId)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            // Request parameters
            queryString["start"] = "0";
            queryString["top"] = "1000";
            var uri = PREFIX + $"/persongroups/{personGroupId}/persons?" + queryString;

            var response = await client.GetAsync(uri);

            string respBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{respBody}");
        }

        static async Task CreatePersonRequest(string personGroupId, Person person)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            var uri = PREFIX + $"/persongroups/{personGroupId}/persons?" + queryString;

            HttpResponseMessage response;

            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(person));

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);

                string respBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{respBody}");
            }
        }

        static async Task GetPGListRequest()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            queryString["start"] = "0";
            queryString["top"] = "1000";
            queryString["returnRecognitionModel"] = "false";
            var uri = PREFIX + "/persongroups?" + queryString;

            var response = await client.GetAsync(uri);

            string respBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{respBody}");
        }

        static async Task CreatePGRequest(string pdId, PG createPersonGroup)
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", KEY);

            var uri = PREFIX + "/persongroups/" + pdId + "?" + queryString;

            HttpResponseMessage response;

            byte[] byteData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(createPersonGroup));

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PutAsync(uri, content);

                string respBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"{respBody}");
            }
        }
    }
}
