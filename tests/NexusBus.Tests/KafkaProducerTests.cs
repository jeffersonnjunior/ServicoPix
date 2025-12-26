using Confluent.Kafka;
using NexusBus.Configuration;
using NexusBus.Providers.Kafka;
using Xunit;

namespace NexusBus.Tests;

public class KafkaProducerTests
{
    [Fact]
    public void ApplySecurity_sets_security_properties_when_configured()
    {
        var options = new KafkaOptions
        {
            SecurityProtocol = "SaslSsl",
            SaslMechanism = "ScramSha512",
            SaslUsername = "user",
            SaslPassword = "pass"
        };

        var config = new ClientConfig();

        KafkaProducer.ApplySecurity(config, options);

        Assert.Equal(SecurityProtocol.SaslSsl, config.SecurityProtocol);
        Assert.Equal(SaslMechanism.ScramSha512, config.SaslMechanism);
        Assert.Equal("user", config.SaslUsername);
        Assert.Equal("pass", config.SaslPassword);
    }

    [Fact]
    public void ApplySecurity_does_not_throw_when_values_are_invalid_or_empty()
    {
        var options = new KafkaOptions
        {
            SecurityProtocol = "not-a-real-value",
            SaslMechanism = "also-not-real",
            SaslUsername = null,
            SaslPassword = null
        };

        var config = new ClientConfig();

        KafkaProducer.ApplySecurity(config, options);

        Assert.Null(config.SaslUsername);
        Assert.Null(config.SaslPassword);
    }
}
