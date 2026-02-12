using MudBlazor;

namespace TrainingApp.Web.Theme;

public static class AppTheme
{
    public static MudTheme Default => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1565C0",
            Secondary = "#00897B",
            AppbarBackground = "#1565C0",
            Background = "#FAFAFA",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#424242",
            TextPrimary = "#212121",
            TextSecondary = "#757575"
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7C3AED",
            Secondary = "#00E676",
            Tertiary = "#EC4899",
            AppbarBackground = "#0A0A0F",
            Background = "#0A0A0F",
            Surface = "#12121A",
            DrawerBackground = "#12121A",
            DrawerText = "#FAFAFE",
            DrawerIcon = "#9CA3AF",
            TextPrimary = "#FAFAFE",
            TextSecondary = "#9CA3AF",
            ActionDefault = "#9CA3AF",
            ActionDisabled = "#4B5563",
            Divider = "#1F1F2E",
            DividerLight = "#1F1F2E",
            TableLines = "#1F1F2E",
            LinesDefault = "#1F1F2E",
            Success = "#00E676",
            Warning = "#F59E0B",
            Error = "#EC4899",
            Info = "#42A5F5"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"]
            },
            H4 = new H4Typography
            {
                FontWeight = "700"
            },
            H5 = new H5Typography
            {
                FontWeight = "600"
            },
            H6 = new H6Typography
            {
                FontWeight = "600"
            }
        }
    };
}
