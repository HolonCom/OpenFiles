#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using DotNetNuke.Web.Api;

#endregion

namespace Satrabel.OpenFiles.Components
{
    public class OpenFilesRouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute("OpenFiles", "default", "{controller}/{action}", new[] { "Satrabel.OpenFiles.Components", "Satrabel.OpenFiles.Components.JPList" });
        }
    }
} 

