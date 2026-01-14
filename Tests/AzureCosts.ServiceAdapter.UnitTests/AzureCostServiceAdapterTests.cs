using Azure.Core;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using TheAssistant.AzureCosts.ServiceAdapter;
using TheAssistant.Core.AzureCosts;

namespace TheAssistant.AzureCosts.ServiceAdapter.UnitTests
{
    public class AzureCostServiceAdapterTests
    {
        private readonly Mock<ILogger<AzureCostServiceAdapter>> mockLogger;
        private readonly Mock<IHttpClientFactory> mockHttpClientFactory;
        private readonly Mock<TokenCredential> mockTokenCredential;
        private readonly Mock<HttpMessageHandler> mockHttpHandler;
        private readonly IOptions<AzureCostSettings> settings;

        public AzureCostServiceAdapterTests()
        {
            mockLogger = new Mock<ILogger<AzureCostServiceAdapter>>();
            mockHttpClientFactory = new Mock<IHttpClientFactory>();
            mockTokenCredential = new Mock<TokenCredential>();
            mockHttpHandler = new Mock<HttpMessageHandler>();

            settings = Options.Create(new AzureCostSettings
            {
                SubscriptionId = "test-subscription-id"
            });

            var httpClient = new HttpClient(mockHttpHandler.Object)
            {
                BaseAddress = new Uri("https://management.azure.com")
            };

            mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            mockTokenCredential.Setup(x => x.GetTokenAsync(
                    It.IsAny<TokenRequestContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AccessToken("test-token", DateTimeOffset.UtcNow.AddHours(1)));
        }

        [Fact]
        public void ConstructorWithNullSettingsShouldThrow()
        {
            var act = () => new AzureCostServiceAdapter(
                null!,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                mockTokenCredential.Object);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("settings");
        }

        [Fact]
        public void ConstructorWithNullLoggerShouldThrow()
        {
            var act = () => new AzureCostServiceAdapter(
                settings,
                null!,
                mockHttpClientFactory.Object,
                mockTokenCredential.Object);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void ConstructorWithNullTokenCredentialShouldThrow()
        {
            var act = () => new AzureCostServiceAdapter(
                settings,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("tokenCredential");
        }

        [Fact]
        public void ConstructorWithNullSubscriptionIdShouldThrow()
        {
            var invalidSettings = Options.Create(new AzureCostSettings
            {
                SubscriptionId = null
            });

            var act = () => new AzureCostServiceAdapter(
                invalidSettings,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                mockTokenCredential.Object);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*SubscriptionId is required*");
        }

        [Fact]
        public async Task GetCurrentMonthCostsShouldReturnCostsForCurrentMonth()
        {
            SetupSuccessfulHttpResponse();
            var adapter = CreateAdapter();

            var result = await adapter.GetCurrentMonthCosts();

            result.Should().NotBeNull();
            result.TotalCost.Should().BeGreaterThanOrEqualTo(0);
            result.Currency.Should().Be("EUR");
            result.Period.Should().Contain(DateTime.UtcNow.ToString("MMMM yyyy"));
        }

        [Fact]
        public async Task GetPreviousMonthCostsShouldReturnCostsForPreviousMonth()
        {
            SetupSuccessfulHttpResponse();
            var adapter = CreateAdapter();

            var result = await adapter.GetPreviousMonthCosts();

            result.Should().NotBeNull();
            result.TotalCost.Should().BeGreaterThanOrEqualTo(0);
            result.Currency.Should().Be("EUR");
            
            var previousMonth = DateTime.UtcNow.AddMonths(-1);
            result.Period.Should().Contain(previousMonth.ToString("MMMM yyyy"));
        }

        [Fact]
        public async Task GetCostsForPeriodShouldReturnCostsForSpecificPeriod()
        {
            SetupSuccessfulHttpResponse();
            var adapter = CreateAdapter();
            var startDate = new DateTime(2024, 10, 1);
            var endDate = new DateTime(2024, 10, 31);

            var result = await adapter.GetCostsForPeriod(startDate, endDate);

            result.Should().NotBeNull();
            result.TotalCost.Should().BeGreaterThanOrEqualTo(0);
            result.Currency.Should().Be("EUR");
            result.Period.Should().Contain("2024");
        }

        [Fact]
        public async Task GetCostsForPeriodWithForbiddenResponseShouldThrowInvalidOperationException()
        {
            SetupHttpResponse(HttpStatusCode.Forbidden, "{}");
            var adapter = CreateAdapter();
            var startDate = new DateTime(2024, 10, 1);
            var endDate = new DateTime(2024, 10, 31);

            var act = () => adapter.GetCostsForPeriod(startDate, endDate);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Insufficient permissions*");
        }

        [Fact]
        public async Task GetCostsForPeriodWithServerErrorShouldThrowInvalidOperationException()
        {
            SetupHttpResponse(HttpStatusCode.InternalServerError, "{}");
            var adapter = CreateAdapter();
            var startDate = new DateTime(2024, 10, 1);
            var endDate = new DateTime(2024, 10, 31);

            var act = () => adapter.GetCostsForPeriod(startDate, endDate);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Failed to retrieve Azure costs*");
        }

        [Fact]
        public async Task GetCostsForPeriodShouldParseServicesCorrectly()
        {
            SetupSuccessfulHttpResponseWithMultipleServices();
            var adapter = CreateAdapter();
            var startDate = new DateTime(2024, 10, 1);
            var endDate = new DateTime(2024, 10, 31);

            var result = await adapter.GetCostsForPeriod(startDate, endDate);

            result.Should().NotBeNull();
            result.TopServices.Should().HaveCountGreaterThan(0);
            result.TopServices.Should().OnlyContain(s => s.Cost >= 0);
            result.TopServices.Should().OnlyContain(s => s.Percentage >= 0 && s.Percentage <= 100);
        }

        private AzureCostServiceAdapter CreateAdapter()
        {
            return new AzureCostServiceAdapter(
                settings,
                mockLogger.Object,
                mockHttpClientFactory.Object,
                mockTokenCredential.Object);
        }

        private void SetupSuccessfulHttpResponse()
        {
            var responseJson = @"{
                ""properties"": {
                    ""columns"": [
                        { ""name"": ""PreTaxCost"", ""type"": ""Number"" },
                        { ""name"": ""Currency"", ""type"": ""String"" },
                        { ""name"": ""ServiceName"", ""type"": ""String"" }
                    ],
                    ""rows"": [
                        [100.50, ""EUR"", ""Azure Functions""]
                    ]
                }
            }";

            SetupHttpResponse(HttpStatusCode.OK, responseJson);
        }

        private void SetupSuccessfulHttpResponseWithMultipleServices()
        {
            var responseJson = @"{
                ""properties"": {
                    ""columns"": [
                        { ""name"": ""PreTaxCost"", ""type"": ""Number"" },
                        { ""name"": ""Currency"", ""type"": ""String"" },
                        { ""name"": ""ServiceName"", ""type"": ""String"" }
                    ],
                    ""rows"": [
                        [100.50, ""EUR"", ""Azure Functions""],
                        [50.25, ""EUR"", ""Storage""],
                        [25.10, ""EUR"", ""Key Vault""]
                    ]
                }
            }";

            SetupHttpResponse(HttpStatusCode.OK, responseJson);
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, string content)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };

            mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }
    }
}
