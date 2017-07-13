using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MovieTracker.Helpers
{
    public static class Constants
    {
        public enum ActionContext
        {
            Unspecified = 0,
            All = 1,
            Wanted = 2,
            Owned = 4,
            Find = 8,
            Add = 16,
            Delete = 32,
            Vote = 64,
            Buy = 128
        }
    }
}