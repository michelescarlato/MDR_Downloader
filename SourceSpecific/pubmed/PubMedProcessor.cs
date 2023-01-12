using HtmlAgilityPack;
using MDR_Downloader.Helpers;
using MDR_Downloader.pubmed;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;
using System.Xml;
using System.Xml.Serialization;
using MDR_Downloader.yoda;

namespace MDR_Downloader.pubmed;

public class PubMed_Processor
{
    public FullObject? ProcessData(PubmedArticle art)
    {
        var citation = art.MedlineCitation;

        if (citation is null)
        {
            // big problems!
            return null;
        }

        var article = citation.Article;

        if (article is null)
        {
            // big problems!
            return null;
        }

        // Establish main citation object
        // and list structures to receive data
        FullObject b = new();

        List<string>? ArticleLangs = new();
        List<Creator>? Creators = new();
        List<ArticleType>? ArticleTypes = new();
        List<EReference>? EReferences = new();
        List<Database>? DatabaseList = new();
        List<Fund>? FundingList = new();
        List<Substance>? SubstanceList = new();
        List<MeshTerm>? MeshList = new();
        List<SupplMeshTerm>? SupplMeshList = new();
        List<KWord>? KeywordList = new();
        List<Correction>? CorrectionsList = new();
        List<AdditionalId>? AdditionalIds = new();
        List<HistoryDate>? History = new ();
        List<ArticleId>? ArticleIds = new ();

        b.ipmid = citation.PMID?.Value;
        b.pmid_version = citation.PMID?.Version;
        string? pmid = b.ipmid.ToString();
        b.sd_oid = pmid;

        b.status = citation.Status;
        b.owner = citation.Owner;
        b.versionID = citation.VersionID;

        b.dateCitationCompleted = new NumericDate(citation.DateCompleted?.Year,
                                                  citation.DateCompleted?.Month,
                                                  citation.DateCompleted?.Day);
        b.dateCitationRevised = new NumericDate(citation.DateRevised?.Year,
                                                  citation.DateRevised?.Month,
                                                  citation.DateRevised?.Day);

        b.articleTitle = article.ArticleTitle;
        b.vernacularTitle = article.VernacularTitle;
        b.medlinePgn = article.Pagination?.MedlinePgn;

        var journal = article.Journal;
        if (journal is not null)
        {
            b.journalTitle = journal.Title;
            b.journalISOAbbreviation = journal.ISOAbbreviation;
            b.journalIssnType = journal.ISSN?.IssnType;
            b.journalIssn = journal.ISSN?.Value;

            var journalIssue = journal.JournalIssue;
            if (journalIssue is not null)
            {
                b.pubYear = journalIssue.PubDate?.Year;
                b.pubMonth = journalIssue.PubDate?.Month;
                b.pubDay = journalIssue.PubDate?.Day;
                b.medlineDate = journalIssue.PubDate?.MedlineDate;
                b.journalVolume = journalIssue.Volume;
                b.journalIssue = journalIssue.Issue;
                b.journalCitedMedium = journalIssue.CitedMedium;
            }
        }

        var journalInfo = citation.MedlineJournalInfo;
        if (journalInfo is not null)
        {
            b.journalCountry = journalInfo.Country;
            b.journalMedlineTA = journalInfo.MedlineTA;
            b.journalNlmUniqueID = journalInfo.NlmUniqueID;
            b.journalISSNLinking = journalInfo.ISSNLinking;
        }

        b.dateCitationRevised = new NumericDate(article.ArticleDate?.Year,
                                                article.ArticleDate?.Month,
                                                article.ArticleDate?.Day);
        b.articleDateType = article.ArticleDate?.DateType;
        b.pubModel = article.PubModel;
        //b.abstractCopyright = article.Abstract?.CopyrightInformation;

        if (citation.ChemicalList is not null && citation.MeshHeadingList.Length > 0)
        {
            foreach (var s in citation.ChemicalList)
            {
                SubstanceList.Add(new Substance(s.NameOfSubstance?.UI,
                                                s.NameOfSubstance?.Value));
            }
        }

        if (citation.MeshHeadingList is not null && citation.MeshHeadingList.Length > 0)
        {
            foreach (var m in citation.MeshHeadingList)
            {
                MeshList.Add(new MeshTerm(m.DescriptorName?.UI, m.DescriptorName?.MajorTopicYN,
                                m.DescriptorName?.Type, m.DescriptorName?.Value));
            }
        }

        if (citation.SupplMeshList is not null)
        {
            if (citation.SupplMeshList.SupplMeshName?.Any() == true)
            {
                foreach (var s in citation.SupplMeshList.SupplMeshName)
                {
                    SupplMeshList.Add(new SupplMeshTerm(s.Type, s.UI, s.Value));
                }
            }
        }

        if (citation.KeywordList is not null)
        {
            b.keywordOwner = citation.KeywordList.Owner;
            if (citation.KeywordList.Keyword?.Any() == true)
            {
                foreach (var k in citation.KeywordList.Keyword)
                {
                    KeywordList.Add(new KWord(k.MajorTopicYN, k.Value));
                }
            }
        }

        if (citation.CommentsCorrectionsList is not null && citation.CommentsCorrectionsList.Length > 0)
        {
            foreach (var c in citation.CommentsCorrectionsList)
            {
                CorrectionsList.Add(new Correction(c.RefSource, c.PMID?.Value, 
                                                    c.PMID?.Version, c.RefType));
            }
        }


        if (article.Languages is not null && article.Languages.Length > 0)
        {
            foreach (string lang in article.Languages)
            {
                ArticleLangs.Add(lang);
            }
        }

        if (article.OtherIDs is not null && article.OtherIDs.Length > 0)
        {
            foreach (var t in article.OtherIDs)
            {
                AdditionalIds.Add(new AdditionalId(t.Source, t.Value));
            }
        }

        if (article.ELocationIDs is not null && article.ELocationIDs.Length > 0)
        {
            foreach (var e in article.ELocationIDs)
            {
                EReferences.Add(new EReference(e.EIdType, e.Value));
            }
        }

 
        if (article.AuthorList is not null)
        {
            if (article.AuthorList.Author?.Any() == true)
            {
                foreach (var a in article.AuthorList.Author)
                {
                    List<AffiliationInfo>? affiliations = null;
                    if (a.Affiliations?.Any() == true)
                    {
                        affiliations = new();
                        foreach (var aff in a.Affiliations)
                        {
                            affiliations.Add(new AffiliationInfo(aff.Affiliation, aff.Identifier?.Source,
                                                                 aff.Identifier?.Value));
                        }
                    }
                    Creators.Add(new Creator(a.CollectiveName, a.LastName, a.ForeName,
                                             a.Initials, a.Suffix, a.Identifier?.Source,
                                             a.Identifier?.Value, affiliations));
                }
            }
        }

        

        if (article.PublicationTypeList is not null && article.PublicationTypeList.Length > 0)
        {
            foreach (var p in article.PublicationTypeList)
            {
                ArticleTypes.Add(new ArticleType(p.UI, p.Value));
            }
        }


        if (article.DataBankList is not null)
        {
            if (article.DataBankList.DataBanks?.Any() == true)
            {
                foreach (var d in article.DataBankList.DataBanks)
                {
                    List<string>? accessionNumbers = null;
                    if (d.AccessionNumberList is not null && d.AccessionNumberList.Length > 0)
                    {
                        accessionNumbers = new();
                        foreach (string accNum in d.AccessionNumberList)
                        {
                            accessionNumbers.Add(accNum);
                        }
                    }
                    DatabaseList.Add(new Database(d.DataBankName, accessionNumbers));
                }
            }
        }


        if (article.GrantList is not null)
        {
            if (article.GrantList.Grant?.Any() == true)
            {
                foreach (var g in article.GrantList.Grant)
                {
                    FundingList.Add(new Fund(g.GrantID, g.Acronym, g.Agency, g.Country));
                }
            }
        }


        var pubmedData = art.PubmedData;
        if (pubmedData is not null)
        {
            b.PublicationStatus = pubmedData.PublicationStatus;

            if (pubmedData.History?.Any() == true)
            {
                foreach(var hd in pubmedData.History)
                {
                    History.Add(new HistoryDate(hd.Year, hd.Month, hd.Day, hd.PubStatus));
                }
            }

            if (pubmedData.ArticleIdList?.Length > 0)
            {
                foreach (var aid in pubmedData.ArticleIdList)
                {
                    ArticleIds.Add(new ArticleId(aid.IdType, aid.Value));
                }
            }
        }

        b.ArticleLangs = ArticleLangs;
        b.Creators = Creators;
        b.ArticleTypes = ArticleTypes;
        b.EReferences = EReferences;
        b.DatabaseList = DatabaseList;
        b.FundingList = FundingList;
        b.SubstanceList = SubstanceList;
        b.MeshList = MeshList;
        b.SupplMeshList = SupplMeshList;
        b.KeywordList = KeywordList;
        b.CorrectionsList = CorrectionsList;
        b.History = History;
        b.ArticleIds = ArticleIds;
        b.AdditionalIds = AdditionalIds;

        return b;
    }


}


