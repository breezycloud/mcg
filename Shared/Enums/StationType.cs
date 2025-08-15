using System.ComponentModel;

public enum StationType
{
    [Description("Loading Depot")]
    LoadingDepot,
    [Description("Receiving Depot")]
    ReceivingDepot,
    [Description("Refuelling Station")]
    RefuellingStation,
    [Description("Discharge Station")]
    DischargeStation    
}