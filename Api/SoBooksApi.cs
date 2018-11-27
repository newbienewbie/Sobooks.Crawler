using RestSharp;
using System.Threading.Tasks;


namespace App.Api{

    public class PostDetailPagePayload{

        public string SecretKey {get;set;}
    }



    public class ApiClient{
        private RestClient _client;

        public ApiClient(RestClient client){
            this._client = client;
        }

        public IRestResponse GetResponse(string url){

            var request=  new RestRequest(url,Method.GET);
            var response =  this._client.Execute(request);
            return response;
        }

        public IRestResponse PostToRetrievePassword(string url,string key){
            var request = new RestRequest(url,Method.POST);
            request.AddParameter("e_secret_key",key);
            return this._client.Execute(request);
        }

    }



}