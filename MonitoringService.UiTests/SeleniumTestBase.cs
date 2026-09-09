using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace MonitoringService.UiTests;

public abstract class SeleniumTestBase
{
    protected IWebDriver Driver = null!;
    protected WebDriverWait Wait = null!;

    protected static string BaseUrl =>
        Environment.GetEnvironmentVariable("SM_WEB_URL") ?? "http://localhost:5173";

    protected static string AdminUser =>
        Environment.GetEnvironmentVariable("SM_TEST_USER") ?? "admin";

    protected static string AdminPassword =>
        Environment.GetEnvironmentVariable("SM_TEST_PASSWORD") ?? "Admin@123456";

    [SetUp]
    public void SetUpDriver()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--window-size=1400,900");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--no-sandbox");

        Driver = new ChromeDriver(options);
        Wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(20));
        Wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));
    }

    [TearDown]
    public void TearDownDriver()
    {
        Driver?.Quit();
        Driver?.Dispose();
    }

    protected void LoginAsAdmin()
    {
        var session = TestAuthHelper.CreateSession(AdminUser, AdminPassword);
        TestAuthHelper.ApplySession(Driver, BaseUrl, session);
        Driver.Navigate().GoToUrl($"{BaseUrl}/");
        WaitUntilVisible(UiSelectors.DashboardHeading);
    }

    protected void LoginAsAdminViaUi()
    {
        GoToLogin();
        Driver.FindElement(UiSelectors.LoginUsername).SendKeys(AdminUser);
        Driver.FindElement(UiSelectors.LoginPassword).SendKeys(AdminPassword);
        Driver.FindElement(UiSelectors.LoginSubmit).Click();
        WaitUntilVisible(UiSelectors.DashboardHeading);
    }

    protected void GoToLogin()
    {
        Driver.Navigate().GoToUrl($"{BaseUrl}/login");
        WaitUntilVisible(UiSelectors.LoginUsername);
    }

    protected void WaitUntilVisible(By locator)
    {
        Wait.Until(d =>
        {
            var element = d.FindElement(locator);
            return element.Displayed;
        });
    }

    protected void WaitForUrlContains(string fragment)
    {
        Wait.Until(d => d.Url.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class UiSelectors
{
    public static readonly By LoginUsername = By.CssSelector("input[placeholder='you@company.com']");
    public static readonly By LoginPassword = By.CssSelector("form.auth-form input[type='password']");
    public static readonly By LoginSubmit = By.XPath("//form[contains(@class,'auth-form')]//button[@type='submit']");
    public static readonly By LoginError = By.CssSelector(".error-banner");
    public static readonly By DashboardHeading = By.XPath("//h1[normalize-space()='Dashboard']");
    public static readonly By AlertsHeading = By.XPath("//h1[normalize-space()='Alerts']");
    public static readonly By DevicesHeading = By.XPath("//h1[normalize-space()='Devices']");
    public static readonly By NavAlerts = By.CssSelector("a[href='/alerts']");
    public static readonly By NavDevices = By.CssSelector("a[href='/devices']");
    public static readonly By NavDashboard = By.XPath("//nav//a[normalize-space()='Dashboard']");
    public static readonly By SignOut = By.XPath("//button[contains(normalize-space(), 'Sign out')]");
}
