using System.ComponentModel;

namespace Shared.Enums;


public enum TimeFilter
{
    [Description("This Week")]
    ThisWeek,
    [Description("Last Week")]
    LastWeek,
    [Description("This Month")]
    ThisMonth,
    [Description("Last Month")]
    LastMonth
}
