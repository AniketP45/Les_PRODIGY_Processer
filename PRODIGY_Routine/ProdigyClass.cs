using HTTPWrapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static LeSCommon.LeSCommon;
using System.Xml.Linq;
using System.Xml;
using MTML.GENERATOR;
using System.Configuration;
using System.Web;
using HtmlAgilityPack;


namespace PRODIGY_Routine
{
    public class ProdigyClass : LeSCommon.LeSCommon
    {


        public string errormsg = "", mailtxt_path = "", rfqmtml_path = "", processorname = "", mailmsgFilename = "'", sDoneFile = "", processedRFQ = "";
        public bool isRFQ = false, isPO = false;
        public string istoreUrl = "", portalUrl = "", username = "", LoginURL = "", password = "", Ref_Url = "", buyercode = "", buyercode1 = "", suppcode = "", buyersupplinkcode = "", buyername = "", suppname = "", server = "", currDocType = "";
        public string sessionid = "", req = "", vesselid = "", vendorid = "", rfqno = "", quotationid = "", referrer = "", headerdetailhtmldata = "", lineitemhtmldata = "", itemrefferer = "", Headeresponsestr = "", cEncryprtLink = "";
        public string Ref_body_data = "", Currency = "", dataLink = "", htmlNode = "", AuditName = "", AttachmentPath = "", cslUrl = "", addressString = "", uniqueKeyValue = "", ref_Number = "", ref_Name = "", rfqUrl = "";
        public Dictionary<string, string> dctHeaderDetails = new Dictionary<string, string>();
        public Dictionary<string, string> _dctStateData = new Dictionary<string, string>();
        public Dictionary<string, string> _dctItemDescr = new Dictionary<string, string>();
        //public Vlist lineitemcls = new Vlist();
        //public D itemdetails = new D();
        //Quote quote1 = new Quote();
        // public MTMLInterchange _interchange { get; set; }
        //public LineItemCollection _QuoteLineItems = null;
        string linkFileName = "", mailtextname = "", cEmbdPdfAttachPath = "", cEmbdPdfAttachFile = "", QuotePath = "", UCRefNo = "", AAGRefNo = "", HeaderDiscount = "0", LeadDays = "0", ExpDate = "",
          DelvDate = "", MsgRefNumber = "", QuoteCurrency = "", FreightAmt = "0", OtherCosts = "0", TaxCost = "0", DepositCost = "0", GrandTotal = "0", TotalLineItemsAmt = "0",
          SupplierComment = "", PayTerms = "", TermsCondition = "", BuyerTotal = "0", Allowance = "0", PackingCost = "0", cHSCode = "";
        string[] Actions, _arrByrDet, _arrByrLinkCode, _arrByrSppLinkId;
        private int loadUrlRe = 0, Retry = 0;
        public bool IsSubmitQuote = false, IsSendMail = false, IsDecline = false, IsSaveQuote = false, IsUploadAttach = false, IsProcessMailPDF = false;

        public void StartProcess()
        {
            try
            {
                processorname = ConfigurationManager.AppSettings["PROCESSOR_NAME"];
                rfqmtml_path = ConfigurationManager.AppSettings["LeS_MTML_PATH"];
                AuditPath = ConfigurationManager.AppSettings["ESUPPLIER_AUDIT"] + "\\";
                if (!Directory.Exists(rfqmtml_path)) Directory.CreateDirectory(rfqmtml_path);
                processedRFQ = AppDomain.CurrentDomain.BaseDirectory + "DownloadedRFQ.txt";
                AuditName = Convert.ToString(ConfigurationManager.AppSettings["AUDIT_NAME"].Trim());
                AttachmentPath = PrintScreenPath = Convert.ToString(ConfigurationManager.AppSettings["ESUPPLIER_ATTACHMENTS"].Trim());
                //IsSendMail = Convert.ToBoolean(ConfigurationManager.AppSettings["SENDMAIL"].Trim());
                LoadAppsettingsXML();

            }
            catch (Exception ex)
            {
                //WriteLog("Error occurred while initialization " + ex.Message);
                LogText = "Error occurred while initialization" + ex.Message;
            }
        }

