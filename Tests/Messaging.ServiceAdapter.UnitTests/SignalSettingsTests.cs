using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace TheAssistant.Messaging.ServiceAdapter.UnitTests;

public class SignalSettingsTests
{
    [Fact]
    public void ValidationShouldFailWhenRequiredFieldsMissing()
    {
        var settings = new TheAssistant.Messaging.ServiceAdapter.SignalSettings();
        var context = new ValidationContext(settings, null, null);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(settings, context, results, true);

        valid.Should().BeFalse();
        results.Should().NotBeEmpty();
    }

    [Fact]
    public void ValidationShouldSucceedWhenValid()
    {
        var settings = new TheAssistant.Messaging.ServiceAdapter.SignalSettings { BaseUrl = "http://localhost", PhoneNumber = "+100" };
        var context = new ValidationContext(settings, null, null);
        var results = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(settings, context, results, true);

        valid.Should().BeTrue();
        results.Should().BeEmpty();
    }
}
