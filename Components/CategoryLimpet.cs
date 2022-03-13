using DNNrocketAPI;
using Simplisity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DNNrocketAPI.Components;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Web;

namespace RocketDocs.Components
{
    public class CategoryLimpet
    {
        public const string _tableName = "RocketDocs";
        public const string _entityTypeCode = "CAT";

        private DNNrocketController _objCtrl;
        /// <summary>
        /// Should be used to create an article, the portalId is required on creation
        /// </summary>
        /// <param name="portalId"></param>
        /// <param name="articleId"></param>
        /// <param name="langRequired"></param>
        public CategoryLimpet(int portalId, int categoryId, string langRequired, bool populate = true)
        {
            if (categoryId <= 0) categoryId = -1;  // create new record.
            PortalId = portalId;
            EntityTypeCode = _entityTypeCode;
            TableName = _tableName;
            CultureCode = langRequired;

            Info = new SimplisityInfo();
            Info.ItemID = categoryId;
            Info.TypeCode = EntityTypeCode;
            Info.ModuleId = -1;
            Info.UserId = -1;
            Info.PortalId = PortalId;
            Info.Lang = CultureCode;

            Populate();
        }
        private void Populate()
        {
            _objCtrl = new DNNrocketController();

            var info = _objCtrl.GetData(EntityTypeCode, CategoryId, CultureCode, ModuleId, TableName); // get existing record.
            if (info != null) // check if we have a real record.
                Info = info;
            else
                Info.ItemID = -1; // flags categories does not exist yet.

            Info.Lang = CultureCode; // reset langauge, for legacy record, without lang.
            PortalId = Info.PortalId;
            PortalCatalog = new PortalContentLimpet(PortalId, CultureCode);
        }
        public void Delete()
        {
            if (Info.ItemID > 0)
            {
                // remove category xref
                var catxrefList = _objCtrl.GetList(PortalId, -1, "CATXREF", " and [XrefItemId] = " + CategoryId + " ", "", "", 0, 0, 0, 0, TableName);
                foreach (var catxrefRecord in catxrefList)
                {
                    _objCtrl.Delete(catxrefRecord.ItemID, TableName);
                }
                _objCtrl.Delete(Info.ItemID, TableName);
                CacheUtils.ClearAllCache();
            }
        }
        private void ReplaceInfoFields(SimplisityInfo postInfo, string xpathListSelect)
        {
            var textList = Info.XMLDoc.SelectNodes(xpathListSelect);
            if (textList != null)
            {
                foreach (XmlNode nod in textList)
                {
                    Info.RemoveXmlNode(xpathListSelect.Replace("*", "") + nod.Name);
                }
            }
            textList = postInfo.XMLDoc.SelectNodes(xpathListSelect);
            if (textList != null)
            {
                foreach (XmlNode nod in textList)
                {
                    Info.SetXmlProperty(xpathListSelect.Replace("*", "") + nod.Name, nod.InnerText);
                }
            }
        }
        public int Save(SimplisityInfo postInfo)
        {
            ReplaceInfoFields(postInfo, "genxml/textbox/*");
            ReplaceInfoFields(postInfo, "genxml/lang/genxml/textbox/*");
            ReplaceInfoFields(postInfo, "genxml/checkbox/*");
            ReplaceInfoFields(postInfo, "genxml/lang/genxml/checkbox/*");
            ReplaceInfoFields(postInfo, "genxml/select/*");
            ReplaceInfoFields(postInfo, "genxml/radio/*");

            Info.RemoveList(ImageListName);
            foreach (var listItem in postInfo.GetList(ImageListName))
            {
                Info.AddListItem(ImageListName, listItem);
            }

            // sort only on category save.
            var lp = 5;
            var articleList = postInfo.GetRecordList(ArticleListName);
            foreach (var articleRec in articleList)
            {
                var categoryguidkey = articleRec.GetXmlProperty("genxml/hidden/categoryguidkey");
                var articleId = articleRec.GetXmlPropertyInt("genxml/hidden/articleid");
                var articleData = new ArticleLimpet(articleId, CultureCode);
                articleData.UpdateCategorySortOrder(categoryguidkey, lp);
                lp += 5;
            }

            return ValidateAndUpdate();
        }
        public List<CategoryLimpet> GetChildren()
        {
            var categoryDataList = new CategoryLimpetList(PortalId, CultureCode);
            var rtnList = new List<CategoryLimpet>();
            rtnList = categoryDataList.GetCategoryRecursive(CategoryId, rtnList, Level);
            return rtnList;
        }
        public bool IsChild(int categoryid)
        {
            var l = GetChildren();
            foreach (var c in l)
            {
                if (c.CategoryId == categoryid) return true;
            }
            return false;
        }
        public ArticleLimpetList GetArticles()
        {
            var portalCatalog = new PortalContentLimpet(PortalId, CultureCode);
            return new ArticleLimpetList(CategoryId, portalCatalog, CultureCode, true);
        }
        public int Update()
        {
            Info = _objCtrl.SaveData(Info, TableName);
            if (Info.GUIDKey == "")
            {
                // get unique ref
                Info.GUIDKey = GeneralUtils.GetGuidKey();
                Info = _objCtrl.SaveData(Info, TableName);
            }
            return Info.ItemID;
        }
        public int ValidateAndUpdate()
        {
            Validate();
            return Update();
        }
        public void Validate()
        {
        }
        public string UrlTokens(string url)
        {
            url = url.Replace("{catid}", CategoryId.ToString());
            url = url.Replace("{categoryname}", GeneralUtils.UrlFriendly(Name));
            url = LocalUtils.TokenReplacementCultureCode(url, CultureCode.ToLower());
            return url;
        }
        public string UrlTokens(string url, int page, int pagesize)
        {
            url = url.Replace("{catid}", CategoryId.ToString());
            url = url.Replace("{page}", page.ToString());
            url = url.Replace("{pagesize}", pagesize.ToString());
            url = url.Replace("{categoryname}", GeneralUtils.UrlFriendly(Name));
            url = LocalUtils.TokenReplacementCultureCode(url, CultureCode.ToLower());
            return url;
        }
        public string CategoryUrl(SessionParams sessionParamData)
        {
            if (CategoryId == 0) return sessionParamData.PageUrl;

            var url = sessionParamData.PageUrl + PortalCatalog.ArticleListPageUrl;
            url = url.Replace("{page}", sessionParamData.Page.ToString());
            url = url.Replace("{pagesize}", sessionParamData.PageSize.ToString());
            url = url.Replace("{catid}", CategoryId.ToString());
            url = url.Replace("{categoryname}", GeneralUtils.UrlFriendly(Name));
            url = LocalUtils.TokenReplacementCultureCode(url, CultureCode.ToLower());
            return url;
        }

