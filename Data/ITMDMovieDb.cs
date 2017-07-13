using System.Collections.Generic;
using TMDbLib.Objects.Movies;
using TMDbLib.Objects.Search;

namespace MovieTracker.Data
{
    public interface ITMDbMovieDb
    {
        IEnumerable<SearchMovie> SearchMovie(string Title);
        Movie GetMovie(int ID);
        string GetPosterUrl(Movie Movie);
        string GetTrailerUrl(Movie Movie);
        string GetKeywords(Movie Movie);
        string GetGenres(Movie Movie);
    }
}