using DNNrocketAPI.Components;
using RocketDocs.Components;
using Simplisity;
using System;
using System.Collections.Generic;
using System.Text;

namespace RocketDocs.API
{
    public partial class StartConnect : DNNrocketAPI.APInterface
    {
        private SimplisityInfo _postInfo;
        private SimplisityInfo _paramInfo;
        private RocketInterface _rocketInterface;
        private SystemLimpet _systemData;
        private Dictionary<string, string> _passSettings;
        private SessionParams _sessionParams;
        private UserParams _userParams;
        private AppThemeSystemLimpet _appThemeSystem;
        private PortalContentLimpet _portalContent;

        public override Dictionary<string, object> ProcessCommand(string paramCmd, SimplisityInfo systemInfo, SimplisityInfo interfaceInfo, SimplisityInfo postInfo, SimplisityInfo paramInfo, string langRequired = "")
        {
            var strOut = ""; // return nothing if not matching commands.
            var storeParamCmd = paramCmd;

            paramCmd = InitCmd(paramCmd, systemInfo, interfaceInfo, postInfo, paramInfo, langRequired);

            var rtnDic = new Dictionary<string, object>();

            switch (paramCmd)
            {
                case "rocketsystem_edit":
                    strOut = RocketSystem();
                    break;
                case "rocketsystem_init":
                    strOut = RocketSystemInit();
                    break;
                case "rocketsystem_delete":
                    strOut = RocketSystemDelete();
                    break;
                case "rocketsystem_valid":
                    strOut = RocketSystemValid();
                    break;
                case "rocketsystem_adminpanel":
                    strOut = AdminPanel();
                    break;
                case "rocketsystem_save":
                    strOut = RocketSystemSave();
                    break;
                case "rocketsystem_login":
                    _userParams.TrackClear(_systemData.SystemKey);
                    strOut = ReloadPage();
                    break;



                case "rocketdocs_adminpanel":
                    strOut = AdminPanel();
                    break;
                case "rocketdocs_adminpanelheader":
                    strOut = AdminPanelHeader();
                    break;
                case "rocketdocs_admin":
                    strOut = "rocketdocs_admin";
                    break;




                case "invalidcommand":
                    strOut = "INVALID COMMAND: " + storeParamCmd;
                    break;

            }

            rtnDic.Add("outputhtml", strOut);
            return rtnDic;

        }

