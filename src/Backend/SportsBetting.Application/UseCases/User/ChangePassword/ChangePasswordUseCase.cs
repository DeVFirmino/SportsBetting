using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.ChangePassword;

public sealed class ChangePasswordUseCase : IChangePasswordUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IUserUpdateOnlyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordUseCase(ILoggedUser loggedUser, IUserUpdateOnlyRepository repository, IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _loggedUser = loggedUser;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task Execute(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var loggedUser = await _loggedUser.GetUserAsync(cancellationToken);
        
        Validate(request, loggedUser);
        
        var user = await _repository.GetByIdAsync(loggedUser.Id, cancellationToken);
        
        user.Password = _passwordHasher.Hash(user, request.NewPassword);
        
        _repository.Update(user);
        
        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private void Validate(ChangePasswordRequest request, Domain.Entities.User loggedUser)
    {
        var result = new ChangePasswordValidator().Validate(request);
        
        if (_passwordHasher.Verify(loggedUser, loggedUser.Password, request.Password)
            is PasswordVerificationOutcome.Failed)
        {
             result.Errors.Add(new FluentValidation.Results.ValidationFailure("password", 
                 ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID));
        }

        if (!result.IsValid)
            throw new ErrorOnValidationException(result.Errors.Select(e => e.ErrorMessage).ToList());
    } }
