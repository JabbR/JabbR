using JabbR.Infrastructure;
using JabbR.Services;
using JabbR.WebApi;
using JabbR.WebApi.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Hosting;
using Xunit;

namespace JabbR.Test
{
    public class ApiFrontPageControllerFacts
    {
        public class GetFrontPage
        {

            [Fact]
            public void ShouldOutputAuthEndpoint()
            {
                Mock<IVirtualPathUtility> virtualPathMock = new Mock<IVirtualPathUtility>();
                virtualPathMock.Setup(vp => vp.ToAbsolute(It.IsAny<string>())).Returns("/Auth/Login.ashx");

                Mock<IApplicationSettings> appSettingsMock = new Mock<IApplicationSettings>();

                ApiFrontPageController controller = new ApiFrontPageController(virtualPathMock.Object, appSettingsMock.Object);
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com/api");
                requestMessage.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
                requestMessage.SetIsLocal(true);

                controller.Request = requestMessage;

                var responseData = controller.GetFrontPage().Content as ObjectContent;

                Assert.Equal("http://example.com/Auth/Login.ashx", ((ApiFrontpageModel)responseData.Value).Auth.AuthUri);
            }

            [Fact]
            public void ShouldOutputAppId()
            {
                Mock<IVirtualPathUtility> virtualPathMock = new Mock<IVirtualPathUtility>();
                virtualPathMock.Setup(vp => vp.ToAbsolute(It.IsAny<string>())).Returns("/Auth/Login.ashx");

                Mock<IApplicationSettings> appSettingsMock = new Mock<IApplicationSettings>();
                appSettingsMock.Setup(a => a.AuthAppId).Returns("theAppId");

                ApiFrontPageController controller = new ApiFrontPageController(virtualPathMock.Object, appSettingsMock.Object);
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com/api");
                requestMessage.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
                requestMessage.SetIsLocal(true);

                controller.Request = requestMessage;


                var responseData = controller.GetFrontPage().Content as ObjectContent;

                Assert.Equal("theAppId", ((ApiFrontpageModel)responseData.Value).Auth.JanrainAppId);
            }
            [Fact]
            public void ShouldUsePortWhenLocal()
            {
                Mock<IVirtualPathUtility> virtualPathMock = new Mock<IVirtualPathUtility>();
                virtualPathMock.Setup(vp => vp.ToAbsolute(It.IsAny<string>())).Returns("/Auth/Login.ashx");

                Mock<IApplicationSettings> appSettingsMock = new Mock<IApplicationSettings>();

                ApiFrontPageController controller = new ApiFrontPageController(virtualPathMock.Object, appSettingsMock.Object);
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com:1067/api");
                requestMessage.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
                requestMessage.SetIsLocal(true);

                controller.Request = requestMessage;

                var responseData = controller.GetFrontPage().Content as ObjectContent;

                Assert.Equal("http://example.com:1067/Auth/Login.ashx", ((ApiFrontpageModel)responseData.Value).Auth.AuthUri);
            }
            [Fact]
            public void ShouldUsePort80WhenNotLocal()
            {
                Mock<IVirtualPathUtility> virtualPathMock = new Mock<IVirtualPathUtility>();
                virtualPathMock.Setup(vp => vp.ToAbsolute(It.IsAny<string>())).Returns("/Auth/Login.ashx");

                Mock<IApplicationSettings> appSettingsMock = new Mock<IApplicationSettings>();

                ApiFrontPageController controller = new ApiFrontPageController(virtualPathMock.Object, appSettingsMock.Object);
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com:1067/api");
                requestMessage.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
                requestMessage.SetIsLocal(false);

                controller.Request = requestMessage;

                var responseData = controller.GetFrontPage().Content as ObjectContent;

                Assert.Equal("http://example.com/Auth/Login.ashx", ((ApiFrontpageModel)responseData.Value).Auth.AuthUri);
            }
            [Fact]
            public void ShouldUseHttpsWhenXForwardedProtoIsHttps()
            {
                Mock<IVirtualPathUtility> virtualPathMock = new Mock<IVirtualPathUtility>();
                virtualPathMock.Setup(vp => vp.ToAbsolute(It.IsAny<string>())).Returns("/Auth/Login.ashx");

                Mock<IApplicationSettings> appSettingsMock = new Mock<IApplicationSettings>();

                ApiFrontPageController controller = new ApiFrontPageController(virtualPathMock.Object, appSettingsMock.Object);
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://example.com:1067/api");
                requestMessage.Properties[HttpPropertyKeys.HttpConfigurationKey] = new HttpConfiguration();
                requestMessage.SetIsLocal(false);
                requestMessage.Headers.Add("X-Forwarded-Proto", "https");

                controller.Request = requestMessage;

                var responseData = controller.GetFrontPage().Content as ObjectContent;

                Assert.Equal("https://example.com/Auth/Login.ashx", ((ApiFrontpageModel)responseData.Value).Auth.AuthUri);
            }
        }
    }
}
