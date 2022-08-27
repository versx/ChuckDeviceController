namespace ChuckDeviceConfigurator.Views.Home
{
    using Microsoft.AspNetCore.Http.Features;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;

    public class PrivacyModel : PageModel
    {
        public void OnGet()
        {
        }

        public IActionResult OnPostWithdraw()
        {
            var trackingConsentFeature = HttpContext.Features.Get<ITrackingConsentFeature>();
            trackingConsentFeature?.WithdrawConsent();
            return RedirectToPage("./Index");
        }
    }
}