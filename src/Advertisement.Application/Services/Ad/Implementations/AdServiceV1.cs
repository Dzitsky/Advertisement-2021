﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Advertisement.Application.Services.Ad.Contracts;
using Advertisement.Application.Services.Ad.Contracts.Exceptions;
using Advertisement.Application.Services.Ad.Interfaces;
using Advertisement.Application.Services.User.Interfaces;
using Advertisement.Domain.Shared.Exceptions;

namespace Advertisement.Application.Services.Ad.Implementations
{
    public sealed class AdServiceV1 : IAdService
    {
        private readonly IRepository<Domain.Ad, int> _repository;
        private readonly IUserService _userService;

        public AdServiceV1(IUserService userService, IRepository<Domain.Ad, int> repository)
        {
            _userService = userService;
            _repository = repository;
        }

        public async Task<Create.Response> Create(Create.Request request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetCurrent(cancellationToken);
            var ad = new Domain.Ad
            {
                Name = request.Name,
                Price = request.Price,
                Status = Domain.Ad.Statuses.Created,
                OwnerId = user.Id,
                // Owner = new Domain.User
                // {
                //     Id = user.Id
                // },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _repository.Save(ad, cancellationToken);

            return new Create.Response
            {
                Id = ad.Id
            };
        }

        public async Task Delete(Delete.Request request, CancellationToken cancellationToken)
        {
            var ad = await _repository.FindById(request.Id, cancellationToken);
            if (ad == null)
            {
                throw new AdNotFoundException(request.Id);
            }

            var user = await _userService.GetCurrent(cancellationToken);
            if (ad.Owner.Id != user.Id)
            {
                throw new NoRightsException("Нет прав для выполнения операции.");
            }

            ad.Status = Domain.Ad.Statuses.Closed;
            ad.UpdatedAt = DateTime.UtcNow;
            await _repository.Save(ad, cancellationToken);
        }

        public async Task<Get.Response> Get(Get.Request request, CancellationToken cancellationToken)
        {
            var ad = await _repository.FindById(request.Id, cancellationToken);
            if (ad == null)
            {
                throw new AdNotFoundException(request.Id);
            }
            
            return new Get.Response
            {
                Name = ad.Name,
                Owner = new Get.Response.OwnerResponse
                {
                    Id = ad.Owner.Id,
                    Name = ad.Owner.Name
                },
                Price = ad.Price,
                Status = ad.Status.ToString()
            };
        }

        public async Task<GetPaged.Response> GetPaged(GetPaged.Request request, CancellationToken cancellationToken)
        {
            var total = await _repository.Count(
                cancellationToken
            );

            if (total == 0)
            {
                return new GetPaged.Response
                {
                    Items = Array.Empty<GetPaged.Response.AdResponse>(),
                    Total = total,
                    Offset = request.Offset,
                    Limit = request.Limit
                };
            }
            
            var ads = await _repository.GetPaged(
                request.Offset, request.Limit, cancellationToken
            );


            return new GetPaged.Response
            {
                Items = ads.Select(ad => new GetPaged.Response.AdResponse
                {
                    Name = ad.Name,
                    Price = ad.Price,
                    Status = ad.Status.ToString()
                }),
                Total = total,
                Offset = request.Offset,
                Limit = request.Limit
            };
        }
    }
}