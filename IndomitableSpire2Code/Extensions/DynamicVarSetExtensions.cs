using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class DynamicVarSetExtensions
{
    public static DynamicVar Durability(this DynamicVarSet  vars) =>vars[DurabilityVar.DefaultName];
    
    public static DynamicVar MaxDurability(this DynamicVarSet  vars) => vars[MaxDurabilityVar.DefaultName];
    
    public static DynamicVar MotivationGain(this DynamicVarSet  vars) => vars[nameof(MotivationGain)];
    
    public static DynamicVar MotivationRequire(this DynamicVarSet  vars) => vars[nameof(MotivationRequire)];
    
    public static DynamicVar MotivationConsume(this DynamicVarSet  vars) => vars[nameof(MotivationConsume)];
    
    public static DynamicVar ArmorBreak(this DynamicVarSet  vars) => vars[nameof(ArmorBreakPower)];
    
    public static DynamicVar Flooding(this DynamicVarSet  vars) => vars[nameof(FloodingPower)];
    
    public static DynamicVar OnFire(this DynamicVarSet  vars) => vars[nameof(OnFirePower)];
    
    public static DynamicVar Intercepted(this DynamicVarSet  vars) => vars[nameof(InterceptedPower)];
    
    public static DynamicVar Recon(this DynamicVarSet  vars) => vars[ReconVar.DefaultName];
}