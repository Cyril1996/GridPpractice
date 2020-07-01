using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI;//IExternalCommand用
using Autodesk.Revit.DB;//Document用
using Autodesk.Revit.Attributes;//TransactionAttribute和RegenerationAttribute
using Autodesk.Revit.DB.Structure;//StructuralType.NonStructural用

namespace GridPractise
{
    [TransactionAttribute(TransactionMode.Manual)]
    [RegenerationAttribute(RegenerationOption.Manual)]
    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;//定义用户文件夹
            Document revitDoc = uidoc.Document;//将uidoc转换为Document
            FilteredElementCollector coll = new FilteredElementCollector(revitDoc);//创建元素过滤收集器
            ElementClassFilter gridFilter = new ElementClassFilter(typeof(Grid));//过滤类型为轴网的元素
            List<Element> grid = coll.WherePasses(gridFilter).ToElements().ToList();//创建轴网
            List<Line> gridLines = new List<Line>();//创建轴线
            List<XYZ> intPos = new List<XYZ>();//创建交点
            //找到轴网交点
            foreach (Grid gri in grid)
            {
                gridLines.Add(gri.Curve as Line);//将轴网转换为线
            }
            foreach (Line ln1 in gridLines)//找到第一根线
            {
                foreach (Line ln2 in gridLines)//找到第二根线
                {
                    XYZ normal1 = ln1.Direction;//得到的是直线的方向向量
                    XYZ normal2 = ln2.Direction;
                    if (normal1.IsAlmostEqualTo(normal2))
                    {
                        continue;//如果两根轴线方向相同,则遍历下一组
                    }
                    IntersectionResultArray results;//交点数组
                    SetComparisonResult intRst = ln1.Intersect(ln2, out results);//枚举，判断相交类型;如果两根轴线相交,则输出交点
                    if (intRst == SetComparisonResult.Overlap && results.Size == 1)//除去重复的交点
                    {
                        XYZ tp = results.get_Item(0).XYZPoint;//获取不重复的点
                        if (intPos.Where(m => m.IsAlmostEqualTo(tp)).Count() == 0)//比较得到的交点和intPos数组里面的元素是否相同，不同才Add到intPos数组中，作用是排除重复的点
                        {
                            intPos.Add(tp);//收集所有的交点
                        }
                    }
                }
            }

            Level level = revitDoc.GetElement(new ElementId(13071)) as Level;//ID为放置层标高ID
            FamilySymbol familysymbol=revitDoc.GetElement(new ElementId(72254)) as FamilySymbol;//ID为放置族类型的ID，不是族实例ID，不是具体的柱子

            using (Transaction trans = new Transaction(revitDoc))
            {
                trans.Start("dfs");
                if (!familysymbol.IsActive) familysymbol.Activate();//判断familysymbol是否为active，不是则设为Active状态
                foreach (XYZ p in intPos)
                {
                    FamilyInstance familyInstance = revitDoc.Create.NewFamilyInstance(p, familysymbol, level, StructuralType.NonStructural);
                }
                trans.Commit();
            }
                return Result.Succeeded;
        }
    }
}
