using AzureFunctions.Shared.Models;
using FluentAssertions;
using Xunit;

namespace AzureFunctions.Shared.Tests.Models;

public class OperationResultTests
{
    [Fact]
    public void Success_WithData_CreatesSuccessfulResultContainingData()
    {
        // Arrange
        var data = "test-data";

        // Act
        var result = OperationResult<string>.Success(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(data);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Success_WithDataAndMessage_IncludesMessage()
    {
        // Arrange
        var data = 42;
        var message = "Operation completed successfully.";

        // Act
        var result = OperationResult<int>.Success(data, message);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(42);
        result.Message.Should().Be(message);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithMessageAndErrorCode_CreatesFailedResult()
    {
        // Arrange
        var errorMessage = "Something went wrong.";
        var errorCode = "TEST_ERROR";

        // Act
        var result = OperationResult<string>.Failure(errorMessage, errorCode);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
        result.Error.ErrorCode.Should().Be(errorCode);
        result.Error.Detail.Should().BeNull();
    }

    [Fact]
    public void Failure_WithDetail_IncludesDetailInError()
    {
        // Arrange
        var errorMessage = "Processing failed.";
        var errorCode = "PROCESSING_ERROR";
        var detail = "Inner exception detail.";

        // Act
        var result = OperationResult<int>.Failure(errorMessage, errorCode, detail);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Message.Should().Be(errorMessage);
        result.Error.ErrorCode.Should().Be(errorCode);
        result.Error.Detail.Should().Be(detail);
    }

    [Fact]
    public void Success_IsSuccessIsTrue()
    {
        // Act
        var result = OperationResult<bool>.Success(true);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Failure_IsSuccessIsFalse()
    {
        // Act
        var result = OperationResult<bool>.Failure("Error", "ERR");

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void Success_WithNullMessage_HasNullMessage()
    {
        // Act
        var result = OperationResult<string>.Success("data");

        // Assert
        result.Message.Should().BeNull();
    }
}
