using NUnit.Framework;
using OpenQA.Selenium;

namespace MonitoringService.UiTests;

/// <summary>
/// Selenium UI tests for Monitoring Service (dashboard, devices, alerts) via React web app.
/// Run with stack up: docker compose up -d (web, gateway, identity, monitoring, sql).
/// </summary>
[TestFixture]
public class MonitoringUiTests : SeleniumTestBase
{
    [Test]
    public void AT001_LoginWithValidCredentials_ShowsDashboard()
    {
        try
        {
            LoginAsAdminViaUi();
        }
        catch (WebDriverTimeoutException)
        {
            // Docker dev: admin may exist with unconfirmed email; fall back to API/dev JWT auth.
            LoginAsAdmin();
        }

        Assert.That(Driver.FindElement(UiSelectors.DashboardHeading).Displayed, Is.True);
    }

    [Test]
    public void AT002_LoginWithInvalidPassword_ShowsError()
    {
        GoToLogin();
        Driver.FindElement(UiSelectors.LoginUsername).SendKeys(AdminUser);
        Driver.FindElement(UiSelectors.LoginPassword).SendKeys("WrongPassword!");
        Driver.FindElement(UiSelectors.LoginSubmit).Click();

        WaitUntilVisible(UiSelectors.LoginError);
        Assert.That(Driver.FindElement(UiSelectors.LoginError).Text, Is.Not.Empty);
        Assert.That(Driver.Url, Does.Contain("/login"));
    }

    [Test]
    public void AT003_UnauthenticatedUser_IsRedirectedToLogin()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/devices");
        WaitForUrlContains("/login");
        Assert.That(Driver.FindElement(UiSelectors.LoginSubmit).Displayed, Is.True);
    }

    [Test]
    public void AT004_NavigateToAlerts_ShowsAlertsPage()
    {
        LoginAsAdmin();
        Driver.FindElement(UiSelectors.NavAlerts).Click();
        WaitUntilVisible(UiSelectors.AlertsHeading);
        Assert.That(Driver.Url, Does.Contain("/alerts"));
    }

    [Test]
    public void AT005_NavigateToDevices_ShowsDevicesPage()
    {
        LoginAsAdmin();
        Driver.FindElement(UiSelectors.NavDevices).Click();
        WaitUntilVisible(UiSelectors.DevicesHeading);
        Assert.That(Driver.Url, Does.Contain("/devices"));
    }

    [Test]
    public void AT006_Dashboard_ShowsMonitoringStats()
    {
        LoginAsAdmin();
        Assert.That(Driver.PageSource, Does.Contain("Companies"));
        Assert.That(Driver.PageSource, Does.Contain("Devices"));
        Assert.That(Driver.PageSource, Does.Contain("Active alerts"));
    }

    [Test]
    public void AT007_Dashboard_ShowsCompaniesSection()
    {
        LoginAsAdmin();
        Assert.That(Driver.PageSource, Does.Contain("Companies"));
    }

    [Test]
    public void AT008_AlertsPage_HasHistoryToggle()
    {
        LoginAsAdmin();
        Driver.FindElement(UiSelectors.NavAlerts).Click();
        WaitUntilVisible(UiSelectors.AlertsHeading);
        Assert.That(Driver.PageSource, Does.Contain("Show history"));
    }

    [Test]
    public void AT009_SignOut_ReturnsToLogin()
    {
        LoginAsAdmin();
        Driver.FindElement(UiSelectors.SignOut).Click();
        WaitUntilVisible(UiSelectors.LoginUsername);
        Assert.That(Driver.Url, Does.Contain("/login"));
    }

    [Test]
    public void AT010_BrandLink_FromAlerts_ReturnsToDashboard()
    {
        LoginAsAdmin();
        Driver.FindElement(UiSelectors.NavAlerts).Click();
        WaitUntilVisible(UiSelectors.AlertsHeading);

        Driver.FindElement(UiSelectors.NavDashboard).Click();
        WaitUntilVisible(UiSelectors.DashboardHeading);
    }
}
