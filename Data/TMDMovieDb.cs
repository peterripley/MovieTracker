using System;
using System.Collections.Generic;
using System.Linq;
using TMDbLib.Client;
using TMDbLib.Objects.General;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;

namespace MovieTracker.Data
{
    public class TMDbMovieDb : ITMDbMovieDb
    {
        public static string PosterSizeCode = "w154";
        private TMDbLib.Client.TMDbClient _TMDbClient = null;

        public string TrailerUrl { get; private set; }
        public string Keywords { get; private set; }
                
        public TMDbMovieDb(string Sid)
        {
            // See for usage of The Movie Database Library (TMDbLib): https://github.com/LordMike/TMDbLib
            this._TMDbClient = new TMDbClient(Sid);
            this._TMDbClient.GetConfig();
        }

        public IEnumerable<SearchMovie> SearchMovie(string Title)
        {
            try
            {
                // TODO: Create extension method to order titles by similarity 
                return this._TMDbClient.SearchMovie(Title, page: 1).Results;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Movie GetMovie(int ID)
        {
            return this._TMDbClient.GetMovie(ID, MovieMethods.Videos | MovieMethods.Keywords);
        }

        public string GetPosterUrl(Movie Movie)
        {
            return this._TMDbClient.GetImageUrl(PosterSizeCode, Movie.PosterPath).AbsoluteUri;
        }

        public string GetTrailerUrl(Movie Movie)
        {
            string completeUrl = null;

            // Looking for the first English language video that resides on YouTube
            Video video = Movie.Videos.Results.Where(v => v.Iso_639_1 == "en").Where(v => v.Site.ToLower() == "youtube").FirstOrDefault();

            if (video != null)
            {
                completeUrl = String.Format("http://www.{0}.com/watch?v={1}", video.Site.ToLower(), video.Key);
            }
            
            return completeUrl;
        }

        public string GetKeywords(Movie Movie)
        {
            return string.Join(", ", Movie.Keywords.Keywords.Select(k => k.Name).ToArray());
        }

        public string GetGenres(Movie Movie)
        {
            return string.Join(" | ", Movie.Genres.Select(g => g.Name).ToArray());
        }
    }
}