namespace RobotsPlugin.ViewModels
{
    using System.ComponentModel;

    public class CustomUserAgentViewModel
    {
        #region Properties

        [DisplayName("User Agent")]
        public string UserAgent { get; set; }

        [DisplayName("Route")]
        public string Route { get; set; }

        [DisplayName("Comment")]
        public string? Comment { get; set; }

        [DisplayName("Is Allowed")]
        public bool IsAllowed { get; set; }

        [DisplayName("Is Custom")]
        public bool IsCustom { get; set; }

        #endregion

        #region Constructor

        public CustomUserAgentViewModel()
        {
        }

        public CustomUserAgentViewModel(string userAgent, string route, string? comment = null, bool isAllowed = false, bool isCustom = false)
        {
            if (string.IsNullOrEmpty(userAgent))
                throw new ArgumentNullException(nameof(userAgent));

            if (string.IsNullOrEmpty(route))
                throw new ArgumentNullException(nameof(route));

            UserAgent = userAgent;
            Route = route;
            Comment = comment ?? string.Empty;
            IsAllowed = isAllowed;
            IsCustom = isCustom;
        }

        #endregion
    }
}