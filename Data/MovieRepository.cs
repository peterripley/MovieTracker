﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using MovieTracker.Models;
using TMDbLib.Objects.Search;
using PagedList;
using RestSharp;

namespace MovieTracker.Data
{
    public class MovieRepository : IMovieRepository
    {
        private const string _MoviesCacheKey = "MovieRepository.MovieCollection";
        private const string _TMDbDataCacheKey = "MovieRepository.TMDbDataCollection";
        
        public BaseMovieApi MovieApi { get; set; }
        public TMDbMovieDb TMDbMovieDb { get; set; }
        
        public MovieRepository() { }

        public MovieRepository(BaseMovieApi MovieApi, TMDbMovieDb TMDbMovieDb)
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

        public void Delete(int ID)
        {
            var request = new RestRequest("api/movies/{id}", Method.DELETE);

            request.AddParameter("id", ID, ParameterType.UrlSegment);
            this.MovieApi.Execute<Movie>(request);

            FlushMoviesCache();
        }

        public void Add(string Title, string Year)
        {
            var request = new RestRequest("api/movies/{title}", Method.POST);
            
            request.AddParameter("title", GetCollectionKey(Title, Year));
            this.MovieApi.Execute<Movie>(request);

            FlushMoviesCache();
        }
        
        public Movie GetByID(int ID)
        {
            var request = new RestRequest("api/movies/{id}", Method.GET);

            request.AddParameter("id", ID, ParameterType.UrlSegment);
            Movie movie = this.MovieApi.Execute<Movie>(request);

            if (movie != null)
            {
                movie.Title = GetTitleFromCollectionKey(movie.Title);
                AddTMDbData(movie);
                movie.IsInCollection = true;
            }

            return movie;
        }

        public Movie GetByTitle(string Title, string Year = null)
        {
            Movie movie = null;
            
            if(Year != null)
            {
                movie = GetMoviesCollection().Where(m => m.Title == GetCollectionKey(Title, Year)).FirstOrDefault();
            }
            else
            {
                movie = GetMoviesCollection().Where(m => m.Title == Title).FirstOrDefault();
            }

            if(movie != null)
            {
                movie.Title = GetTitleFromCollectionKey(movie.Title);
                AddTMDbData(movie);
                movie.IsInCollection = true;
            }
            return movie;
        }

        public IPagedList<Movie>GetAll(int PageNumber = 1, int ItemsPerPage = 10)
        {
            return GetMovies(null, PageNumber, ItemsPerPage);
        }
            
        public IPagedList<Movie> GetOwned(int PageNumber = 1, int ItemsPerPage = 10)
        {
            return GetMovies(true, PageNumber, ItemsPerPage);
        }

        public IPagedList<Movie>GetWanted(int PageNumber = 1, int ItemsPerPage = 10)
        {
            return GetMovies(false, PageNumber, ItemsPerPage);
        }

        private IPagedList<Movie>GetMovies(bool? Owned = null, int PageNumber = 1, int ItemsPerPage = 10)
        {
            IEnumerable<Movie> moviesCollection = GetMoviesCollection();
            IEnumerable<Movie> filteredMovies = moviesCollection.Where(m => m.IsOwned == (Owned != null ? (bool)Owned.Value : m.IsOwned));
            IPagedList<Movie> sortedFilteredPagedMovies = filteredMovies.OrderBy(m => m.Title.Replace("The", "").Trim()).ToPagedList(PageNumber, ItemsPerPage);
            
            foreach (var movie in sortedFilteredPagedMovies)
            {
                movie.IsInCollection = true;
                movie.Title = GetTitleFromCollectionKey(movie.Title);
                AddTMDbData(movie);
            }
            return sortedFilteredPagedMovies;
        }
        
        public IPagedList<Movie> FindByTitle(string Title, int PageNumber = 1, int ItemsPerPage = 10)
        {
            // Find movies in our collection
            List<Movie> moviesByTitle = GetMoviesCollection().Where(m => m.Title.ToLowerInvariant() == Title.ToLowerInvariant()).ToList();
            moviesByTitle.AddRange(GetMoviesCollection().Where(m => m.Title.ToLowerInvariant().Contains(Title.ToLowerInvariant())).ToList());
            foreach (Movie movie in moviesByTitle)
            {
                movie.IsInCollection = true;
                movie.Title = GetTitleFromCollectionKey(movie.Title);
                AddTMDbData(movie);
            }
            
            // Find movies in 'The Movie Database'
            List<TMDbLib.Objects.Movies.Movie> TMDbMovies = GetTMDbMovies(Title, ItemsPerPage - moviesByTitle.Count).ToList();
            foreach(TMDbLib.Objects.Movies.Movie TMDbMovie in TMDbMovies)
            {
                if (moviesByTitle.Find(m => m.Title == TMDbMovie.Title) == null)
                {
                    Movie movie = new Movie(TMDbMovie.Title);
                    movie.IsInTMDb = true;
                    AddTMDbData(movie, TMDbMovie);
                    moviesByTitle.Add(movie);
                }
            }
            
            return moviesByTitle.ToPagedList(PageNumber, ItemsPerPage);
        }

