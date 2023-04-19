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

    public Periodical? ProcessNLMData(NLMRecord rec)
    {
        Periodical p = new(rec.NlmUniqueID);

        var t = rec.TitleMain;
        if (t is not null)
        {
            string title = t.Title.Value;
            if (!string.IsNullOrEmpty(title))
            {
                title = title.Replace("&amp;", "&");
                title = title.Replace("&quot;", "");
                p.title = title.Trim(' ', '.');
            }
        }
        
        var issn = rec.ISSN;
        if (issn?.Any() is true)
        {
            foreach (var iss in issn)
            {
                string eissn = "", pissn = "", xissn = "";
                string issnType = iss.IssnType;
                if (iss.IssnType == "Print")
                {
                    pissn = ", " + iss.Value.Replace("-", "");
                }
                else if (iss.IssnType == "Electronic")
                {
                    eissn = ", " + iss.Value.Replace("-", "");
                }
                else    // issn type usually "Undetermined"
                {
                    xissn = ", " + iss.Value.Replace("-", "");
                }
                p.pissn = pissn == "" ? null : pissn[2..];
                p.eissn = eissn == "" ? null : eissn[2..];
                p.xissn = xissn == "" ? null : xissn[2..];
            }
        }
        
        p.medline_ta = rec.MedlineTA;

        var i = rec.PublicationInfo;
        if (i is not null)
        {
            p.publication_country = i.Country;
            
            var im = i.Imprint;
            if (im is not null)
            {  
               p.full_imprint = im.ImprintFull;
               string? implace = im.Place;
               if (!string.IsNullOrEmpty(implace))
               {
                   implace = implace.Replace("&amp;", "&");
                   implace = implace.Replace("&quot;", "");
                   implace = implace.Trim(' ', '[', ']', ':', ',', ';', '.');
                   if (implace.EndsWith("etc"))
                   {
                       implace = implace[..^3];
                   }
                   implace = implace.Trim(' ', '[', ']', ':', ',', ';', '.');
                   p.imprint_place = implace;
               }
               p.date_issued = im.DateIssued;
               
               string? pub = im.Entity;
               if (!string.IsNullOrEmpty(pub))
               {
                   // trim string, remove fullstops, and make escaped characters normal or disappear.

                   pub = pub.Trim(' ', '[', ']', ':', ',', ';');
                   pub = pub.Replace(".", "");
                   pub = pub.Replace("&amp;", "&");
                   pub = pub.Replace("&quot;", "");
                   pub = pub.Replace("&lt;", "");
                   pub = pub.Replace("&gt;", "");

                   // drop right hand bits after any remaining semi-colon.

                   if (pub.IndexOf(';') > -1)
                   {
                       pub = pub[..pub.IndexOf(';')];
                   }

                   // See if it is, or starts with, any of the 'meaningless' names
                   // add_pub starts as true unless the name starts with 'El' or 'La'

                   bool add_pub = !(pub.StartsWith("El ") || pub.StartsWith("La "));

                   if (add_pub && pub.StartsWith("Le ")
                               && !pub.StartsWith("Le Jacq") && !pub.StartsWith("Le François"))
                   {
                       add_pub = false;
                   }

                   if (add_pub &&
                       (pub.StartsWith("O Instituto") || pub.StartsWith("O Hospital") || pub.StartsWith("O Centro")
                        || pub.StartsWith("O Departamento") || pub.StartsWith("O Associação")))
                   {
                       add_pub = false;
                   }

                   if (add_pub && pub.StartsWith("The "))
                   {
                       if (pub.StartsWith("The Alcohol") || pub.StartsWith("The American") ||
                           pub.StartsWith("The Australian") || pub.StartsWith("The Berkeley") ||
                           pub.StartsWith("The Biochemical") || pub.StartsWith("The British") ||
                           pub.StartsWith("The Japan") || pub.StartsWith("The Korean") ||
                           pub.StartsWith("The Kenya") || pub.StartsWith("The Dougmar"))
                       {
                           add_pub = true;
                       }
                       else if (pub.StartsWith("The Eastern") || pub.StartsWith("The Egyptian") ||
                                pub.StartsWith("The European") || pub.StartsWith("The Finnish") ||
                                pub.StartsWith("The Health") || pub.StartsWith("The H Edgar") ||
                                pub.StartsWith("The Iconoclast") || pub.StartsWith("The American") ||
                                pub.StartsWith("The Penicillin") || pub.StartsWith("The Pan African"))
                       {
                           add_pub = true;
                       }
                       else if (pub.StartsWith("The Pysch") || pub.StartsWith("The Phys") ||
                                pub.StartsWith("The Lancet") || pub.StartsWith("The Medical") ||
                                pub.StartsWith("The Menninger") || pub.StartsWith("The Methodist") ||
                                pub.StartsWith("The National Association") || pub.StartsWith("The Royal") ||
                                pub.StartsWith("The Resident") || pub.StartsWith("The South") ||
                                pub.StartsWith("The Southern") || pub.StartsWith("The University of"))
                       {
                           add_pub = true;
                       }
                       else
                       {
                           add_pub = false;
                       }
                   }

                   if (pub is "sn" or "sl" or "publisher not identified")
                   {
                       add_pub = false;
                   }

                   if (add_pub)
                   {
                       if (pub.StartsWith("Published "))
                       {
                           // simplify these if possible

                           pub = pub.Replace("Published and distributed by ", "");
                           pub = pub.Replace("Published on behalf of the ", "");
                           pub = pub.Replace("Published and maintained by ", "");
                           pub = pub.Replace("Published at the offices of the Society ", "");
                           pub = pub.Replace("Published by authority of the ", "");
                           pub = pub.Replace("Published by the ", "");
                           pub = pub.Replace("Published by ", "");
                           pub = pub.Replace("Published for the ", "");
                           pub = pub.Replace("Published for ", "");

                           // often still have a second part with the publisher named at the end

                           int by_pos = pub.LastIndexOf("by ", StringComparison.Ordinal);
                           if (by_pos > -1)
                           {
                               pub = pub[(by_pos + 2)..];
                           }
                       }
                       
                       // Trim odd beginnings and ends
                       // and abbreviations for 'company'

                       if (pub.StartsWith("OOO "))
                       {
                           pub = pub[4..];
                       }
                       if (pub.EndsWith("etc") || pub.EndsWith("ect"))
                       {
                           pub = pub[..^3];
                       }
                       pub = pub.Trim(' ', '[', ']', ':', ',', ';');
                       
                       if (pub.EndsWith(" et Cie") || pub.EndsWith(" et cie"))
                       {
                           pub = pub[..^7];
                       }
                       if (pub.EndsWith(" S L U"))
                       {
                           pub = pub[..^6];
                       }    
                       if (pub.EndsWith(" GmbH"))
                       {
                           pub = pub[..^5];
                       }                           
                       if (pub.EndsWith(" Inc") || pub.EndsWith(" inc")
                           || pub.EndsWith(" Ltd") || pub.EndsWith(" LTD")
                           || pub.EndsWith(" B V") || pub.EndsWith(" S A")
                           || pub.EndsWith(" LLC") || pub.EndsWith(" SAS")
                           || pub.EndsWith(" SLU") || pub.EndsWith(" A/S")
                           || pub.EndsWith(" Cie") )
                       {
                           pub = pub[..^4];
                       }
                       if (pub.EndsWith(" Co") || pub.EndsWith(" SL")
                          || pub.EndsWith(" BV") || pub.EndsWith(" SA")
                          || pub.EndsWith(" AG") || pub.EndsWith(" S A"))
                       {
                           pub = pub[..^3];
                       }                           
                       pub = pub.Trim(' ', '[', ']', ':', ',', ';');
                       
                       // Expand some abbreviations for greater consistency
                       
                       pub = pub.Replace("HMSO", "H M Stationery Office");
                       pub = pub.Replace("Assn", "Association");
                       pub = pub.Replace("Pub ", "Publishing ");
                       if (pub.EndsWith(" Pub"))
                       {
                           pub = pub[..^4] + " Publishing";
                       }
                       pub = pub.Replace("Off ", "Office ");
                       if (pub.EndsWith(" Off"))
                       {
                           pub = pub[..^4] + " Office";
                       }
                       pub = pub.Replace("Corp ", "Corporation ");
                       if (pub.EndsWith(" Corp"))
                       {
                           pub = pub[..^5] + " Corporation";
                       }
                       pub = pub.Replace("Univ ", "University ");
                       if (pub.EndsWith(" Univ"))
                       {
                           pub = pub[..^5] + " University";
                       }
                       
                       // Make this change to reflect an organisational change (from about 2005)
                       // that is still not reflected in most of the available source data

                       if (pub.Contains("Lippincott"))
                       {
                           pub = "Wolters Kluwer Health";
                       }
                       
                       p.publisher = pub;
                   }
               }
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


