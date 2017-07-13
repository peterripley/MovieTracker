using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MovieTracker.Helpers;

namespace MovieTracker.Models
{
    public class Movie
    {
        public Movie() { }
        public Movie(string Title)
        {
            this.Title = Title;
        }
        public int MovieID { get; set; }
        public string Title { get; set; }
        public int VoteCount { get; set; }
        public bool IsOwned { get; set; }
        public string Year { get; set; }
        public string Genres { get; set; }
        public string Tagline { get; set; }
        public string Keywords { get; set; }
        public string Overview { get; set; }
        public string PosterURL { get; set; }
        public string TrailerURL { get; set; }
        public bool IsInCollection { get; set; }
        public bool IsInTMDb { get; set; }
        public Constants.ActionContext ActionContext { get; set; }
    }
}