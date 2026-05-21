using IndomitableSpire2.IndomitableSpire2Code.Localization.DynamicVars;
using IndomitableSpire2.IndomitableSpire2Code.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class DynamicVarSetExtensions
{
    public static DynamicVar Durability(this DynamicVarSet  vars) =>vars[DurabilityVar.DefaultName];
    
    public static DynamicVar MaxDurability(this DynamicVarSet  vars) => vars[MaxDurabilityVar.DefaultName];
    
    public static DynamicVar MotivationGain(this DynamicVarSet  vars) => vars[nameof(MotivationGain)];
    
    public static DynamicVar MotivationRequire(this DynamicVarSet  vars) => vars[nameof(MotivationRequire)];
    
    public static DynamicVar MotivationConsume(this DynamicVarSet  vars) => vars[nameof(MotivationConsume)];
    
    public static DynamicVar ArmorBreak(this DynamicVarSet  vars) => vars[nameof(ArmorBreakPower)];
    
    public static DynamicVar Aviation(this DynamicVarSet  vars) => vars[nameof(AviationPower)];
    
    public static DynamicVar Flooding(this DynamicVarSet  vars) => vars[nameof(FloodingPower)];
    
    public static DynamicVar OnFire(this DynamicVarSet  vars) => vars[nameof(OnFirePower)];
    
    public static DynamicVar Hypnotized(this DynamicVarSet  vars) => vars[nameof(HypnotizedPower)];
    
    public static DynamicVar Intercepted(this DynamicVarSet  vars) => vars[nameof(InterceptedPower)];
    
    public static DynamicVar Slow(this DynamicVarSet  vars) => vars[nameof(SlowPower)];
    
    public static DamageMultiplierVar DamageMultiplier(this DynamicVarSet vars) => 
        (DamageMultiplierVar) vars[DamageMultiplierVar.DefaultName];
    
    public static ReconVar Recon(this DynamicVarSet  vars) => (ReconVar) vars[ReconVar.DefaultName];
    
    public static ShieldVar Shield(this DynamicVarSet  vars) => (ShieldVar) vars[ShieldVar.DefaultName];
}