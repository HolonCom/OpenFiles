/*
' Copyright (c) 2015-2016 Satrabel.be
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Modules.Definitions;
using DotNetNuke.Entities.Tabs;

namespace Satrabel.OpenFiles.Components
{
    public class FeatureController : IUpgradeable //: ModuleSearchBase //,IPortable 
    {
        #region Optional Interfaces

        /*
        public string ExportModule(int ModuleID)
        {
            string xml = "";
            OpenContentController ctrl = new OpenContentController();
            var content = ctrl.GetFirstContent(ModuleID);
            if ((content != null))
            {
                xml += "<opencontent>";
                xml += "<json>" + XmlUtils.XMLEncode(content.Json) + "</json>";
                xml += "</opencontent>";
            }
            return xml;
        }
        public void ImportModule(int ModuleID, string Content, string Version, int UserID)
        {
            OpenContentController ctrl = new OpenContentController();
            XmlNode xml = Globals.GetContent(Content, "opencontent");
            var content = new OpenContentInfo()
            {
                ModuleId = ModuleID,
                Json = xml.SelectSingleNode("json").InnerText,
                CreatedByUserId = UserID,
                CreatedOnDate = DateTime.Now,
                LastModifiedByUserId = UserID,
                LastModifiedOnDate = DateTime.Now,
                Html = ""
            };
            ctrl.AddContent(content);
        }
         */
        #region ModuleSearchBase
        /*
        public override IList<SearchDocument> GetModifiedSearchDocuments(ModuleInfo modInfo, DateTime beginDateUtc)
        {
            var searchDocuments = new List<SearchDocument>();
            OpenContentController ctrl = new OpenContentController();
            var content = ctrl.GetFirstContent(modInfo.ModuleID);
            if (content != null &&
                (content.LastModifiedOnDate.ToUniversalTime() > beginDateUtc &&
                 content.LastModifiedOnDate.ToUniversalTime() < DateTime.UtcNow))
            {
                var searchDoc = new SearchDocument
                {
                    UniqueKey = modInfo.ModuleID.ToString(),
                    PortalId = modInfo.PortalID,
                    Title = modInfo.ModuleTitle,
                    Description = content.Title,
                    Body = content.Json,
                    ModifiedTimeUtc = content.LastModifiedOnDate.ToUniversalTime()
                };
                searchDocuments.Add(searchDoc);
            }
            return searchDocuments;
        }
         */
        #endregion

        #endregion

        /// -----------------------------------------------------------------------------
        /// <summary>
        /// UpgradeModule implements the IUpgradeable Interface
        /// </summary>
        /// <param name="version">The current version of the module</param>
        /// -----------------------------------------------------------------------------
        public string UpgradeModule(string version)
        {
            string res = "";
            if (version == "03.02.00")
            {
                Lucene.LuceneController.Instance.IndexAll();
            }
            return version + res;
        }

        internal static void GenerateAdminTab(int portalId)
        {
            GenerateAdminTab("OpenFiles", portalId);
        }

        internal static bool AdminTabExists( int portalId)
        {
            return AdminTabExists("OpenFiles", portalId);
        }

        private static bool AdminTabExists(string friendlyModuleName, int portalId)
        {
            var tabId = TabController.GetTabByTabPath(portalId, $"//Admin//{friendlyModuleName}", Null.NullString);
            return (tabId != Null.NullInteger);
        }

        private static void GenerateAdminTab(string friendlyModuleName, int portalId)
        {
            var tabId = TabController.GetTabByTabPath(portalId, $"//Admin//{friendlyModuleName}", Null.NullString);
            if (tabId == Null.NullInteger)
            {
                var adminTabId = TabController.GetTabByTabPath(portalId, @"//Admin", Null.NullString);

                // create new page 
                int parentTabId = adminTabId;
                var tabName = friendlyModuleName;
                var tabPath = Globals.GenerateTabPath(parentTabId, tabName);
                tabId = TabController.GetTabByTabPath(portalId, tabPath, Null.NullString);
                if (tabId == Null.NullInteger)
                {
                    //Create a new page
                    var newTab = new TabInfo
                    {
                        TabName = tabName,
                        ParentId = parentTabId,
                        PortalID = portalId,
                        IsVisible = true,
                        IconFile = "~/Images/icon_search_16px.gif",
                        IconFileLarge = "~/Images/icon_search_32px.gif"
                    };
                    newTab.TabID = new TabController().AddTab(newTab, false);
                    tabId = newTab.TabID;
                }
            }

            // create new module
            var moduleCtl = new ModuleController();
            if (moduleCtl.GetTabModules(tabId).Count == 0)
            {
                var dm = DesktopModuleController.GetDesktopModuleByModuleName(friendlyModuleName, portalId);
                var md = ModuleDefinitionController.GetModuleDefinitionByFriendlyName(friendlyModuleName, dm.DesktopModuleID);

                var objModule = new ModuleInfo
                {
                    PortalID = portalId,
                    TabID = tabId,
                    ModuleOrder = Null.NullInteger,
                    ModuleTitle = friendlyModuleName,
                    PaneName = Globals.glbDefaultPane,
                    ModuleDefID = md.ModuleDefID,
                    InheritViewPermissions = true,
                    AllTabs = false,
                    IconFile = "~/Images/icon_search_32px.gif"
                };
                moduleCtl.AddModule(objModule);
            }
        }

    }
}