        private string RocketSystemSave()
        {
            var portalId = _paramInfo.GetXmlPropertyInt("genxml/hidden/portalid"); // we may have passed selection
            if (portalId >= 0)
            {
                var portalContent = new PortalContentLimpet(portalId, _sessionParams.CultureCodeEdit);
                if (portalContent.PortalId >= 0) portalContent.Save(_postInfo);
                _portalContent = new PortalContentLimpet(portalId, _sessionParams.CultureCodeEdit); // reload portal data after save (for langauge change)
                var razorTempl = _appThemeSystem.GetTemplate("RocketSystem.cshtml");
                var pr = RenderRazorUtils.RazorProcessData(razorTempl, _portalContent, null, _passSettings, _sessionParams, true);
                if (pr.StatusCode != "00") return pr.ErrorMsg;
                return pr.RenderedText;
            }
            return "Invalid PortalId";
        }
        private String RocketSystem()
            {
                try
                {
                    _portalContent = new PortalContentLimpet(_portalContent.PortalId, _sessionParams.CultureCodeEdit);
                    var razorTempl = _appThemeSystem.GetTemplate("RocketSystem.cshtml");
                    var pr = RenderRazorUtils.RazorProcessData(razorTempl, _portalContent, null, _passSettings, _sessionParams, true);
                    if (pr.StatusCode != "00") return pr.ErrorMsg;
                    return pr.RenderedText;
                }
            catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
            private String RocketSystemInit()
            {
                try
                {
                    var newportalId = _paramInfo.GetXmlPropertyInt("genxml/hidden/newportalid");
                    if (newportalId > 0)
                    {
                        _portalContent = new PortalContentLimpet(newportalId, _sessionParams.CultureCodeEdit);
                        _portalContent.Validate();
                        _portalContent.Update();
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
            private String RocketSystemDelete()
            {
                try
                {
                    var portalId = _paramInfo.GetXmlPropertyInt("genxml/hidden/portalid");
                    if (portalId > 0)
                    {
                        _portalContent = new PortalContentLimpet(portalId, _sessionParams.CultureCodeEdit);
                        _portalContent.Delete();
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
            private String RocketSystemValid()
            {
                try
                {
                    var portalId = _paramInfo.GetXmlPropertyInt("genxml/hidden/portalid");
                    if (portalId > 0)
                    {
                        _portalContent = new PortalContentLimpet(portalId, _sessionParams.CultureCodeEdit);
                        if (!_portalContent.Valid)
                        {
                            var razorTempl = _appThemeSystem.GetTemplate("InvalidSystem.cshtml");
                            var pr = RenderRazorUtils.RazorProcessData(razorTempl, _portalContent, null, _passSettings, _sessionParams, true);
                            if (pr.StatusCode != "00") return pr.ErrorMsg;
                            return pr.RenderedText;
                        }
                    }
                    return "";
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }
            }
        private string AdminPanel()
        {
            try
            {
                var razorTempl = _appThemeSystem.GetTemplate("AdminPanel.cshtml");
                var pr = RenderRazorUtils.RazorProcessData(razorTempl, _portalContent, null, _passSettings, _sessionParams, true);
                if (pr.StatusCode != "00") return pr.ErrorMsg;
                return pr.RenderedText;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string AdminPanelHeader()
        {
            try
            {
                var razorTempl = _appThemeSystem.GetTemplate("AdminPanelHeader.cshtml");
                var pr = RenderRazorUtils.RazorProcessData(razorTempl, _portalContent, null, _passSettings, _sessionParams, true);
                if (pr.StatusCode != "00") return pr.ErrorMsg;
                return pr.RenderedText;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        private string ReloadPage()
        {
            try
            {
                // user does not have access, logoff
                UserUtils.SignOut();

                var portalAppThemeSystem = new AppThemeDNNrocketLimpet("rocketportal");
                var razorTempl = portalAppThemeSystem.GetTemplate("Reload.cshtml");
                var pr = RenderRazorUtils.RazorProcessData(razorTempl, _portalContent, null, _passSettings, _sessionParams, true);
                if (pr.StatusCode != "00") return pr.ErrorMsg;
                return pr.RenderedText;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }
        public string InitCmd(string paramCmd, SimplisityInfo systemInfo, SimplisityInfo interfaceInfo, SimplisityInfo postInfo, SimplisityInfo paramInfo, string langRequired = "")
        {
            _postInfo = postInfo;
            _paramInfo = paramInfo;
            _systemData = new SystemLimpet(systemInfo.GetXmlProperty("genxml/systemkey"));
            _appThemeSystem = new AppThemeSystemLimpet(_systemData.SystemKey);
            _rocketInterface = new RocketInterface(interfaceInfo);
            _sessionParams = new SessionParams(_paramInfo);
            _userParams = new UserParams(_sessionParams.BrowserSessionId);
            _passSettings = new Dictionary<string, string>();

            // Assign Langauge
            DNNrocketUtils.SetCurrentCulture();
            if (_sessionParams.CultureCode == "") _sessionParams.CultureCode = DNNrocketUtils.GetCurrentCulture();
            if (_sessionParams.CultureCodeEdit == "") _sessionParams.CultureCodeEdit = DNNrocketUtils.GetEditCulture();
            DNNrocketUtils.SetCurrentCulture(_sessionParams.CultureCode);
            DNNrocketUtils.SetEditCulture(_sessionParams.CultureCodeEdit);

            var portalid = _paramInfo.GetXmlPropertyInt("genxml/hidden/portalid");
            if (portalid >= 0 && PortalUtils.GetPortalId() == 0)
                _portalContent = new PortalContentLimpet(portalid, _sessionParams.CultureCodeEdit); // Portal 0 is admin, editing portal setup
            else
            {
                _portalContent = new PortalContentLimpet(PortalUtils.GetPortalId(), _sessionParams.CultureCodeEdit);
                if (!_portalContent.Active) return "";
            }
            // [TODO]: Users should only have access to their own services for setup on portal 0.
            // [TODO]: Private admin needs to allow access for managers.
            // [TODO]: Public facing API should allow access for all users.

            var securityData = new SecurityLimpet(_portalContent.PortalId, _systemData.SystemKey, _rocketInterface, -1, -1);
            paramCmd = securityData.HasSecurityAccess(paramCmd, "rocketsystem_login");

            return paramCmd;
        }
    }

}
