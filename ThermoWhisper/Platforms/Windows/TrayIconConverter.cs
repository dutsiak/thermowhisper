using System.Globalization;

using ThermoWhisper.Application.Power;

namespace ThermoWhisper.Platforms.Windows;

/// <summary>Maps the tray icon kind to its .ico asset (MauiAsset file names in ThermoWhisper.csproj).</summary>
public sealed class TrayIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        ImageSource.FromFile(value is TrayIconKind.Slowed ? "tray-slowed.ico" : "tray-normal.ico");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
