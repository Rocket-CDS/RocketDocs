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
using Newtonsoft.Json;
using System.IO;

namespace RocketDocs.Components
{
    public class ArticleLimpet
    {
        public const string _tableName = "RocketDocs";
        public const string _entityTypeCode = "ART";
        private DNNrocketController _objCtrl;
        private int _articleId;
        private List<SimplisityInfo> _propXrefList;
        private List<int> _propXrefListId;
        private List<string> _propXrefListRef;
        private List<SimplisityInfo> _catXrefList;
        private List<int> _catXrefListId;
        private List<string> _catXrefListRef;

        public ArticleLimpet()
        {
            Info = new SimplisityInfo();
        }
        /// <summary>
        /// Read an existing article, if it does not exist the "Exists" property will be false. 
        /// </summary>
        /// <param name="articleId"></param>
        /// <param name="langRequired"></param>
        public ArticleLimpet(int articleId, string langRequired)
        {
            Info = new SimplisityInfo();
            _articleId = articleId;
            Populate(langRequired);
        }
        /// <summary>
        /// Should be used to create an article, the portalId is required on creation
        /// </summary>
        /// <param name="portalId"></param>
        /// <param name="articleId"></param>
        /// <param name="langRequired"></param>
        public ArticleLimpet(int portalId, int articleId, string langRequired)
        {
            if (articleId <= 0) articleId = -1;  // create new record.
            _articleId = articleId;
            PortalId = portalId;
            Info = new SimplisityInfo();
            Info.ItemID = articleId;
            Info.TypeCode = _entityTypeCode;
            Info.ModuleId = -1;
            Info.UserId = -1;
            Info.PortalId = PortalId;

            Populate(langRequired);
        }
        /// <summary>
        /// When we populate with a child article row.
        /// </summary>
        /// <param name="articleData"></param>
        public ArticleLimpet(ArticleLimpet articleData)
        {
            Info = articleData.Info;
            _articleId = articleData.ArticleId;
            CultureCode = articleData.CultureCode;
            PortalId = Info.PortalId;
            PortalCatalog = new PortalContentLimpet(PortalId, CultureCode);
        }
        private void Populate(string cultureCode)
        {
            _objCtrl = new DNNrocketController();
            CultureCode = cultureCode;

            var info = _objCtrl.GetData(PortalId, _entityTypeCode, _articleId, CultureCode, ModuleId, _tableName); // get existing record.
            if (info != null && info.ItemID > 0) Info = info; // check if we have a real record, or a dummy being created and not saved yet.
            Info.Lang = CultureCode;
            PortalId = Info.PortalId;
            PortalCatalog = new PortalContentLimpet(PortalId, CultureCode);
        }
        public void PopulateLists()
        {
            _propXrefList = _objCtrl.GetList(PortalId, -1, "PROPXREF", " and R1.[ParentItemId] = " + ArticleId + " ", "", "", 0, 0, 0, 0, _tableName);
            _propXrefListId = new List<int>();
            _propXrefListRef = new List<string>();
            foreach (var p in _propXrefList)
            {
                var c = new PropertyLimpet(PortalId, p.XrefItemId, CultureCode);
                _propXrefListId.Add(p.XrefItemId);
                _propXrefListRef.Add(c.Ref);
            }
            _catXrefList = _objCtrl.GetList(PortalId, -1, "CATXREF", " and R1.[ParentItemId] = " + ArticleId + " ", "", "", 0, 0, 0, 0, _tableName);
            _catXrefListId = new List<int>();
            _catXrefListRef = new List<string>();
            foreach (var p in _catXrefList)
            {
                var c = new CategoryLimpet(PortalId, p.XrefItemId, CultureCode);
                _catXrefListId.Add(p.XrefItemId);
                _catXrefListRef.Add(c.Ref);
            }
        }
        public void Delete()
        {
            _objCtrl.Delete(Info.ItemID, _tableName);
        }

