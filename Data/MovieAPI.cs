using System;
using RestSharp;

namespace MovieTracker.Data
{
    public class MovieApi : BaseMovieApi
    {
        public override string DefaultBaseUrl
        {
            get
            {
                return "http://movieservice20170713.azurewebsites.net";
            }
        }
        
        public MovieApi() { }

        public MovieApi(string Sid, string Key) : this(Sid, Key, null) { }

        public MovieApi(string Sid, string Key, string BaseUrl)
        {
            this.Sid = Sid;
            this.Key = Key;
            this.BaseUrl = BaseUrl ?? this.BaseUrl;
        }

        public override T Execute<T>(RestRequest Request)
        {
            var client = new RestClient(BaseUrl);
            client.Authenticator = new HttpBasicAuthenticator(Sid, Key);

            Request.RequestFormat = DataFormat.Json;
            
            // Simple authentication for now
            Request.AddHeader("EmailAddress", Sid);   

            if (Request.Method == Method.POST || Request.Method == Method.PUT)
            {
                Request.AddHeader("Content-Type", "application/json");
            }

            var response = client.Execute<T>(Request);

            if (response.ErrorException != null)
            {
                throw new ApplicationException("Error receiving response.  Check inner exception for details.", response.ErrorException);
            }
            return response.Data;
        }
    }
}