using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class DynamicVarSetExtensions
{
    public static DynamicVar Durability(this DynamicVarSet  vars) =>vars[nameof(Durability)];
    
    public static DynamicVar MaxDurability(this DynamicVarSet  vars) => vars[nameof(MaxDurability)];
    
    public static DynamicVar MotivationGain(this DynamicVarSet  vars) => vars[nameof(MotivationGain)];
    
    public static DynamicVar MotivationRequire(this DynamicVarSet  vars) => vars[nameof(MotivationRequire)];
    
    public static DynamicVar MotivationConsume(this DynamicVarSet  vars) => vars[nameof(MotivationConsume)];
    
    public static DynamicVar Recon(this DynamicVarSet  vars) => vars[nameof(Recon)];
}