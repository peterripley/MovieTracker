using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MovieTracker.Helpers;
using RestSharp;
//using TMDbLib;

namespace MovieTracker.Data
{
    public class TMDMovieApi : BaseMovieApi
    {
        public override string DefaultBaseUrl
        {
            get
            {
                return "http://api.tmdb.org/3/";
            }
        }

        public TMDMovieApi() { }

        public TMDMovieApi(string Sid, string Key) : this(Sid, Key, null) { }

        public TMDMovieApi(string Sid, string Key, string BaseUrl)
        {
            this.Sid = Sid;
            this.Key = Key;
            this.BaseUrl = BaseUrl == null ? this.BaseUrl : BaseUrl;
        }

        public override T Execute<T>(RestRequest Request) 
        {
            var client = new RestClient(BaseUrl);
            client.Authenticator = new HttpBasicAuthenticator(Sid, Key);

            Request.AddHeader("MyEmailAddress", Sid);   // Used on every request
            if (Request.Method == Method.POST)          // Used on POSTs
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