using MovieTracker.Models;
using PagedList;

namespace MovieTracker.Data
{
    public interface IMoviesRepository
    {
        IPagedList<Movie> GetAll(int PageNumber = 1, int ItemsPerPage = 10);
        IPagedList<Movie> GetOwned(int PageNumber = 1, int ItemsPerPage = 10);
        IPagedList<Movie> GetWanted(int PageNumber = 1, int ItemsPerPage = 10);
        IPagedList<Movie> SearchByTitle(string Title, int PageNumber = 1, int ItemsPerPage = 10);
        Movie Get(int ID);
        int Add(string Title, string Year, int TMDbID);
        void Vote(int ID);
        void ClearVotes(int ID);
        void Buy(int ID);
        void Delete(int ID);
    }
}