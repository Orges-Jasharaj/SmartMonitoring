using NUnit.Framework;
using SmartMonitoring.Shared.Dtos.Responses;

namespace SmartMonitoring.Tests;

[TestFixture]
public class ResponseDtoTests
{
    [Test]
    public void CI001_SuccessResponse_SetsSuccessTrue()
    {
        var response = ResponseDto<string>.SuccessResponse("ok", "Created");

        Assert.That(response.Success, Is.True);
        Assert.That(response.Data, Is.EqualTo("ok"));
        Assert.That(response.Message, Is.EqualTo("Created"));
    }

    [Test]
    public void CI002_Failure_SetsSuccessFalse()
    {
        var response = ResponseDto<int>.Failure("Validation failed");

        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Validation failed"));
        Assert.That(response.Data, Is.EqualTo(default(int)));
    }

    [Test]
    public void CI003_Failure_IncludesErrors()
    {
        var errors = new List<ApiError> { new() { ErrorMessage = "Required field" } };
        var response = ResponseDto<bool>.Failure("Invalid", errors);

        Assert.That(response.Errors, Has.Count.EqualTo(1));
        Assert.That(response.Errors![0].ErrorMessage, Is.EqualTo("Required field"));
    }
}
