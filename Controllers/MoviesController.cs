using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MovieTracker.Data;
using MovieTracker.Helpers;
using MovieTracker.Models;
using System.Web.Mvc.Ajax;
using PagedList;
using PagedList.Mvc;

namespace MovieTracker.Controllers
{
    public class MoviesController : Controller
    {
        private IMoviesRepository _MovieRepository = null;

        protected override void OnException(ExceptionContext filterContext)
        {
            Exception e = filterContext.Exception;

            filterContext.ExceptionHandled = true;
            filterContext.Result = new ViewResult()
            {
                ViewName = "Error"
            };
        }

        public MoviesController()
        {
            this._MovieRepository = new MoviesRepository(
                                    new MovieApi(Configuration.MovieApiSid, Configuration.MovieApiKey),
                                    new TMDbMovieDb(Configuration.TheMovieDbApiKey)
                                    );
        }

        public MoviesController(IMoviesRepository MovieRepository)
        {
            this._MovieRepository = MovieRepository;
        }

        public ActionResult Index(int Page = 1, int ItemsPerPage = 10, bool rc = false)
        {
            if(rc)
            {
                // TODO: Comment out for production
                HasVotedThisWeek = false;
            }

            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            return View(_MovieRepository.GetAll(Page, ItemsPerPage));
        }

        // GET: Movies
        public ActionResult Owned(int Page = 1, int ItemsPerPage = 10)
        {
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            return View(_MovieRepository.GetOwned(Page, ItemsPerPage));
        }

        // GET: Movies
        public ActionResult Wanted(int Page = 1, int ItemsPerPage = 10)
        {
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            return View(_MovieRepository.GetWanted(Page, ItemsPerPage));
        }

        // GET: Movies/Details/id
        public ActionResult Details(int Id)
        {
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Movie", _MovieRepository.Get(Id));
            }
            else
            {
                return View(_MovieRepository.Get(Id));
            }
        }

        // GET: Movies/Search/Title
        public ActionResult Search(string Title = null, string Year = null, int Page = 1, int ItemsPerPage = 10)
        {
            if (!String.IsNullOrWhiteSpace(Title))
            {
                if (Request.IsAjaxRequest() || true)
                {
                    return PartialView("_MovieList", _MovieRepository.SearchByTitle(Title, Page, ItemsPerPage));
                }
                else
                {
                    //return PartialView("_MovieList", new PagedList<Movie>(new List<Movie>() { _MovieRepository.SearchByTitle(Title, Page, ItemsPerPage) }, Page, ItemsPerPage));
                    return PartialView("_MovieList", _MovieRepository.SearchByTitle(Title, Page, ItemsPerPage));
                }
            }
            else if(Title != String.Empty)
            {
                return View();
            }
            else
            {
                return null;
            }
        }

        // GET: Movies/Buy/Title
        public ActionResult Buy(string Title = null, string Year = null, int Page = 1, int ItemsPerPage = 10)
        {
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            if (Title != null)
            {
                if (Request.IsAjaxRequest())
                {
                    return PartialView("_MovieList", _MovieRepository.SearchByTitle(Title));
                }
                else
                {
                    return null;
                    //return View(new PagedList<Movie>(new List<Movie>() { _MovieRepository.SearchByTitle(Title) }, Page, ItemsPerPage));
                }
            }
            else
            {
                return RedirectToAction("Find", "Movies"); ;
            }
        }

        public ActionResult Add(string Title, string Year, int TMDbID)
        {
            int movieID = _MovieRepository.Add(Title, Year, TMDbID);
            
            return PartialView("_Movie", _MovieRepository.Get(movieID));
        }

        public ActionResult Vote(int Id)
        {
            if(Configuration.IsValidTimeToVote && !HasVotedThisWeek)
            {
                _MovieRepository.Vote(Id);
                HasVotedThisWeek = true;
            }
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            return PartialView("_Movie", _MovieRepository.Get(Id));
        }

        public ActionResult Buy(int Id)
        {
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            _MovieRepository.Buy(Id);
            return PartialView("_Movie", _MovieRepository.Get(Id));
        }

        public bool HasVotedThisWeek
        {
            get { return Request.Cookies.AllKeys.Contains("HasVotedThisWeek"); }

            set {
                if (value)
                {
                    HttpCookie httpCookie = new HttpCookie("HasVotedThisWeek");
                    httpCookie.Expires = DateTime.Today.NextDayOfWeek(DayOfWeek.Sunday);
                    Response.Cookies.Add(httpCookie);
                }
                else
                {
                    if (Request.Cookies.AllKeys.Contains("HasVotedThisWeek"))
                    {
                        HttpCookie cookie = Request.Cookies["HasVotedThisWeek"];
                        cookie.Expires = DateTime.Today.AddDays(-1);
                        Response.Cookies.Add(cookie);

                        Request.Cookies.Remove("HasVotedThisWeek");
                    }
                }
            }
        }
    }
}