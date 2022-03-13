using DNNrocketAPI;
using DNNrocketAPI.Components;
using Simplisity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace RocketDocs.Components
{

    public class ArticleLimpetList
    {
        private string _langRequired;
        private List<ArticleLimpet> _articleList;
        public const string _tableName = "RocketDocs";
        private const string _entityTypeCode = "ART";
        private DNNrocketController _objCtrl;
        private string _searchFilter;
        private int _catid;

        public ArticleLimpetList(int categoryId, PortalContentLimpet portalCatalog, string langRequired, bool populate)
        {
            PortalCatalog = portalCatalog;

            _langRequired = langRequired;
            if (_langRequired == "") _langRequired = DNNrocketUtils.GetCurrentCulture();
            _objCtrl = new DNNrocketController();

            var paramInfo = new SimplisityInfo();
            SessionParamData = new SessionParams(paramInfo);
            SessionParamData.PageSize = 0;

            _catid = categoryId;

            if (populate) Populate();
        }
        public ArticleLimpetList(SimplisityInfo paramInfo, PortalContentLimpet portalCatalog, string langRequired, bool populate, bool showHidden = true)
        {
            PortalCatalog = portalCatalog;

            _langRequired = langRequired;
            if (_langRequired == "") _langRequired = DNNrocketUtils.GetCurrentCulture();
            _objCtrl = new DNNrocketController();

            SessionParamData = new SessionParams(paramInfo);
            if (paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/pagesize") != 0) SessionParamData.PageSize = paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/pagesize");
            if (paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/ps") != 0) SessionParamData.PageSize = paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/ps");
            if (paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/page") != 0) SessionParamData.Page = paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/page");
            if (paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/p") != 0) SessionParamData.Page = paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/p");
            if (SessionParamData.PageSize == 0) SessionParamData.PageSize = 32;

            _catid = paramInfo.GetXmlPropertyInt("genxml/hidden/catid");
            if (_catid == 0) _catid = paramInfo.GetXmlPropertyInt("genxml/remote/urlparams/catid");

            if (SessionParamData.OrderByRef == "" && _catid == 0) SessionParamData.OrderByRef = "sqlorderby-article-ref";

            CatalogSettings = new CatalogSettingsLimpet(portalCatalog.PortalId, SessionParamData.CultureCode);

            if (_catid == 0 && !showHidden) _catid = CatalogSettings.DefaultCategoryId; // showHidden is admin.

            if (populate) Populate(showHidden);
        }
        public void Populate(bool showHidden = true)
        {
            _searchFilter += PortalCatalog.GetFilterProductSQL(SessionParamData.Info);
            if (_catid > 0) _searchFilter += " and [CATXREF].[XrefItemId] = " + _catid + " ";

            var orderby = PortalCatalog.OrderByProductSQL(SessionParamData.OrderByRef);
            if (SessionParamData.OrderByRef == "" && _catid > 0) orderby = " order by [CATXREF].[SortOrder] "; // use manual sort for articles by category;

            //Filter Property
            var checkboxfilter = "";
            RemoteModule remoteModule = null;
            var nodList = SessionParamData.Info.XMLDoc.SelectNodes("r/*[starts-with(name(), 'checkboxfilter')]");
            if (nodList != null && nodList.Count > 0) remoteModule = new RemoteModule(PortalCatalog.PortalId, SessionParamData.ModuleRef);
            foreach (XmlNode nod in nodList)
            {
                if (nod.InnerText.ToLower() == "true")
                {
                    var propid = nod.Name.Replace("checkboxfilter", "");
                    // NOTE: checkbox for filter must be called "checkboxfilterand"
                    if (remoteModule.Record.GetXmlPropertyBool("genxml/checkbox/checkboxfilterand"))
                    {
                        if (checkboxfilter != "") checkboxfilter += " and ";
                        checkboxfilter += " [PROPXREF].[XrefItemId] = " + propid + " ";
                    }
                    else
                    {
                        if (checkboxfilter != "") checkboxfilter += " or ";
                        checkboxfilter += " [PROPXREF].[XrefItemId] = " + propid + " ";
                    }
                }
            }
            if (checkboxfilter != "") _searchFilter += " and ( " + checkboxfilter + " ) ";

            // Filter hidden
            if (!showHidden) _searchFilter += " and NOT(isnull([XMLData].value('(genxml/checkbox/hidden)[1]','nvarchar(4)'),'false') = 'true') and NOT(isnull([XMLData].value('(genxml/lang/genxml/checkbox/hidden)[1]','nvarchar(4)'),'false') = 'true') ";

            SessionParamData.RowCount = _objCtrl.GetListCount(PortalCatalog.PortalId, -1, _entityTypeCode, _searchFilter, _langRequired, _tableName);
            RecordCount = SessionParamData.RowCount;

            DataList = _objCtrl.GetList(PortalCatalog.PortalId, -1, _entityTypeCode, _searchFilter, _langRequired, orderby, 0, SessionParamData.Page, SessionParamData.PageSize, SessionParamData.RowCount, _tableName);
        }
        public void DeleteAll()
        {
            var l = GetAllArticlesForShopPortal();
            foreach (var r in l)
            {
                _objCtrl.Delete(r.ItemID);
            }
        }

        public SessionParams SessionParamData { get; set; }
        public List<SimplisityInfo> DataList { get; private set; }
        public PortalContentLimpet PortalCatalog { get; set; }
        public CatalogSettingsLimpet CatalogSettings { get; private set; }
        
        public int RecordCount { get; set; }
        public int CategoryId { get { return _catid; } }        
        public List<ArticleLimpet> GetArticleList()
        {
            _articleList = new List<ArticleLimpet>();
            foreach (var o in DataList)
            {
                var articleData = new ArticleLimpet(PortalCatalog.PortalId, o.ItemID, _langRequired);
                _articleList.Add(articleData);
            }
            return _articleList;
        }
        public void SortOrderMove(int toItemId)
        {
            SortOrderMove(SessionParamData.SortActivate, toItemId);
        }
        public void SortOrderMove(int fromItemId, int toItemId)
        {
            if (fromItemId > 0 && toItemId > 0)
            {
                var moveData = new ArticleLimpet(PortalCatalog.PortalId, fromItemId, _langRequired);
                var toData = new ArticleLimpet(PortalCatalog.PortalId, toItemId, _langRequired);

                var newSortOrder = toData.SortOrder - 1;
                if (moveData.SortOrder < toData.SortOrder) newSortOrder = toData.SortOrder + 1;

                moveData.SortOrder = newSortOrder;
                moveData.Update();
                SessionParamData.CancelItemSort();
            }
        }

        public List<SimplisityInfo> GetAllArticlesForShopPortal()
        {
            return _objCtrl.GetList(PortalCatalog.PortalId, -1, _entityTypeCode, "", _langRequired, "", 0, 0, 0, 0, _tableName);
        }

        public void Validate()
        {
            var list = GetAllArticlesForShopPortal();
            foreach (var pInfo in list)
            {
                var articleData = new ArticleLimpet(PortalCatalog.PortalId, pInfo.ItemID, _langRequired);
                articleData.ValidateAndUpdate();
            }
        }
        public string PagingUrl(int page)
        {
            var categoryData = new CategoryLimpet(PortalCatalog.PortalId, CategoryId, _langRequired);
            var url = SessionParamData.PageUrl.TrimEnd('/') + PortalCatalog.ArticlePagingUrl;
            url = url.Replace("{page}", page.ToString());
            url = url.Replace("{pagesize}", SessionParamData.PageSize.ToString());
            url = url.Replace("{catid}", categoryData.CategoryId.ToString());
            url = url.Replace("{categoryname}", categoryData.Name);
            url = LocalUtils.TokenReplacementCultureCode(url, _langRequired.ToLower());
            return url;
        }
        public string ListUrl()
        {
            return SessionParamData.PageUrl;
        }
    }

}
