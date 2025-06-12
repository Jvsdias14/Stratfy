using Xunit;
using NSubstitute;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using STRATFY.Controllers;
using STRATFY.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Http; // Para DefaultHttpContext

public class HomeControllerTests
{
    private readonly ILogger<HomeController> _mockLogger;
    private readonly HomeController _controller;

    public HomeControllerTests()
    {
        _mockLogger = Substitute.For<ILogger<HomeController>>();
        _controller = new HomeController(_mockLogger);

        // Configurar um HttpContext para que Activity.Current e HttpContext.TraceIdentifier funcionem em testes
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void Index_ReturnsViewResult()
    {
        // Act
        var result = _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull(); // Significa que ele usará a view padrão "Index"
        viewResult.Model.Should().BeNull(); // Não há modelo sendo passado
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        // Act
        var result = _controller.Privacy();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.ViewName.Should().BeNull(); // Significa que ele usará a view padrão "Privacy"
        viewResult.Model.Should().BeNull(); // Não há modelo sendo passado
    }

    [Fact]
    public void Error_ReturnsViewResultWithCorrectViewModelAndRequestId()
    {
        // Arrange
        // Mock HttpContext.TraceIdentifier para um valor previsível
        var traceIdentifier = "trace-123";
        _controller.HttpContext.TraceIdentifier = traceIdentifier;

        // Simular um Activity.Current (para garantir que ele seja usado se presente)
        // Isso é mais complexo, mas para um teste simples, podemos focar no HttpContext.TraceIdentifier
        // Se você quer testar Activity.Current, precisaria de um using System.Threading.Tasks; e fazer assim:
        // var activity = new Activity("test-activity").Start();
        // Activity.Current = activity; // Isso altera o estado global, use com cautela em testes unitários.
        // É mais comum mockar o HttpContext.TraceIdentifier diretamente.

        // Act
        var result = _controller.Error();

        // Assert
        result.Should().BeOfType<ViewResult>();
        var viewResult = result.As<ViewResult>();
        viewResult.Model.Should().BeOfType<ErrorViewModel>();
        var errorModel = viewResult.Model.As<ErrorViewModel>();

        // Verifica se o RequestId é o TraceIdentifier do HttpContext
        errorModel.RequestId.Should().Be(traceIdentifier);
        // O Id do Activity.Current pode ser nulo se não houver um Activity ativo,
        // então o fallback para HttpContext.TraceIdentifier é importante.
        // Se Activity.Current fosse mockado para ter um Id, poderíamos verificar isso também.

        viewResult.ViewName.Should().BeNull(); // Ou "Error", se você tivesse especificado. Por convenção é nulo.
    }
}