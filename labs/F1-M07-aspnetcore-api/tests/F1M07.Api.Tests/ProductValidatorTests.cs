using F1M07.Api.Products;

namespace F1M07.Api.Tests;

/// <summary>Passo 1: regras de validação puras, sem HTTP.</summary>
public class ProductValidatorTests
{
    [Fact]
    public void Validate_RequestValido_RetornaDicionarioVazio()
    {
        ProductValidator.Validate(new ProductRequest("Mouse", 99.90m)).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_SemNome_RetornaErroEmName(string? name)
    {
        var errors = ProductValidator.Validate(new ProductRequest(name, 10m));

        errors.Keys.ShouldBe(["Name"]);
    }

    [Fact]
    public void Validate_NomeMaiorQue100_RetornaErroEmName()
    {
        var errors = ProductValidator.Validate(new ProductRequest(new string('x', 101), 10m));

        errors.ShouldContainKey("Name");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PrecoNaoPositivo_RetornaErroEmPrice(decimal price)
    {
        var errors = ProductValidator.Validate(new ProductRequest("Mouse", price));

        errors.Keys.ShouldBe(["Price"]);
    }

    [Fact]
    public void Validate_VariosProblemas_RetornaTodosOsCampos()
    {
        var errors = ProductValidator.Validate(new ProductRequest(null, 0m));

        errors.Keys.ShouldBe(["Name", "Price"], ignoreOrder: true);
    }
}
