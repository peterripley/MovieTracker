using MovieTracker.Models;
using PagedList;

namespace MovieTracker.Data
{
    public interface IMovieRepository
    {
        IPagedList<Movie> GetAll(int PageNumber = 1, int ItemsPerPage = 10);
        IPagedList<Movie> GetOwned(int PageNumber = 1, int ItemsPerPage = 10);
        IPagedList<Movie> GetWanted(int PageNumber = 1, int ItemsPerPage = 10);
        IPagedList<Movie> FindByTitle(string Title, int PageNumber = 1, int ItemsPerPage = 10);
        Movie GetByID(int ID);
        Movie GetByTitle(string Title, string Year = null);
        void Add(string Title, string Year);
        void Vote(int ID);
        void ClearVotes(int ID);
        void Buy(int ID);
        void Delete(int ID);
    }
}