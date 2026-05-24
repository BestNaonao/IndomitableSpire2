namespace IndomitableSpire2.IndomitableSpire2Code.Extensions;

public static class EnumerableExtensions
{
    /// <summary>
    /// 按照给定的谓词（条件）优先级顺序筛选集合。
    /// 一旦某个谓词筛选出至少一个元素，即刻返回该结果集合。
    /// 如果所有谓词都未能筛选出元素，则返回空集合。
    /// </summary>
    public static IEnumerable<T> GetByPriority<T>(
        this IEnumerable<T> source, 
        params Func<T, bool>[] priorities)
    {
        // 防御性编程：为了避免 IEnumerable 被多次重复遍历带来性能开销，先将其具象化为集合
        var sourceList = source as IReadOnlyList<T> ?? source.ToList();
        if (sourceList.Count == 0) return [];
        
        // 依次计算每个优先级的条件
        foreach (var predicate in priorities)
        {
            var results = sourceList.Where(predicate).ToList();
            if (results.Count > 0)
                return results;
        }
        
        // 所有优先级条件都未命中
        return [];
    }
}