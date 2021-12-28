using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Core.Tasks
{
    public class TaskCenterBuilder
    {
        private static ITaskCenter center;
        private static List<TaskDetail> tasks;
       
        private TaskCenterBuilder() {
        }

        public static TaskCenterBuilder Create()
        {
            return new TaskCenterBuilder();
        }

        public TaskCenterBuilder Register(params string[] assemblies)
        {
            tasks = new List<TaskDetail>();
            foreach (var assemblyItem in assemblies)
            {
                var assembly = Assembly.Load(assemblyItem);
                var types = assembly.GetTypes();
                var methods = types.Select(p => p.GetMethods()).SelectMany(p => p).ToList();
                foreach (var method in methods)
                {
                    var attr = method.GetCustomAttribute<TaskAttribute>();
                    if (attr == null)
                        continue;//忽略无TaskAttribute属性方法

                    var identity = $"{method.ReflectedType.FullName}<{method.Name}>";
                    var parameters = method.GetParameters();
                    
                    if (!method.IsStatic && parameters.Length > 0)
                        throw new Exception($"{identity}必须为无参静态方法");
                    

                    var detail = new TaskDetail
                    {
                        Name = attr.Name,
                        Group = attr.Group,
                        Handler = FastInvoke.GetMethodInvoker(method),
                        Interval = attr.Interval,
                        StartNow = attr.StartNow,
                    };
                    tasks.Add(detail);
                }
            }
            return this;
        }
        public TaskCenterBuilder SetCenter(ITaskCenter center)
        {
            TaskCenterBuilder.center = center;
            return this;
        }

        public ITaskCenter Build()
        {
            foreach (var item in tasks)
                center.Subscribe(item);
            return center;
        }
    }
}
