using MediatR;
using FluentValidation;

namespace Application.Common;

public abstract class BaseValidator<T> : AbstractValidator<T> { }
