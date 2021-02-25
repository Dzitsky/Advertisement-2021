﻿using Advertisement.Domain.Shared.Exceptions;

namespace Advertisement.Application.Services.User.Contracts.Exceptions
{
    public sealed class UserNotFoundException : NotFoundException
    {
        public UserNotFoundException(string message) : base(message)
        {

        }
    }
}