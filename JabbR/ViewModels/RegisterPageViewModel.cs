using System.Collections.Generic;

using JabbR.Services;

namespace JabbR.ViewModels
{
    public class RegisterPageViewModel
    {
        public RegisterPageViewModel()
        {
            this.Countries = CountryLookup.GetCountries();
        }

        public IDictionary<string, string> Countries { get; private set; } 
    }
}