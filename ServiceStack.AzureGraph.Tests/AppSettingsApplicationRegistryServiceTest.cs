using System;
using FluentAssertions;
using NUnit.Framework;
using ServiceStack.AzureGraph.ServiceModel.Entities;
using ServiceStack.AzureGraph.Services;
using ServiceStack.Configuration;

namespace ServiceStack.AzureGraph.Tests
{
    public class AppSettingsApplicationRegistryServiceTest
    {
        private const string AppId = "e7319d8d-bf2b-4aca-a794-8a9f949ef80b";
        private const string PublicKey = "8ccdacd6a78a4b3891c3097c16fe29bb";
        private const string DirectoryName = "foo.onmicrosoft.com";
        public AppSettingsApplicationRegistryService Service { get; set; }

        [SetUp]
        public void Setup()
        {
            var appSettings = new AppSettings();
            appSettings.Set(AppSettingsApplicationRegistryService.ConfigSettings.GetClientIdKey(), AppId);
            appSettings.Set(AppSettingsApplicationRegistryService.ConfigSettings.GetClientSecretKey(), PublicKey);
            appSettings.Set(AppSettingsApplicationRegistryService.ConfigSettings.GetDirectoryNameKey(), DirectoryName);
            Service = new AppSettingsApplicationRegistryService(appSettings);
        }

        [Test]
        public void ShouldReturnRegistrationWithMatchingDirectoryName()
        {
            var reg = Service.GetApplicationByDirectoryName(DirectoryName);

            reg.Should().NotBeNull();
            reg.ClientId.Should().Be(AppId);
            reg.ClientSecret.Should().Be(PublicKey);
            reg.DirectoryName.Should().Be(DirectoryName);
        }

        [Test]
        public void ShouldReturnConfiguredRegistrationWhenDirectoryNameDoesNotMatch()
        {
            var reg = Service.GetApplicationByDirectoryName("zzz" + DirectoryName);

            reg.Should().NotBeNull();
            reg.ClientId.Should().Be(AppId);
            reg.ClientSecret.Should().Be(PublicKey);
            reg.DirectoryName.Should().Be(DirectoryName);
        }

        [Test]
        public void ShouldReturnRegistrationWithMatchingApplicationId()
        {
            var reg = Service.GetApplicationById(AppId);

            reg.Should().NotBeNull();
            reg.ClientId.Should().Be(AppId);
            reg.ClientSecret.Should().Be(PublicKey);
            reg.DirectoryName.Should().Be(DirectoryName);
        }

        [Test]
        public void ShouldReturnConfiguredRegistrationWhenApplicationIdDoesNotMatch()
        {
            var reg = Service.GetApplicationByDirectoryName(Guid.NewGuid().ToString());

            reg.Should().NotBeNull();
            reg.ClientId.Should().Be(AppId);
            reg.ClientSecret.Should().Be(PublicKey);
            reg.DirectoryName.Should().Be(DirectoryName);
        }

        [Test]
        public void ShouldIdentifyRegisteredApplication()
        {
            var isRegistered = Service.ApplicationIsRegistered(DirectoryName);
            isRegistered.Should().Be(true);
        }

        [Test]
        public void ShouldNotIdentifyUnRegisteredApplication()
        {
            var isRegistered = Service.ApplicationIsRegistered("zzz" + DirectoryName);
            isRegistered.Should().Be(false);
        }

        [Test]
        public void ShouldNotRegisterNewApplicationAtRuntime()
        {
            var registration = new ApplicationRegistration
            {
                ClientId = Guid.NewGuid().ToString(),
                DirectoryName = Guid.NewGuid().ToString("N"),
                ClientSecret = Guid.NewGuid().ToString("N")
            };

            Action tryRegister = () => Service.RegisterApplication(registration);

            tryRegister.ShouldThrow<NotImplementedException>();

        }
    }
}