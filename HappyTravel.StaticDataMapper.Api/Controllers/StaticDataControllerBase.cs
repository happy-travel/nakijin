using System.Globalization;
using Microsoft.AspNetCore.Mvc;

namespace HappyTravel.StaticDataMapper.Api.Controllers
{
    public class StaticDataControllerBase : ControllerBase 
    {
        protected string LanguageCode => CultureInfo.CurrentCulture.Name; 
    }
}