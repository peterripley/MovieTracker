using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MovieTracker.Models;
using TMDbLib.Objects.Search;
using PagedList;
using RestSharp;
using System.Web.Caching;

namespace MovieTracker.Data
{
    public class MoviesRepository : IMoviesRepository
    {
        private const string MoviesCacheKey = "MovieRepository.MovieCollection";
        private const string TMDbDataCacheKey = "MovieRepository.TMDbDataCollection";
        private const int CacheTimeoutHours = 1;
        
        private Hashtable TMDbDataCollection
        {
            get { return (Hashtable)HttpRuntime.Cache[TMDbDataCacheKey]; }
            set { HttpRuntime.Cache.Insert(TMDbDataCacheKey, value, null, DateTime.UtcNow.AddMinutes(CacheTimeoutHours * 60), Cache.NoSlidingExpiration); }
        }

        public BaseMovieApi MovieApi { get; set; }
        public TMDbMovieDb TMDbMovieDb { get; set; }
        
        public MoviesRepository() { }

        public MoviesRepository(BaseMovieApi MovieApi, TMDbMovieDb TMDbMovieDb)
        {
            this.MovieApi = MovieApi;
            this.TMDbMovieDb = TMDbMovieDb;
        }

        public void Vote(int ID)
        {
            var request = new RestRequest("api/movies/{id}/vote", Method.PUT);
            
            request.AddParameter("id", ID, ParameterType.UrlSegment);
            this.MovieApi.Execute<Movie>(request);

            FlushMoviesCache();
        }
                           
        public void Buy(int ID)
        {
            var request = new RestRequest("/api/movies/{id}/buy", Method.PUT);

            request.AddParameter("id", ID, ParameterType.UrlSegment);
            this.MovieApi.Execute<Movie>(request);

            FlushMoviesCache();
        }

        public void ClearVotes(int ID)
        {
            var request = new RestRequest("api/movies/{id}/clearVotes", Method.PUT);

            request.AddParameter("id", ID, ParameterType.UrlSegment);
            this.MovieApi.Execute<Movie>(request);

            FlushMoviesCache();
        }

        public int Add(string Title, string Year, int TMDbID)
        {
            var request = new RestRequest("api/movies/{title}", Method.POST);

            request.AddParameter("Title", Title);
            request.AddParameter("Year", Year);
            request.AddParameter("TMDbID", TMDbID);

            int movieID = ((Movie)this.MovieApi.Execute<Movie>(request)).MovieID;

            FlushMoviesCache();

            return movieID;
        }

        public void Delete(int ID)
        {
            var request = new RestRequest("api/movies/{id}", Method.DELETE);

            request.AddParameter("id", ID, ParameterType.UrlSegment);
            this.MovieApi.Execute<Movie>(request);

            FlushMoviesCache();
        }
        
        public Movie Get(int ID)
        {
            return GetMoviesCollectionItem(ID: ID);
        }

        public IPagedList<Movie>GetAll(int PageNumber = 1, int ItemsPerPage = 10)
        {
            return GetMovies(PageNumber, ItemsPerPage);
        }
            
        public IPagedList<Movie> GetOwned(int PageNumber = 1, int ItemsPerPage = 10)
        {
            return GetMovies(PageNumber, ItemsPerPage, true);
        }

        public IPagedList<Movie>GetWanted(int PageNumber = 1, int ItemsPerPage = 10)
        {
            return GetMovies(PageNumber, ItemsPerPage, false);
        }

        private IPagedList<Movie>GetMovies(int PageNumber, int ItemsPerPage = 10, bool? OwnedOnly = null)
        {
            return GetMoviesCollectionPage(PageNumber, ItemsPerPage, OwnedOnly);
        }
        
        public IPagedList<Movie> SearchByTitle(string Title, int PageNumber = 1, int ItemsPerPage = 10)
        {
            List<Movie> moviesByTitle = new List<Movie>(); // GetMoviesCollection().Where(m => m.Title.ToLowerInvariant() == Title.ToLowerInvariant()).ToList();

            // Find movies in our collection
            moviesByTitle.AddRange(GetMoviesCollectionCache().Where(m => m.Title.ToLowerInvariant().Contains(Title.ToLowerInvariant())).ToList());

            foreach (Movie movie in moviesByTitle)
            {
                movie.IsInCollection = true;

                if (!movie.HasTMDbData)
                {
                    AddTMDbData(movie);
                }
            }
            
            // Find movies in 'The Movie Database'
            List<TMDbLib.Objects.Movies.Movie> TMDbMovies = GetTMDbMoviesByTitle(Title, ItemsPerPage - moviesByTitle.Count).ToList();

            foreach(TMDbLib.Objects.Movies.Movie TMDbMovie in TMDbMovies)
            {
                if (moviesByTitle.Find(m => m.Title == TMDbMovie.Title) == null)
                {
                    Movie movie = new Movie(TMDbMovie.Title);
                    movie.HasTMDbData = true;
                    AddTMDbData(movie, TMDbMovie);
                    moviesByTitle.Add(movie);
                }
            }
            
            return moviesByTitle.ToPagedList(PageNumber, ItemsPerPage);
        }

