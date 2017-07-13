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
        private IMovieRepository _MovieRepository = null;

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
            this._MovieRepository = new MovieRepository(
                                    new MovieApi(Configuration.MovieApiSid, Configuration.MovieApiKey),
                                    new TMDbMovieDb(Configuration.TheMovieDbApiKey)
                                    );
        }

        public MoviesController(IMovieRepository MovieRepository)
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
                return PartialView("_Movie", _MovieRepository.GetByID(Id));
            }
            else
            {
                return View(_MovieRepository.GetByID(Id));
            }
        }

        // GET: Movies/Find/Title
        public ActionResult Find(string Title = null, string Year = null, int Page = 1, int ItemsPerPage = 10)
        {
            if (!String.IsNullOrWhiteSpace(Title))
            {
                if (Request.IsAjaxRequest())
                {
                    return PartialView("_MovieList", _MovieRepository.FindByTitle(Title, Page, ItemsPerPage));
                }
                else
                {
                    return PartialView("_MovieList", new PagedList<Movie>(new List<Movie>() { _MovieRepository.GetByTitle(Title) }, Page, ItemsPerPage));
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
                    return PartialView("_MovieList", _MovieRepository.FindByTitle(Title));
                }
                else
                {
                    return View(new PagedList<Movie>(new List<Movie>() { _MovieRepository.GetByTitle(Title) }, Page, ItemsPerPage));
                }
            }
            else
            {
                return RedirectToAction("Find", "Movies"); ;
            }
        }

        public ActionResult Add(string Title, string Year)
        {
            _MovieRepository.Add(Title, Year);

            if (Request.IsAjaxRequest())
            {
                return PartialView("_Movie", _MovieRepository.GetByTitle(Title, Year));
            }
            else
            {
                return View(_MovieRepository.GetByTitle(Title, Year));
            }
        }

        public ActionResult Vote(int Id)
        {
            if(Configuration.IsValidTimeToVote && !HasVotedThisWeek)
            {
                _MovieRepository.Vote(Id);
                HasVotedThisWeek = true;
            }
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            return PartialView("_Movie", _MovieRepository.GetByID(Id));
        }

        public ActionResult Buy(int Id)
        {
            ViewBag.HasVotedThisWeek = HasVotedThisWeek;

            _MovieRepository.Buy(Id);
            return PartialView("_Movie", _MovieRepository.GetByID(Id));
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