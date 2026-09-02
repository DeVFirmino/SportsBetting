using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.Update;

public sealed class UpdateUserUseCase : IUpdateUserUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly IUserUpdateOnlyRepository _repository;
    private readonly IUserReadOnlyRepository _userReadOnlyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserUseCase(ILoggedUser loggerUser,
        IUserUpdateOnlyRepository repository,
        IUserReadOnlyRepository userReadOnlyRepository,
        IUnitOfWork unitOfWork)
    {
        _loggedUser = loggerUser;
        _repository = repository;
        _userReadOnlyRepository = userReadOnlyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var loggedUser = await _loggedUser.GetUserAsync(cancellationToken);

        await Validate(request, loggedUser.Email, cancellationToken);

        var user = await _repository.GetByIdAsync(loggedUser.Id, cancellationToken);

        user.Name = request.Name;
        user.Email = request.Email;

        _repository.Update(user);

        await _unitOfWork.CommitAsync(cancellationToken);
    }

    private async Task Validate(UpdateUserRequest request, string currentEmail, CancellationToken cancellationToken)
    {
        var validator = new UpdateUserValidator();

        var result = await validator.ValidateAsync(request);

        if (!request.Email.Equals(currentEmail))
        {
            var userExist = await _userReadOnlyRepository.ExistsActiveUserWithEmailAsync(request.Email, cancellationToken);
            if (userExist)
                result.Errors.Add(new FluentValidation.Results.ValidationFailure("email", ResourcesMessagesException.EMAIL_ALREADY_REGISTERED));
        }

        if (!result.IsValid)
        {
            var errorMessages = result.Errors.Select(error => error.ErrorMessage).ToList();

            throw new ErrorOnValidationException(errorMessages);
        }

    }

}