        private void ReplaceInfoFields(SimplisityInfo postInfo, string xpathListSelect)
        {
            var textList = Info.XMLDoc.SelectNodes(xpathListSelect);
            if (textList != null)
            {
                foreach (XmlNode nod in textList)
                {
                    Info.RemoveXmlNode(xpathListSelect.Replace("*","") + nod.Name);
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
            ReplaceInfoFields(postInfo, "genxml/lang/genxml/select/*");
            ReplaceInfoFields(postInfo, "genxml/radio/*");
            ReplaceInfoFields(postInfo, "genxml/lang/genxml/radio/*");

            var postLists = postInfo.GetLists();
            foreach (var listname in postLists)
            {
                if (listname != "imagelist" && listname != "documentlist" && listname != "linklist")
                {
                    var listData = postInfo.GetList(listname);
                    foreach (var listItem in listData)
                    {
                        Info.AddListItem(listname, listItem);
                    }
                    Info.SetXmlProperty("genxml/" + listname + "/@list", "true");
                    Info.SetXmlProperty("genxml/lang/genxml/" + listname + "/@list", "true");
                }
            }

            UpdateImages(postInfo.GetList("imagelist"));
            UpdateDocs(postInfo.GetList("documentlist"));
            UpdateLinks(postInfo.GetList("linklist"));

            return ValidateAndUpdate();
        }
        public int Update()
        {
            Info = _objCtrl.SaveData(Info, _tableName);
            if (Info.GUIDKey == "")
            {
                //var l = Info.GetList(ArticleRowListName);
                //if (l.Count == 0) UpdateArticleRow("<genxml></genxml>"); // Create Default ArticleRow

                // get unique ref
                Info.GUIDKey = GeneralUtils.GetGuidKey();
                Info = _objCtrl.SaveData(Info, _tableName);
            }
            return Info.ItemID;
        }
        public int ValidateAndUpdate()
        {
            Validate();
            return Update();
        }
        public int Copy()
        {
            Info.ItemID = -1;
            Info.GUIDKey = GeneralUtils.GetGuidKey();
            return Update();
        }

        public void AddListItem(string listname)
        {
            if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
            Info.AddListItem(listname);         
            Update();
        }
        public string ArticleDetailUrl(SessionParams sessionParams)
        {
            var url = sessionParams.PageUrl + PortalCatalog.ArticleDetailPageUrl;
            url = url.Replace("{page}", sessionParams.Page.ToString());
            url = url.Replace("{pagesize}", sessionParams.PageSize.ToString());
            url = url.Replace("{articleid}", ArticleId.ToString());
            url = url.Replace("{articlename}", GeneralUtils.UrlFriendly(Name));
            url = url.Replace("{catid}", sessionParams.GetInt("catid").ToString()); // use int so we always get a value (i.e. "0")
            url = LocalUtils.TokenReplacementCultureCode(url, CultureCode.ToLower());
            return url;
        }

        public void Validate()
        {
        }

        #region "images"

        public string ImageListName { get { return "imagelist";  } }
        public void UpdateImages(List<SimplisityInfo> imageList)
        {
            Info.RemoveList(ImageListName);
            foreach (var sInfo in imageList)
            {
                var imgData = new ArticleImage(sInfo, "articleimage");
                UpdateImage(imgData);
            }
        }
        public List<SimplisityInfo> GetImageList()
        {
            return Info.GetList(ImageListName);
        }
        public ArticleImage AddImage(string uniqueName)
        {
            var articleImage = new ArticleImage(new SimplisityInfo(), "articleimage");
            if (GetImageList().Count < PortalCatalog.ArticleImageLimit)
            {
                if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
                articleImage.RelPath = PortalCatalog.ImageFolderRel.TrimEnd('/') + "/" + uniqueName;
                Info.AddListItem(ImageListName, articleImage.Info);
                Update();
            }
            return articleImage;
        }
        public void UpdateImage(ArticleImage articleImage)
        {
            Info.RemoveListItem(ImageListName, "genxml/hidden/imagekey", articleImage.ImageKey);
            Info.AddListItem(ImageListName, articleImage.Info);
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
        #endregion

        #region "docs"
        public string DocumentListName { get { return "documentlist"; } }
        public void UpdateDocs(List<SimplisityInfo> docList)
        {
            Info.RemoveList(DocumentListName);
            foreach (var sInfo in docList)
            {
                var docData = new ArticleDoc(sInfo, "articledoc");
                UpdateDoc(docData);
            }
        }
        public List<SimplisityInfo> GetDocList()
        {
            return Info.GetList(DocumentListName);
        }
        public ArticleDoc AddDoc(string uniqueName)
        {
            var articleDoc = new ArticleDoc(new SimplisityInfo(), "articledoc");
            if (GetDocList().Count < PortalCatalog.ArticleDocumentLimit)
            {
                if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
                articleDoc.RelPath = PortalCatalog.DocFolderRel.TrimEnd('/') + "/" + uniqueName;
                articleDoc.Name = uniqueName;
                Info.AddListItem(DocumentListName, articleDoc.Info);
                Update();
            }
            return articleDoc;
        }
        public void UpdateDoc(ArticleDoc articleDoc)
        {
            Info.RemoveListItem(DocumentListName, "genxml/hidden/dockey", articleDoc.DocKey);
            Info.AddListItem(DocumentListName, articleDoc.Info);
        }
        public ArticleDoc GetDoc(int idx)
        {
            return new ArticleDoc(Info.GetListItem(DocumentListName, idx), "articledoc");
        }
        public List<ArticleDoc> GetDocs()
        {
            var rtn = new List<ArticleDoc>();
            foreach (var i in Info.GetList(DocumentListName))
            {
                rtn.Add(new ArticleDoc(i, "articledoc"));
            }
            return rtn;
        }
        #endregion

        #region "links"
        public string LinkListName { get { return "linklist"; } }
        public void UpdateLinks(List<SimplisityInfo> linkList)
        {
            Info.RemoveList(LinkListName);
            foreach (var sInfo in linkList)
            {
                var linkData = new ArticleLink(sInfo, "articlelink");
                UpdateLink(linkData);
            }
        }
        public List<SimplisityInfo> GetLinkList()
        {
            return Info.GetList(LinkListName);
        }
        public ArticleLink AddLink()
        {
            var articleLink = new ArticleLink(new SimplisityInfo(), "articlelink");
            if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
            Info.AddListItem(LinkListName, articleLink.Info);
            Update();
            return articleLink;
        }
        public void UpdateLink(ArticleLink articleLink)
        {
            Info.RemoveListItem(LinkListName, "genxml/hidden/linkkey", articleLink.LinkKey);
            Info.AddListItem(LinkListName, articleLink.Info);
        }
        public ArticleLink Getlink(int idx)
        {
            return new ArticleLink(Info.GetListItem(LinkListName, idx), "articlelink");
        }
        public List<ArticleLink> Getlinks()
        {
            var rtn = new List<ArticleLink>();
            foreach (var i in Info.GetList(LinkListName))
            {
                rtn.Add(new ArticleLink(i, "articlelink"));
            }
            return rtn;
        }
        #endregion

        #region "category"
        public void AddCategory(int categoryId, bool cascade = true)
        {
            if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
            var catRecord = _objCtrl.GetRecord(categoryId, _tableName);
            if (catRecord != null)
            {
                var xrefGuidKey = GUIDKey + "-" + catRecord.GUIDKey;
                var catXrefRecord = _objCtrl.GetRecordByGuidKey(PortalId, -1, "CATXREF", xrefGuidKey, "", _tableName);
                if (catXrefRecord == null)
                {
                    var sRec = new SimplisityRecord();
                    sRec.ItemID = -1;
                    sRec.PortalId = PortalId;
                    sRec.ParentItemId = ArticleId;
                    sRec.XrefItemId = categoryId;
                    sRec.TypeCode = "CATXREF";
                    sRec.GUIDKey = xrefGuidKey;
                    _objCtrl.Update(sRec, _tableName);

                    if (cascade) AddCasCadeCategory(categoryId, 0);
                }
            }
        }
        private void AddCasCadeCategory(int categoryId, int levelCount)
        {
            var catRecord = _objCtrl.GetRecord(categoryId, _tableName);
            if (catRecord != null && catRecord.ParentItemId > 0 && levelCount < 50) // use levelCount to stop infinate loop for corrupt data
            {
                if (!IsInCategory(catRecord.ParentItemId))
                {
                    var catParentRecord = _objCtrl.GetRecord(catRecord.ParentItemId, _tableName);
                    var xrefGuidKey = GUIDKey + "-" + catParentRecord.GUIDKey;
                    var casCadeRec = _objCtrl.GetByGuidKey(PortalId, -1, "CATXREF", xrefGuidKey, "", _tableName);
                    if (casCadeRec == null)
                    {
                        var sRec = new SimplisityRecord();
                        sRec.ItemID = -1;
                        sRec.PortalId = PortalId;
                        sRec.ParentItemId = ArticleId;
                        sRec.XrefItemId = catParentRecord.ItemID;
                        sRec.TypeCode = "CATXREF";
                        sRec.GUIDKey = xrefGuidKey;
                        _objCtrl.Update(sRec, _tableName);
                    }
                }
                AddCasCadeCategory(catRecord.ParentItemId, levelCount + 1);
            }
        }

        public void RemoveCategory(int categoryId)
        {

            var filter = " and xrefitemid = " + categoryId + " and ParentItemid = " + ArticleId + " ";
            var l = _objCtrl.GetList(PortalId, -1, "CATXREF", filter, "", "", 0, 0, 0, 0, _tableName);
            foreach (var cx in l)
            {
                //RemoveCasCadeCategory(categoryId);
                _objCtrl.Delete(cx.ItemID, _tableName);
            }
            Update();
        }
        private void RemoveCasCadeCategory(int categoryId)
        {
            var catRecord = _objCtrl.GetRecord(categoryId, _tableName);
            if (catRecord != null && catRecord.ParentItemId > 0)
            {
                var catParentRecord = _objCtrl.GetRecord(catRecord.ParentItemId, _tableName);
                var xrefGuidKey = GUIDKey + "-" + catParentRecord.GUIDKey;
                var casCadeRec = _objCtrl.GetByGuidKey(PortalId, -1, "CATXREF", xrefGuidKey, "", _tableName);
                if (casCadeRec != null)
                {
                    _objCtrl.Delete(casCadeRec.ItemID, _tableName);
                }
                RemoveCasCadeCategory(catRecord.ParentItemId);
            }
        }
        public void UpdateCategorySortOrder(string categoryGUIDKey, int sortOrder)
        {
            var xrefGuidKey = GUIDKey + "-" + categoryGUIDKey;
            var catXrefRecord = _objCtrl.GetRecordByGuidKey(PortalId, -1, "CATXREF", xrefGuidKey, "", _tableName);
            if (catXrefRecord != null)
            {
                catXrefRecord.SortOrder = sortOrder;
                _objCtrl.Update(catXrefRecord, _tableName);
            }
        }
        public List<SimplisityInfo> GetCategoriesInfo()
        {
            if (_catXrefList == null) PopulateLists();
            return _catXrefList;
        }
        public List<CategoryLimpet> GetCategories()
        {
            var rtn = new List<CategoryLimpet>();
            if (_catXrefListId == null) PopulateLists();
            foreach (var categoryId in _catXrefListId)
            {
                var catData = new CategoryLimpet(PortalId, categoryId, CultureCode);
                if (catData.Exists)
                    rtn.Add(catData);
                else
                    RemoveCategory(categoryId);
            }
            return rtn;
        }
        public bool IsInCategory(int categoryId)
        {
            if (_catXrefListId == null) PopulateLists();
            if (_catXrefListId.Contains(categoryId)) return true;
            return false;
        }
        public bool IsInCategory(string propertyRef)
        {
            if (_catXrefListId == null) PopulateLists();
            if (_catXrefListRef.Contains(propertyRef)) return true;
            return false;
        }

        #endregion

        #region "property"
        public void AddProperty(int propertyId)
        {
            if (Info.ItemID < 0) Update(); // blank record, not on DB.  Create now.
            var catRecord = _objCtrl.GetRecord(propertyId, _tableName);
            if (catRecord != null)
            {
                var xrefGuidKey = GUIDKey + "-" + catRecord.GUIDKey;
                var catXrefRecord = _objCtrl.GetRecordByGuidKey(PortalId, -1, "PROPXREF", xrefGuidKey, "", _tableName);
                if (catXrefRecord == null)
                {
                    var sRec = new SimplisityRecord();
                    sRec.ItemID = -1;
                    sRec.PortalId = PortalId;
                    sRec.ParentItemId = ArticleId;
                    sRec.XrefItemId = propertyId;
                    sRec.TypeCode = "PROPXREF";
                    sRec.GUIDKey = xrefGuidKey;
                    _objCtrl.Update(sRec, _tableName);
                }
            }
        }
        public void RemoveProperty(int propertyId)
        {
            var filter = " and xrefitemid = " + propertyId + " and ParentItemid = " + ArticleId + " ";
            var l = _objCtrl.GetList(PortalId, -1, "PROPXREF", filter, "", "", 0, 0, 0, 0, _tableName);
            foreach (var cx in l)
            {
                _objCtrl.Delete(cx.ItemID, _tableName);
            }
            Update();
        }
        public List<SimplisityInfo> GetPropertiesInfo()
        {
            if (_propXrefList == null) PopulateLists();
            return _propXrefList;
        }
        public List<PropertyLimpet> GetProperties()
        {
            if (_propXrefListId == null) PopulateLists();
            var rtn = new List<PropertyLimpet>();
            foreach (var propertyId in _propXrefListId)
            {
                var propertyData = new PropertyLimpet(PortalId, propertyId, CultureCode);
                if (propertyData.Exists)
                    rtn.Add(propertyData);
                else
                    RemoveProperty(propertyId);
            }
            return rtn;
        }
        public bool HasProperty(int propertyId)
        {
            if (_propXrefListId == null) PopulateLists();
            if (_propXrefListId.Contains(propertyId)) return true;
            return false;
        }
        public bool HasProperty(string propertyRef)
        {
            if (_propXrefListId == null) PopulateLists();
            if (_propXrefListRef.Contains(propertyRef)) return true;
            return false;
        }

        #endregion

        #region "properties"

        public string CultureCode { get; private set; }
        public string EntityTypeCode { get { return _entityTypeCode; } }
        public SimplisityInfo Info { get; set; }
        public int ModuleId { get { return Info.ModuleId; } set { Info.ModuleId = value; } }
        public int XrefItemId { get { return Info.XrefItemId; } set { Info.XrefItemId = value; } }
        public int ParentItemId { get { return Info.ParentItemId; } set { Info.ParentItemId = value; } }
        public int ArticleId { get { return Info.ItemID; } set { Info.ItemID = value; } }
        public string GUIDKey { get { return Info.GUIDKey; } set { Info.GUIDKey = value; } }
        public int SortOrder { get { return Info.SortOrder; } set { Info.SortOrder = value; } }
        public string ImageFolder { get; set; }
        public string DocumentFolder { get; set; }
        public string AppTheme { get; set; }
        public string AppThemeVersion { get; set; }
        public string AppThemeRelPath { get; set; }
        public PortalContentLimpet PortalCatalog { get; set; }
        public bool DebugMode { get; set; }
        public int PortalId { get; set; }
        public bool Exists { get {if (Info.ItemID  <= 0) { return false; } else { return true; }; } }
        public string LogoRelPath { get { var articleImage = GetImage(0); return articleImage.RelPath;} }
        public string NameUrl { get { return GeneralUtils.UrlFriendly(Name); } }
        public string Ref { get { return Info.GetXmlProperty(RefXPath); } }
        public string RefXPath { get { return "genxml/textbox/articleref"; } }
        public string RichText { get { return Info.GetXmlProperty(RichTextXPath); } }
        public string RichTextXPath { get { return "genxml/lang/genxml/textbox/articlerichtext"; } }
        public string Name { get { return Info.GetXmlProperty(NameXPath); } set { Info.SetXmlProperty(NameXPath, value); } }
        public string NameXPath { get { return "genxml/lang/genxml/textbox/articlename"; } }
        public string Summary { get { return Info.GetXmlProperty(SummaryXPath); } }
        public string SummaryXPath { get { return "genxml/lang/genxml/textbox/articlesummary"; } }
        public bool Hidden { get { return Info.GetXmlPropertyBool(HiddenXPath); } }
        public string HiddenXPath { get { return "genxml/checkbox/hidden"; } }
        public bool HiddenByCulture { get { return Info.GetXmlPropertyBool(HiddenByCultureXPath); } }
        public string HiddenByCultureXPath { get { return "genxml/lang/genxml/checkbox/hidden"; } }
        public bool IsHidden { get { if (Hidden || HiddenByCulture) return true; else return false; } }
        public string SeoTitle
        {
            get
            {
                if (Info.GetXmlProperty(SeoTitleXPath) == "")
                    return NameUrl;
                else
                    return GeneralUtils.UrlFriendly(Info.GetXmlProperty(SeoTitleXPath));
            }
        }

        public string SeoTitleXPath { get { return "genxml/lang/genxml/textbox/seotitle"; } }
        public string SeoDescription 
        { 
            get 
            { 
                    return Info.GetXmlProperty(SeoDescriptionXPath);
            }
        }
        public string SeoDescriptionXPath { get { return "genxml/lang/genxml/textbox/seodescription"; } }
        public string SeoKeyWords 
        { 
            get 
            { 
                if (Info.GetXmlProperty(SeoKeyWordsXPath) == "")
                    return SeoDescription; 
                else 
                    return Info.GetXmlProperty(SeoKeyWordsXPath); 
            } 
        }
        public string SeoKeyWordsXPath { get { return "genxml/lang/genxml/textbox/seokeyword"; } }

        #endregion

    }

}