        private void LoadAppsettingsXML()
        {
            try
            {
                string appSettingFile = Environment.CurrentDirectory + "\\AppSettings.xml";
                if (File.Exists(appSettingFile))
                {
                    XmlDocument document = new XmlDocument();
                    document.Load(appSettingFile);

                    XmlNodeList xmlAppSettings = document.SelectNodes("//APPSETTINGS");
                    if (xmlAppSettings != null)
                    {
                        foreach (XmlNode appSetting in xmlAppSettings)
                        {
                            dctAppSettings = new Dictionary<string, string>();
                            XmlNodeList childNodes = appSetting.ChildNodes;
                            foreach (XmlNode setting in childNodes)
                            {
                                XmlElement userSetting = (XmlElement)setting;
                                dctAppSettings.Add(userSetting.Name, userSetting.InnerText);
                            }

                            if (dctAppSettings != null && dctAppSettings.Count > 0)
                            {
                                #region GET SETTINGS
                                portalUrl = dctAppSettings["DOMAIN"];
                                istoreUrl = dctAppSettings["DOMAINNAME"];
                                LoginURL = dctAppSettings["LOGINURL"];
                                cslUrl = dctAppSettings["CSLURL"];
                                username = dctAppSettings["USERNAME"];
                                password = dctAppSettings["PASSWORD"];
                                buyercode = dctAppSettings["BUYERCODE"];
                                buyercode1 = dctAppSettings["BUYERLINKCODE"];
                                suppcode = dctAppSettings["SUPPLIERCODE"];
                                //buyersupplinkcode = dctAppSettings["BUYERSUPPLIERLINKCODE"];
                                buyername = dctAppSettings["BUYERNAME"];
                                suppname = dctAppSettings["SUPPLIERNAME"];
                                //server = dctAppSettings["SERVER"];
                                mailtxt_path = dctAppSettings["LINK_INBOX_PATH"];
                                //cEncryprtLink = Convert.ToString(dctAppSettings["ENCYRPTURL"]);
                                QuotePath = Convert.ToString(dctAppSettings["OUTBOX_PATH"].Trim());
                                IsSaveQuote = Convert.ToBoolean(dctAppSettings["SAVE_QUOTE"].Trim());
                                IsSubmitQuote = Convert.ToBoolean(dctAppSettings["SUBMIT_QUOTE"].Trim());
                                string Actions = dctAppSettings["ACTIONS"];
                                #endregion
                                //if (!Directory.Exists(mailtxt_path)) Directory.CreateDirectory(mailtxt_path);

                                LogText = "Processing Started";

                                this.URL = portalUrl;

                                _httpWrapper._AddRequestHeaders.Clear();
                                _httpWrapper._SetRequestHeaders.Clear();
                                _httpWrapper.RequestMethod = "GET";
                                bool loadurl = _httpWrapper.LoadURL(this.URL, "", "", "", "");
                                if (loadurl)
                                {
                                    LogText = "Link loaded";
                                    bool login = Login();
                                    if (login)
                                    {
                                        LogText = "Log In Successful";
                                        try
                                        {
                                            string[] docTypes = Actions.Split(',');
                                            foreach (string docType in docTypes)
                                            {
                                                LogText = "Processing " + docType;
                                                switch (docType.ToLower())
                                                {
                                                    case "rfq":
                                                        currDocType = "RFQ";
                                                        isRFQ = true;
                                                        ProcessRFQ();
                                                        break;
                                                    case "quote":
                                                        currDocType = "QUOTE";
                                                        isRFQ = false;
                                                        //ProcessQuote();
                                                        break;
                                                    default: LogText = "Unknown doctype " + docType; break;
                                                }
                                                LogText = "--------*Process for " + currDocType + " is Done*--------*--------";
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            LogText = ex.Message;
                                            throw ex;
                                        }
                                    }
                                    else
                                    {
                                        LogText = "Login Failed";
                                        //createauditfile(mailmsgfilename, processorname, rfqno, "error", "les:1004 - login failed, unable to process rfq", buyercode, suppcode, auditpath, "", "", "");
                                        CreateAuditFile("", processorname, "", "ERROR", "LeS:1004 - Login Failed, Unable to process RFQ", buyercode, suppcode, AuditPath);

                                    }
                                }
                                else
                                {
                                    errormsg = "Unable to Load URL -" + rfqUrl;
                                    LogText = errormsg;
                                }

                            }
                            else
                            {
                                errormsg = "Containts in AppSettings.xml is Null";
                                LogText = errormsg;
                            }
                        }
                    }
                    else
                    {
                        errormsg = "Containts in AppSettings.xml is Null";
                        LogText = errormsg;
                    }
                }
                else
                {
                    errormsg = "AppSettings.xml file not found";
                    LogText = errormsg;
                }
            }
            catch (Exception ex)
            {
                //LogText = "Error occurred while reading AppSettings xml - " + ex.Message;
                LogText = "Unable to Load URL -" + ex.Message;
                CreateAuditFile("", processorname, "", "ERROR", "Unable to Load URL", buyercode, suppcode, AuditPath);

            }
        }


        private void ProcessRFQ()
        {
            LogText = "RFQ generation Started";
            referrer = ""; Ref_Url = "";
            bool gerenerated = false;
            try
            {
                LoginPostRequest();

                string IsPendingData = GetPendingData();

                //Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(IsPendingData);

                ////LogText = "IsPendingData -" + IsPendingData;
                //LogText = "Total Count: " + convert.ToString(myDeserializedClass.totalcount);

                //foreach (Quote quote in myDeserializedClass.quotes)
                //{
                //    LogText = quote.ORD_OrderNo + " - " + quote.ORD_Stage;

                //    if (quote.ORD_Stage == "Pending")
                //    {
                //        gerenerated = GenerateRFQMtml(quote);

                //        if (gerenerated)
                //        {
                //            //if (!Directory.Exists(mailtxt_path + "\\" + "Backup")) Directory.CreateDirectory(mailtxt_path + "\\" + "Backup");
                //            //File.Move(mailtxtfiles[i], mailtxt_path + "\\" + "Backup\\" + Path.GetFileName(mailtxtfiles[i]));
                //        }
                //        else
                //        {
                //            LogText = "RFQ of " + quote.ORD_OrderNo + " - already generated";
                //            //CreateAuditFile(mailmsgFilename, processorname, rfqno, "ERROR", "Unable to generate RFQ " + errormsg, suppcode, buyercode, AuditPath, "", "", "");
                //            //if (!Directory.Exists(mailtxt_path + "\\" + "Error")) Directory.CreateDirectory(mailtxt_path + "\\" + "Error");
                //            //File.Move(mailtxtfiles[i], mailtxt_path + "\\" + "Error\\" + Path.GetFileName(mailtxtfiles[i]));
                //        }
                //    }
                //}
            }
            catch (Exception ex)
            {
                LogText = ex.Message;
                CreateAuditFile(mailmsgFilename, processorname, "", "ERROR", "Unable to Load URL " + errormsg, suppcode, buyercode, AuditPath);
                if (!Directory.Exists(mailtxt_path + "\\" + "Error")) Directory.CreateDirectory(mailtxt_path + "\\" + "Error");
                File.Move(mailmsgFilename, mailtxt_path + "\\" + "Error\\" + Path.GetFileName(mailmsgFilename));
            }
        }

        private string GetUrl(string txtFile)
        {
            string url = "";
            try
            {
                string txtdata = File.ReadAllText(txtFile);
                string pattern = @"(http|https|ftp)\:\/\/[a-zA-Z0-9\-\.]+\.[a-zA-Z]{2,3}(:[a-zA-Z0-9]*)?\/?([a-zA-Z0-9\-\._\?\,\'\/\\\+&%\$#\=~^?])*";
                // string pattern1 = "<a\\s+(?:[^>]*?\\s+)?href=\"([^\"]+\\?[^\"]+)\"";
                //string pattern = "\\s*(?i)href\\s*=\\s*(\"([^\"]*\")|'[^']*'|([^'\">\\s]+))";
                Regex r = new Regex(pattern);
                MatchCollection mc = Regex.Matches(txtdata, pattern);
                foreach (Match m in mc)
                {
                    if (m.Value.Contains("csl"))
                    {
                        url = convert.ToString(m.Value).Replace("<a href=\"", "").Replace(" href=\"", "").TrimEnd('"');
                        break;
                    }
                }
                if (url == "")
                {
                    LogText = "Unable to get Url from MailText file";
                    throw new Exception("Unable to get Url from MailText file-" + txtFile);
                }
            }
            catch (Exception ex)
            {
                errormsg = "Unable to get Url from MailText file-" + txtFile;
                throw ex;
            }
            //return HttpUtility.UrlDecode(url);
            return url;
        }

        //private string GetProtectedLink(string Link)
        //{
        //    string cTempLink = "";
        //    string cDecodeLnk = HttpUtility.HtmlDecode(Link);
        //    Uri _protectLink = new Uri(Link);
        //    string cQueryLnk = _protectLink.Query.Replace("?url=", "");
        //    if (cQueryLnk.Contains("rapax.tech"))
        //    {
        //        cTempLink = HttpUtility.HtmlDecode(cQueryLnk);
        //    }
        //    return cTempLink;
        //}

        private bool Login()
        {
            bool login = false;
            try
            {
                //combine everything in body because all above are dynamic
                //string body = @"username=" + HttpUtility.UrlEncode(username) + "&password=" + HttpUtility.UrlEncode(password) + "&submit.Signin=Sign+In";
                string body = @"{""username"":""+ username +"",""password"":""+ password +""}";
                referrer = _httpWrapper._CurrentResponse.ResponseUri.OriginalString;
                _httpWrapper.Referrer = referrer;
                //_httpWrapper.Referrer = ;
                _httpWrapper.RequestMethod = "POST";
                //cslUrl = https://csl-msts.cslships.com;
                _httpWrapper._AddRequestHeaders.Add("Origin", @"https://csl-msts.cslships.com");
                _httpWrapper.ContentType = "application/x-www-form-urlencoded";
                _httpWrapper.AcceptMimeType = "*/*";

                //to set the cookie

                bool loginResult = _httpWrapper.PostURL(LoginURL, body, "", "", "");

                if (loginResult)
                {
                    LogText = "logged in";
                    //extract the string from responce in Post request
                    string responce = _httpWrapper._CurrentResponseString;

                    string responselink = _httpWrapper._CurrentResponse.ResponseUri.OriginalString;

                    HtmlNode divNode = _httpWrapper._CurrentDocument.GetElementbyId("form1");

                    login = true;
                }
                else
                {
                    LogText = "Unable to Login";
                    login = false;
                }
            }
            catch (Exception ex)
            {
                LogText = "Error while Login " + ex.Message;
                CreateAuditFile(mailmsgFilename, processorname, rfqno, "ERROR", "Unable to Login " + ex.Message, buyercode, suppcode, AuditPath);
                throw ex;
            }
            return login;
        }

        public bool LoginPostRequest()
        {
            bool flag = false;
            try
            {
                _httpWrapper.RequestMethod = "GET";
                _httpWrapper._AddRequestHeaders.Clear();
                _httpWrapper._SetRequestHeaders.Clear();
                _httpWrapper.Referrer = portalUrl + "Home";

                bool loadurl = _httpWrapper.LoadURL(portalUrl + "QuoteList", "", "", "", "");

                if (loadurl)
                {
                    flag = true;
                }
                else { LogText = "Issue while fetching data."; }




                /*if (IsPendingData)
                {
                    flag = true;
                }
                else { LogText = "Issue while fetching data."; }*/
            }
            catch (Exception ex)
            {
                LogText = "Error while LoginPostRequest " + ex.Message;
            }
            return flag;
        }

        private string GetPendingData()
        {
            bool IsDataExist;
            string JsonData = "";
            try
            {
                _httpWrapper.Referrer = portalUrl + "QuoteList";
                _httpWrapper._AddRequestHeaders.Add("Origin", @portalUrl);

                _httpWrapper.RequestMethod = "POST";
                _httpWrapper.AcceptMimeType = "*/*";
                //_httpWrapper.ContentType = "application/json; charset=UTF-8";

                string postLoginUrl = portalUrl + "QuoteList/LoadQuotes";

                _httpWrapper.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                string body = @"take=20&skip=0&page=1&pageSize=20&sort%5B0%5D%5Bfield%5D=Priority&sort%5B0%5D%5Bdir%5D=asc&sort%5B1%5D%5Bfield%5D=RSP_DateEnqSent&sort%5B1%5D%5Bdir%5D=desc";

                IsDataExist = _httpWrapper.PostURL(postLoginUrl, body, "", "", "");

                if (IsDataExist)
                {
                    JsonData = _httpWrapper._CurrentResponseString;
                }


            }
            catch (Exception ex)
            {
                LogText = "Error while GetPendingData " + ex.Message;
            }
            return JsonData;
        }

        //private bool GenerateRFQMtml(Quote cFinalLink)
        //{
        //    bool result = false;
        //    try
        //    {
        //        MTMLInterchange interchange = new MTMLInterchange();
        //        DocHeader docHeader = new DocHeader();
        //        //Header 
        //        interchange.DocumentHeader = docHeader;
        //        docHeader.VersionNumber = "1";
        //        docHeader.DocType = "RequestForQuote";

        //        interchange.Sender = buyersupplinkcode;
        //        interchange.Recipient = suppcode;
        //        //interchange.Sender = buyercode;
        //        interchange.Sender = buyercode1;

        //        interchange.ControlReference = DateTime.Now.ToString("yyyyMMddHHmmss");
        //        interchange.Identifier = DateTime.Now.ToString("yyyyMMddHHmmss");
        //        interchange.PreparationDate = DateTime.Now.ToString("yyyy-MMM-dd");
        //        interchange.PreparationTime = DateTime.Now.ToString("HH:mm");

        //        //docHeader.MessageReferenceNumber = rfqUrl;
        //        docHeader.MessageNumber = Path.GetFileName(mailmsgFilename);
        //        docHeader.OriginalFile = Path.GetFileName(mailmsgFilename);
        //        //docHeader.CurrencyCode = _obj.d.curr_val;

        //        /*string payterms = _obj.d.paymentTerms;
        //        CommentsCollection _collect = new CommentsCollection();
        //        _collect.Add(new Comments(CommentTypes.ZTP, payterms));*/

        //        _httpWrapper._AddRequestHeaders.Clear();
        //        _httpWrapper._SetRequestHeaders.Clear();
        //        _httpWrapper.RequestMethod = "GET";
        //        _httpWrapper.Referrer = portalUrl + "QuoteList/GetDetailsPage/" + cFinalLink.RSP_ID;
        //        string postLoginUrl = portalUrl + "QuoteList/GetDetails?id=" + cFinalLink.RSP_ID + "&_=1704198463380";

        //        bool loadurl = _httpWrapper.LoadURL(postLoginUrl, "", "", "", "");

        //        addressString = _httpWrapper._CurrentResponseString;
        //        HtmlDocument doc1 = new HtmlDocument();
        //        doc1.LoadHtml(addressString);

        //        //Header Details
        //        Header str = GetHeaderDetails(addressString);

        //        //check for already created RFQ
        //        string downloadRFQPath = Environment.CurrentDirectory + "\\" + "DownloadedRFQ.txt";

        //        if (!File.Exists(downloadRFQPath) || !File.ReadAllLines(downloadRFQPath).Contains(ref_Number))
        //        {
        //            HtmlNode unique = doc1.DocumentNode.SelectSingleNode("//input[@id='PageUniqueKey']");

        //            uniqueKeyValue = unique.GetAttributeValue("value", "");

        //            Console.WriteLine(uniqueKeyValue);
        //            //var unique = doc1.DocumentNode.SelectNodes("//input[@id='PageUniqueKey']");

        //            //reference number
        //            docHeader.References.Add(new Reference(ReferenceQualifier.UC, ref_Number));

        //            //add any header comment here
        //            Comments _HderRmrks = new Comments();
        //            _HderRmrks.Qualifier = CommentTypes.PUR;
        //            _HderRmrks.Value = str.PurchasingOffice + " , " + str.MAOnly;
        //            docHeader.Comments.Add(_HderRmrks);

        //            //rfq name
        //            Comments _HderRmrks1 = new Comments();
        //            _HderRmrks1.Qualifier = CommentTypes.ZAT;
        //            _HderRmrks1.Value = ref_Name;
        //            docHeader.Comments.Add(_HderRmrks1);


        //            //Party Details
        //            docHeader.PartyAddresses = GetPartyAddress(str, docHeader);

        //            //LinesItems
        //            docHeader.LineItems = GetLineItems(cFinalLink);

        //            docHeader.LineItemCount = docHeader.LineItems.Count;

        //            if (docHeader.LineItemCount == 0)
        //            {
        //                throw new Exception("Lineitem count is zero");
        //            }

        //            string fileName = rfqmtml_path + "\\" + "MTML_RFQ_" + convert.ToFileName(ref_Number) + "_" + buyercode + "_" + suppcode + "_" + DateTime.Now.ToString("ddMMyyyyHHmmss") + ".xml";
        //            interchange.MTMLFile = Path.GetFileName(fileName);
        //            interchange.BuyerSuppInfo.DocType = "RequestForQuote";
        //            MTMLClass _class = new MTMLClass();
        //            _class.Create(interchange, fileName);

        //            LogText = "RFQ Mtml Generated for " + ref_Number;

        //            LogText = "RFQ MTML Generated: " + fileName;

        //            CreateAuditFile(fileName, processorname, ref_Number, "SUCCESS", "RFQ Generated Successfully for " + ref_Number, buyercode, suppcode, AuditPath);
        //            //File.AppendAllText(Environment.CurrentDirectory + "\\" + "DownloadedRFQ.txt", ref_Number + Environment.NewLine);
        //            File.AppendAllText(downloadRFQPath, ref_Number + Environment.NewLine);

        //            result = true;
        //        }
        //        else
        //        {
        //            result = false;

        //            //LogText = "MTML File is already created : " + ref_Number;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        LogText = "Error while Generating MTML " + ex.Message;
        //        throw ex;
        //    }
        //    return result;
        //}


    }
}
