using MDR_Downloader.Helpers;

namespace MDR_Downloader.pubmed;

public class PubMed_Processor
{
    private readonly PubMedDataLayer _pubmed_repo;
    private readonly ILoggingHelper _logging_helper;

    public PubMed_Processor(PubMedDataLayer pubmed_repo, ILoggingHelper logging_helper)
    {
        _pubmed_repo = pubmed_repo;
        _logging_helper = logging_helper;
    }

    public FullObject? ProcessData(PubmedArticle art)
    {
        var citation = art.MedlineCitation;

        if (citation is null)
        {
            _logging_helper.LogError("No 'Medline Citation' component of PubMed article class found"); 
            return null;
        }

        var article = citation.Article;

        if (article is null)
        {
            _logging_helper.LogError("No 'article' component of PubMed article / citation class found"); 
            return null;
        }

        // Establish main citation object
        // and list structures to receive data
        FullObject b = new();
        List<ArticleEDate> ArticleEDates = new();
        List<string> ArticleLangs = new();
        List<Creator> Creators = new();
        List<ArticleType> ArticleTypes = new();
        List<EReference> EReferences = new();
        List<Database> DatabaseList = new();
        List<Fund> FundingList = new();
        List<Substance> SubstanceList = new();
        List<MeshTerm> MeshList = new();
        List<SupplMeshTerm> SupplMeshList = new();
        List<KWord> KeywordList = new();
        List<Correction> CorrectionsList = new();
        List<AdditionalId> AdditionalIds = new();
        List<HistoryDate> History = new ();
        List<ArticleId> ArticleIds = new ();
        List<ISSNRecord> ISSNList = new();

        b.ipmid = citation.PMID.Value;
        b.pmid_version = citation.PMID.Version;
        string pmid = b.ipmid.ToString();
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
            if (journal.ISSN is not null && journal.ISSN.Length > 0)
            {
                foreach (var i in journal.ISSN)
                {
                    ISSNList.Add(new ISSNRecord(i.IssnType, i.Value));
                }
            }

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
        b.pubModel = article.PubModel;

        if (citation.ChemicalList is not null && citation.MeshHeadingList?.Length > 0)
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

        if (citation.SupplMeshList is not null && citation.SupplMeshList.Length > 0)
        {
            foreach (var s in citation.SupplMeshList)
            {
                SupplMeshList.Add(new SupplMeshTerm(s.Type, s.UI, s.Value));
            }
        }
        
        if (citation.OtherIDs is not null && citation.OtherIDs.Length > 0)
        {
            foreach (var s in citation.OtherIDs)
            {
                AdditionalIds.Add(new AdditionalId(s.Source, s.Value));
            }
        }

        if (citation.KeywordList is not null)
        {
            b.keywordOwner = citation.KeywordList.Owner;
            if (citation.KeywordList.Keyword?.Any() is true)
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
                string? lang_to_add;
                if (lang == "eng")
                {
                    lang_to_add = "en";
                }
                else
                {
                    lang_to_add = lang.lang_3_to_2();
                    if (lang_to_add == "??")
                    {
                        // need to use the database
                        lang_to_add = _pubmed_repo.lang_3_to_2(lang);
                    }
                }
                if (lang_to_add is not null)
                {
                    ArticleLangs.Add(lang_to_add);
                }
            }
        }


        if (article.ArticleDates is not null && article.ArticleDates.Length > 0)
        {
            ArticleEDates.AddRange(article.ArticleDates.Select(ad => new ArticleEDate(ad.DateType, 
                                                                      ad.Year, ad.Month, ad.Day)));
        }

        if (article.ELocationIDs is not null && article.ELocationIDs.Length > 0)
        {
            EReferences.AddRange(article.ELocationIDs.Select(e => new EReference(e.EIdType, e.Value)));
        }

 
        if (article.AuthorList is not null)
        {
            if (article.AuthorList.Author?.Any() is true)
            {
                foreach (var a in article.AuthorList.Author)
                {
                    List<AffiliationInfo>? affiliations = null;
                    if (a.Affiliations?.Any() is true)
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
            if (article.DataBankList.DataBanks?.Any() is true)
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
            if (article.GrantList.Grant?.Any() is true)
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

            if (pubmedData.History?.Any() is true)
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

        b.ArticleEDates = ArticleEDates;
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
        b.ISSNList = ISSNList;

        return b;
    }

    public PublisherObject? ProcessNLMData(NLMRecord rec)
    {
        PublisherObject p = new();
        p.nlm_unique_id = rec.NlmUniqueID.ToString();

        var t = rec.TitleMain;
        if (t is not null)
        {
            p.title = t.Title.Value;
        }

        p.medline_ta = rec.MedlineTA;

        var i = rec.PublicationInfo;
        if (i is not null)
        {
            p.publication_country = i.Country;
            var im = i.Imprint;
            if (im is not null)
            {
                p.imprint_place = im.Place;
                p.publisher = im.Entity;
                p.date_issued = im.DateIssued;
            }
        }

        var issn = rec.ISSN;
        if (issn?.Any() is true)
        {
            int n = 0;
            foreach (var iss in issn)
            {
                string separator = n == 0 ? "" : ", ";
                p.issn_list += separator + iss.IssnType + ": " + iss.Value;
                n++;
            }
        }

        var notes = rec.GeneralNote;
        if (notes?.Any() is true)
        {
            int n = 0;
            foreach (var nt in notes)
            {
                string separator = n == 0 ? "" : "; ";
                p.general_notes += separator + nt.Value.ReplaceUnicodes();
                n++;
            }
        }
        
        return p;
    }



}


