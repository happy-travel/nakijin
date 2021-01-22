using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure.Conventions
{
    public class AuthorizeControllerModelConvention : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            controller.Filters.Add(new AuthorizeFilter());
        }
    }
}