using AutoMapper;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Security.Tokens;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.UseCases.User.Register;

public sealed class RegisterUserUseCase : IRegisterUserUseCase
{
    private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
    private readonly IUserReadOnlyRepository _userReadOnlyRepository;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenGenerator _accessTokenGenerator;
    private readonly IWalletWriteOnlyRepository _walletWriteOnlyRepository;

    public RegisterUserUseCase(
        IUserWriteOnlyRepository userWriteOnlyRepository,
        IUserReadOnlyRepository userReadOnlyRepository,
        IMapper mapper,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IAccessTokenGenerator accessTokenGenerator,
        IWalletWriteOnlyRepository walletWriteOnlyRepository)
    {
        _userWriteOnlyRepository = userWriteOnlyRepository;
        _userReadOnlyRepository = userReadOnlyRepository;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _accessTokenGenerator = accessTokenGenerator;
        _walletWriteOnlyRepository = walletWriteOnlyRepository;
    }

    public async Task<AuthenticatedUserResponse> Execute(
        RegisterUserRequest request,
        CancellationToken cancellationToken)
    {
        await Validate(request, cancellationToken);

        Domain.Entities.User user = _mapper.Map<Domain.Entities.User>(request);
        user.Password = _passwordHasher.Hash(user, request.Password);
        user.UserIdentifier = Guid.NewGuid();

        await _userWriteOnlyRepository.AddAsync(user, cancellationToken);
        await _walletWriteOnlyRepository.AddAsync(
            new Domain.Entities.Wallet { User = user },
            cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return new AuthenticatedUserResponse
        {
            Name = user.Name,
            Tokens = new AccessTokenResponse
            {
                AccessToken = _accessTokenGenerator.Generate(user.UserIdentifier),
            },
        };
    }

    private async Task Validate(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        RegisterUserValidator validator = new();
        FluentValidation.Results.ValidationResult result = await validator.ValidateAsync(
            request,
            cancellationToken);

        bool emailExists = await _userReadOnlyRepository.ExistsActiveUserWithEmailAsync(
            request.Email,
            cancellationToken);

        if (emailExists)
        {
            result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                nameof(request.Email),
                ResourcesMessagesException.EMAIL_ALREADY_REGISTERED));
        }

        if (result.IsValid is false)
            throw new ErrorOnValidationException(result.Errors.Select(error => error.ErrorMessage).ToList());
    }
}
