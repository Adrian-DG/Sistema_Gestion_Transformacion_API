using Application.Contracts;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    public class GenericRepository : ControllerBase
    {
        protected readonly IMediator _mediator;

        public GenericRepository(IMediator mediator)
        {
            _mediator = mediator;   
        }
    }
}
