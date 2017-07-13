using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MovieTracker.Helpers;
using RestSharp;

namespace MovieTracker.Data
{
    public abstract class BaseMovieApi
    {
        private string _BaseUrl = null;
        public abstract string DefaultBaseUrl { get; }
        public string BaseUrl
        {
            get
            {
                return _BaseUrl ?? DefaultBaseUrl;
            }
            set
            {
                _BaseUrl = value;
            }
        }
        public string Sid { set; get; }
        public string Key { get; set; }

        public BaseMovieApi() { }

        public BaseMovieApi(string Sid, string Key) : this(Sid, Key, null) { }

        public BaseMovieApi(string Sid, string Key, string BaseUrl)
        {
            this.Sid = Sid;
            this.Key = Key;
            this.BaseUrl = BaseUrl;
        }

        public abstract T Execute<T>(RestRequest Request) where T : new();
    }
}