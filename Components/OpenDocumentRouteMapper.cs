#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using DotNetNuke.Web.Api;

#endregion

namespace Satrabel.OpenDocument.Components
{
    public class StructRouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute mapRouteManager)
        {
            mapRouteManager.MapHttpRoute("OpenDocument", "default", "{controller}/{action}", new[] { "Satrabel.OpenDocument.Components", "Satrabel.OpenDocument.Components.JPList" });
        }
    }
} 

