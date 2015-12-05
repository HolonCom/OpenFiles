using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DotNetNuke.Instrumentation;

namespace Satrabel.OpenFiles.Components
{
    public static class Utils
    {
        public static ILog Logger
        {
            get
            {
                return LoggerSource.Instance.GetLogger("OpenFiles");
            }
        }
    }
}