        #region "images"
        public List<SimplisityInfo> GetImageList()
        {
            return Info.GetList(ImageListName);
        }
        public void AddImage(string imageFolderRel, string uniqueName)
        {
            if (GetImageList().Count < 10)
            {
                if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
                var articleImage = new ArticleImage(new SimplisityInfo(), "articleimage");
                articleImage.RelPath = imageFolderRel.TrimEnd('/') + "/" + uniqueName;
                Info.AddListItem(ImageListName, articleImage.Info);
                Update();
            }
        }
        public ArticleImage GetImage(int idx)
        {
            return new ArticleImage(Info.GetListItem(ImageListName, idx), "articleimage");
        }
        public List<ArticleImage> GetImages()
        {
            var rtn = new List<ArticleImage>();
            foreach (var i in Info.GetList(ImageListName))
            {
                rtn.Add(new ArticleImage(i, "articleimage"));
            }
            return rtn;
        }
        public void RemoveImageList()
        {
            Info.RemoveList(ImageListName);
        }
        #endregion


        #region "properties"

        public string CultureCode { get; set; }
        public string EntityTypeCode { get; set; }
        public SimplisityInfo Info { get; set; }
        public int ModuleId { get { return Info.ModuleId; } set { Info.ModuleId = value; } }
        public int XrefItemId { get { return Info.XrefItemId; } set { Info.XrefItemId = value; } }
        public int ParentItemId { get { return Info.ParentItemId; } set { Info.ParentItemId = value; } }
        public int CategoryId { get { return Info.ItemID; } set { Info.ItemID = value; } }
        public string GUIDKey { get { return Info.GUIDKey; } set { Info.GUIDKey = value; } }
        public int SortOrder { get { return Info.SortOrder; } set { Info.SortOrder = value; } }
        public string ImageFolder { get; set; }
        public int PortalId { get; set; }
        public bool Exists { get { if (Info.ItemID <= 0) { return false; } else { return true; }; } }
        public string LogoRelPath
        {
            get
            {
                var articleImage = GetImage(0);
                return articleImage.RelPath;
            }
        }
        public string TableName { get; set; }
        public int Level { get { return Info.GetXmlPropertyInt("genxml/textbox/level"); } set { Info.SetXmlProperty("genxml/textbox/level", value.ToString()); } }
        public string ImageListName { get { return "imagecategorylist"; } }
        public string ArticleListName { get { return "articlelist"; } }
        public PortalContentLimpet PortalCatalog { get; set; }
        public string Ref { get { return Info.GetXmlProperty(RefXPath); } }
        public string RefXPath { get { return "genxml/textbox/ref"; } }
        public string RichText { get { return Info.GetXmlProperty(RichTextXPath); } }
        public string RichTextXPath { get { return "genxml/lang/genxml/textbox/categoryrichtext"; } }
        public string Name { get { return Info.GetXmlProperty(NameXPath); } }
        public string NameXPath { get { return "genxml/lang/genxml/textbox/name"; } }
        public string Summary { get { return Info.GetXmlProperty(SummaryXPath); } }
        public string SummaryXPath { get { return "genxml/lang/genxml/textbox/summary"; } }
        public string Keywords { get { return Info.GetXmlProperty(KeywordsXPath); } }
        public string KeywordsXPath { get { return "genxml/lang/genxml/textbox/categorykeywords"; } }
        public bool Hidden { get { return Info.GetXmlPropertyBool("genxml/checkbox/hidden"); } }
        public bool HiddenByCulture { get { return Info.GetXmlPropertyBool("genxml/lang/genxml/checkbox/hidden"); } }
        public bool IsHidden { get { if (Hidden || HiddenByCulture) return true; else return false; } }
        #endregion

    }

}