        /// <summary>
        /// Serves up movies in the 'local' collection, either from cache or refreshed if needed or requested.
        /// </summary>
        /// <param name="FlushCache">Optionally supply as true to force cache refresh.</param>
        /// <returns>Returns all movies in the collection.</returns>
        private IEnumerable<Movie> GetMoviesCollection(bool FlushCache = false)
        {
            IEnumerable<Movie> movies = HttpRuntime.Cache[_MoviesCacheKey] as IEnumerable<Movie>;

            if(movies == null || FlushCache)
            {
                var request = new RestRequest("api/movies", Method.GET);

                movies = this.MovieApi.Execute<List<Movie>>(request);
                // Absolute expiration of 0 minutes--no caching.
                HttpRuntime.Cache.Insert(_MoviesCacheKey, movies, null, DateTime.Now.AddSeconds(0 * 60), TimeSpan.Zero);
            }
            return movies;
        }

        public static void FlushMoviesCache()
        {
            HttpRuntime.Cache.Remove(_MoviesCacheKey);
        }
        
        /// <summary>
        /// Serves up movies in 'The Movie Database' collection that are matched by the supplied title.
        /// </summary>
        /// <param name="Title"></param>
        /// <param name="Take">Maximum number of movies to return in the collection.</param>
        /// <returns>A collection the requested number of movies in the database that are matched by the movie title.</returns>
        private IEnumerable<TMDbLib.Objects.Movies.Movie> GetTMDbMovies(string Title, int Take = 1)
        {
            List<TMDbLib.Objects.Movies.Movie> TMDbMovies = new List<TMDbLib.Objects.Movies.Movie>();
            TMDbLib.Objects.Movies.Movie TMDbMovie = null;
            List<SearchMovie> searchMovies = null;
            SearchMovie searchMovie = GetTMDbDataCollection()[Title.ToLowerInvariant()] as SearchMovie;

            if (searchMovie != null)
            {
                TMDbMovie = GetTMDbDataCollection()["movie" + searchMovie.Id] as TMDbLib.Objects.Movies.Movie;
                TMDbMovies.Add(TMDbMovie);
            }
            else
            {
                searchMovies = TMDbMovieDb.SearchMovie(Title).ToList();
                if (searchMovies != null)
                {
                    searchMovie = searchMovies.FirstOrDefault();
                    if (searchMovie != null)
                    {
                        TMDbMovie = this.TMDbMovieDb.GetMovie(searchMovie.Id);
                        if (TMDbMovie != null)
                        {
                            if (!GetTMDbDataCollection().Contains("movie" + searchMovie.Id))
                            {
                                // Add the SearchMovie and the Movie to the cache
                                GetTMDbDataCollection().Add(Title.ToLowerInvariant(), searchMovie);
                                GetTMDbDataCollection().Add("movie" + searchMovie.Id, TMDbMovie);
                            }
                            // Add the movie to the collection to be returned
                            TMDbMovies.Add(TMDbMovie);
                        }
                    }
                }
            }
            if (Take > 1)
            {
                if(searchMovies == null)
                {
                    searchMovies = TMDbMovieDb.SearchMovie(Title).ToList();
                }
                // Add up to the requested number of movies to the collection to be returned starting at the second
                for (int index = 1; index < searchMovies.Count && index < Take; index++)
                {
                    TMDbMovies.Add(this.TMDbMovieDb.GetMovie(searchMovies[index].Id));
                }
            }
            return TMDbMovies;
        }

        private void AddTMDbData(MovieTracker.Models.Movie Movie, TMDbLib.Objects.Movies.Movie TMDbMovie = null)
        {
            // Take the top 1 assuming it is most relevant
            if(TMDbMovie == null)
            {
                TMDbMovie = GetTMDbMovies(Movie.Title, 1).FirstOrDefault();
            }

            if (TMDbMovie != null)
            {
                Movie.Title = TMDbMovie.Title;
                Movie.Year = TMDbMovie.ReleaseDate != null ? ((DateTime)TMDbMovie.ReleaseDate).Year.ToString() : null;
                Movie.Tagline = TMDbMovie.Tagline;
                Movie.Overview = TMDbMovie.Overview;
                Movie.Genres = this.TMDbMovieDb.GetGenres(TMDbMovie);
                Movie.PosterURL = this.TMDbMovieDb.GetPosterUrl(TMDbMovie);
                Movie.TrailerURL = this.TMDbMovieDb.GetTrailerUrl(TMDbMovie);
                Movie.IsInTMDb = true;
            }
        }

        private Hashtable GetTMDbDataCollection()
        {
            object TMDbDataCollection = HttpRuntime.Cache[_TMDbDataCacheKey] as Hashtable;
            if (TMDbDataCollection == null)
            {
                TMDbDataCollection = new Hashtable();
                // Absolute expiration of 1 hour 
                HttpRuntime.Cache.Insert(_TMDbDataCacheKey, TMDbDataCollection, null, DateTime.Now.AddMinutes(1 * 60), TimeSpan.Zero);
            }
            return (Hashtable)TMDbDataCollection;
        }

        public static void FlushTMDbDataCache()
        {
            HttpRuntime.Cache.Remove(_TMDbDataCacheKey);
        }

        private string GetCollectionKey(string Title, string Year)
        {
            return String.Format("{0}<![{1}]>", Title.ToLowerInvariant(), Year);
        }

        private string GetTitleFromCollectionKey(string CollectionKey)
        {
            if(CollectionKey.Contains("<!["))
            {
                return CollectionKey.Split(new string[] { "<![" }, StringSplitOptions.None)[0];
            }
            else
            {
                return CollectionKey;
            }
        }

        private string GetYearFromCollectionKey(string CollectionKey)
        {
            if(CollectionKey.Contains("]>"))
            {
                return CollectionKey.Split(new string[] { "]>" }, StringSplitOptions.None)[1];
            }
            else
            {
                return CollectionKey;
            }
        }
    }
}