using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClBillingAPI2;

namespace ClBillingAPI2
{
    public static class GlobalVar
    {
        /// <summary>
        /// Static value protected by access routine.
        /// </summary>
        static string _bearerToken;

        static string _authCookie;

        /// <summary>
        /// Access routine for global variable.
        /// </summary>
        public static string bearerToken
        {
            get
            {
                return _bearerToken;
            }
            set
            {
                _bearerToken = value;
            }
        }

        public static string authCookie
        {
            get
            {
                return _authCookie;
            }
            set
            {
                _authCookie = value;
            }
        }
    }
}