        private IEnumerable<Movie> GetMoviesCollectionCache(bool Flush = false)
        {
            IEnumerable<Movie> moviesCollection = (IEnumerable<Movie>)HttpRuntime.Cache[MoviesCacheKey];
            
            if (moviesCollection == null || Flush)
            {
                // Get all movies in the collection, sort and put in the cache
                var request = new RestRequest("api/movies", Method.GET);
                moviesCollection = this.MovieApi.Execute<List<Movie>>(request).Where(m => m.Title != null).OrderBy(m => m.SortString);

                HttpRuntime.Cache.Insert(MoviesCacheKey, moviesCollection, null, DateTime.UtcNow.AddMinutes(CacheTimeoutHours * 60), Cache.NoSlidingExpiration);
            }

            return moviesCollection;
        }

        private IPagedList<Movie> GetMoviesCollectionPage(int PageNumber, int ItemsPerPage = 10, bool? OwnedOnly = false)
        {
            IPagedList<Movie> moviesCollection = null;

            if(OwnedOnly.HasValue)
            {
                moviesCollection = GetMoviesCollectionCache().Where(m => m.IsOwned == OwnedOnly.Value).ToPagedList(PageNumber, ItemsPerPage);
            }
            else
            {
                moviesCollection = GetMoviesCollectionCache().ToPagedList(PageNumber, ItemsPerPage);
            }
            
            // Populate TMDb data where missing
            foreach (var movie in moviesCollection)
            {
                movie.IsInCollection = true;

                if (!movie.HasTMDbData)
                {
                    AddTMDbData(movie);
                }
            }
            
            return moviesCollection;
        }

        private Movie GetMoviesCollectionItem(int? ID = null, String Title = null)
        {
            Movie movie = null;

            if (ID.HasValue)
            {
                movie = GetMoviesCollectionCache().Where(m => m.MovieID == (int)ID.Value).First();
            }
            else
            {
                movie = GetMoviesCollectionCache().Where(m => m.Title == Title).First();
            }

            // Add the IMDb data if it's missing
            if (movie != null && !movie.HasTMDbData)
            {
                AddTMDbData(movie);
                movie.IsInCollection = true;
            }

            return movie;
        }

        public static void FlushMoviesCache()
        {
            HttpRuntime.Cache.Remove(MoviesCacheKey);
        }

        private TMDbLib.Objects.Movies.Movie GetTMDbMovie(int ID)
        {
            return this.TMDbMovieDb.GetMovie(ID);
        }

        private IEnumerable<TMDbLib.Objects.Movies.Movie> GetTMDbMoviesByTitle(string Title, int Take = 1)
        {
            List<TMDbLib.Objects.Movies.Movie> TMDbMovies = new List<TMDbLib.Objects.Movies.Movie>();
            TMDbLib.Objects.Movies.Movie TMDbMovie = null;
            List<SearchMovie> searchMovies = null;

            SearchMovie searchMovie = null; 

            // Get a list of searchMovies 'stubs' that match the title provided
            searchMovies = TMDbMovieDb.SearchMovie(Title).ToList();

            if (searchMovies != null)
            {
                // Get the first in the list which is the most relevant
                searchMovie = searchMovies.FirstOrDefault();

                if (searchMovie != null)
                {
                    // Get the TMDbMovie associated with the searchMovie 'stub'
                    TMDbMovie = this.TMDbMovieDb.GetMovie(searchMovie.Id);

                    if (TMDbMovie != null)
                    {
                        // Add the movie to the collection to be returned
                        TMDbMovies.Add(TMDbMovie);
                    }
                }

                // Add more movies to the collection if more than 1 was requested
                if (Take > 1)
                {
                    // Add a maximum of the requested number of movies to the collection to be returned, starting at the second
                    for (int index = 1; index < searchMovies.Count && index < Take; index++)
                    {
                        TMDbMovies.Add(this.TMDbMovieDb.GetMovie(searchMovies[index].Id));
                    }
                }
            }
            return TMDbMovies;
        }

        private void AddTMDbData(MovieTracker.Models.Movie Movie, TMDbLib.Objects.Movies.Movie TMDbMovie = null)
        {
            if(TMDbMovie == null)
            {
                if (Movie.TMDbID != 0)
                {
                    TMDbMovie = GetTMDbMovie(Movie.TMDbID);
                }
                else
                {
                    TMDbMovie = GetTMDbMoviesByTitle(Movie.Title, 1).FirstOrDefault();
                }
            }

            if (TMDbMovie != null)
            {
                Movie.Title = TMDbMovie.Title;
                Movie.Year = TMDbMovie.ReleaseDate != null ? ((DateTime)TMDbMovie.ReleaseDate).Year.ToString() : null;
                Movie.Tagline = TMDbMovie.Tagline;
                Movie.Overview = TMDbMovie.Overview;
                Movie.TMDbID = TMDbMovie.Id;
                Movie.Genres = this.TMDbMovieDb.GetGenres(TMDbMovie);
                Movie.PosterURL = this.TMDbMovieDb.GetPosterUrl(TMDbMovie);
                Movie.TrailerURL = this.TMDbMovieDb.GetTrailerUrl(TMDbMovie);
                Movie.HasTMDbData = true;
            }
        }

        public static void FlushTMDbDataCache()
        {
            HttpRuntime.Cache.Remove(TMDbDataCacheKey);
        }
    }
}