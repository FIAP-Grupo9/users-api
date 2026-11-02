using FCG.Users.Api.Application.DTOs;
using FCG.Users.Api.Application.Services;
using FCG.Users.Api.Domain.Entities;
using FCG.Users.Api.Domain.Repositories;
using FCG.Users.Api.Domain.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;

namespace FCG.Users.Api.Tests;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _repoMock = new();
    private readonly Mock<ITokenService> _tokenServiceMock = new();
    private readonly Mock<IPublishEndpoint> _publisherMock = new();
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _service = new UsuarioService(
            _repoMock.Object,
            _tokenServiceMock.Object,
            _publisherMock.Object,
            Mock.Of<ILogger<UsuarioService>>());
    }

    [Fact]
    public async Task CreateUsuarioAsync_EmailJaExistente_RetornaFalha409()
    {
        var dto = new RegisterUsuarioDTO
        {
            Name = "Teste",
            Email = "existente@fcg.com",
            Password = "Senha@1234"
        };
        _repoMock.Setup(r => r.GetByEmailAsync(dto.Email!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Usuario { Email = dto.Email });

        var result = await _service.CreateUsuarioAsync(dto);

        Assert.False(result.IsSuccess);
        Assert.Equal(409, result.StatusCode);
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Usuario>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
