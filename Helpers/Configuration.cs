using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MovieTracker.Helpers
{
    public static class Configuration
    {
        public static string MovieApiUrl = System.Configuration.ConfigurationManager.AppSettings["MovieApiApiUrl"];
        public static string MovieApiSid = System.Configuration.ConfigurationManager.AppSettings["MovieApiSid"];
        public static string MovieApiKey = System.Configuration.ConfigurationManager.AppSettings["MovieApiApiKey"];
        public static string TheMovieDbApiUrl = System.Configuration.ConfigurationManager.AppSettings["TheMovieDbApiUrl"];
        public static string TheMovieDbApiSid = System.Configuration.ConfigurationManager.AppSettings["TheMovieDbApiSid"];
        public static string TheMovieDbApiKey = System.Configuration.ConfigurationManager.AppSettings["TheMovieDbApiKey"];
        public static string TheMovieDbApiAttributionText = System.Configuration.ConfigurationManager.AppSettings["TheMovieDbApiAttributionText"];
        public static string TheMovieDbApiAttributionLogoUrl = System.Configuration.ConfigurationManager.AppSettings["TheMovieDbApiAttributionLogogUrl"];

        public static bool IsValidTimeToVote
        {
            get {
                // TODO: Get parameters from configuration store to make this method configurable and consider time zone
                return DateTime.Now.DayOfWeek != DayOfWeek.Saturday && DateTime.Now.DayOfWeek != DayOfWeek.Sunday;
            }
        }
    }


}