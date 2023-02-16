using HtmlAgilityPack;
using ScrapySharp.Html;
using ScrapySharp.Network;
using System.Net;
using System.Text.RegularExpressions;

namespace MDR_Downloader.vivli
{
    public class Vivli_Processor
    {

        public int GetStudyNumbers(WebPage startPage)
        {
            int results = 0;
            var results_div = startPage.Find("h3", By.Class("results")).FirstOrDefault();
            if (results_div is not null)
            {
                string results_text = results_div.InnerText;
                results_text = results_text.Replace(",", "").Trim();
                if (Regex.Match(results_text, @"\d+").Success)
                {
                    results = Int32.Parse(Regex.Match(results_text, @"\d+").Value);
                }
            }
            return results;
        }


        public List<VivliURL> GetStudyInitialDetails(WebPage webPage, int pagenum)
        {
            int n = 10000 + ((pagenum - 1) * 25);
            List<VivliURL> page_study_list = new();

            var content = webPage.Find("div", By.Class("content")).FirstOrDefault();
            if (content is not null)
            {
                var studyPanels = content.SelectNodes("div[contains(@class,'row')][2]/div/div[contains(@class,'panel')]");
                foreach (HtmlNode panel in studyPanels)
                {
                    if (!panel.HasClass("facets"))
                    {
                        n++;
                        VivliURL st = new()
                        {
                            id = n
                        };
                        HtmlNode[] paneldivs = panel.SelectNodes("div").ToArray();
                        if (paneldivs.Length >= 3)
                        {
                            st.name = paneldivs[0].SelectSingleNode("h3/a").InnerText.Replace("\n", "")
                                .Replace("\r", "").Trim();
                            st.doi = paneldivs[2].SelectSingleNode("a[1]").InnerText.Replace("\n", "")
                                .Replace("\r", "").Trim();

                            string work_id = st.doi[(st.doi.LastIndexOf("/", StringComparison.Ordinal) + 1)..];
                            if (work_id.Contains('.'))
                            {
                                work_id = work_id.Replace(".", "_");
                                st.type = "d";
                                st.vivli_url = "https://prdapi.vivli.org/api/dataPackages/" + work_id + "/metadata";
                            }
                            else
                            {
                                st.type = "s";
                                st.vivli_url = "https://prdapi.vivli.org/api/studies/" + work_id + "/metadata/fromdoi";
                            }

                            page_study_list.Add(st);
                        }
                    }
                }
            }
            return page_study_list;
        }

        public async Task GetAndStoreStudyDetails(VivliURL s, VivliDataLayer repo, ILoggingHelper logging_repo)
        {
            // VivliCopyHelpers vch = new();

            int seqnum = s.id;
            if (seqnum > 10001)
            {
                // Get data from the vivli website API as a json response
                // Use the url previously stored in the VivliRecord.

                //VivliRecord st = new();
                string? url = s.vivli_url;

                if (url is not null)
                {
                    HttpClient webClient = new ();
                    HttpResponseMessage response;
                    try
                    {
                        response = await webClient.GetAsync(url);
                    }

                    catch (WebException we)
                    {
                        if (we.Response is HttpWebResponse errorResponse)
                        {
                            if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                            {
                                logging_repo.LogError("Record " + seqnum + "  threw a 404 error");
                            }
                            else
                            {
                                logging_repo.LogError("Record " + seqnum + "  threw error " + errorResponse.StatusCode.ToString());
                            }
                        }
                        throw;
                    }

                    // Read the response into a string and parse 
                    // Newtonsoft json used to deserialise the string into a dynamic object
                    // The Microsoft built in equivalent does not yet provide this functionality

                    // string responseString = response.ToString();

                    //dynamic? json_data = JsonConvert.DeserializeObject<ExpandoObject>(responseString, new ExpandoObjectConverter());

                    /*
                    if (json_data is not null)
                    {
                        string? record_type = s.type;
                        if (record_type == "s")
                        {
                            VivliRecord vr = new();

                            vr.id = seqnum;
                            vr.vivli_id = json_data.id;
                            vr.study_title = json_data.studyTitle;
                            vr.acronym = json_data.acronym;
                            vr.pi_firstname = json_data.principalInvestigator.firstName;
                            vr.pi_lastname = json_data.principalInvestigator.lastName;
                            vr.pi_orcid_id = json_data.principalInvestigator.orcidId;
                            vr.protocol_id = json_data.sponsorProtocolId;
                            vr.sponsor = json_data.leadSponsor.agency;
                            vr.nct_id = json_data.nctId;
                            vr.dobj_id = json_data.digitalObjectId;
                            vr.data_prot_level = json_data.dataProtectionLevel;
                            vr.save_ipd_for_future = (json_data.saveStudyIPDForFutureUse is not null) ?
                                                        json_data.saveStudyIPDForFutureUse : null;
                            vr.ipd_package_id = json_data.ipdDataPackageId;
                            vr.metadata_package_id = json_data.metaDataDataPackageId;
                            vr.ipd_content_type = json_data.ipdContentType;
                            vr.study_metadata_doi = json_data.studyMetadataDoi;
                            vr.study_req_behav = json_data.studyRequestBehavior;
                            vr.bulk_upload_type = json_data.bulkUploadContentType;
                            vr.downloadable_ipd_package = (json_data.downloadableStudyIPDDataPackage is not null) ?
                                                            json_data.downloadableStudyIPDDataPackage : null;
                            vr.ipd_data_supp_on_submission = (json_data.studyIPDDataPackagesSuppliedOnSubmission is not null) ?
                                                                json_data.studyIPDDataPackagesSuppliedOnSubmission : null;
                            vr.doi_stem = json_data.doiStem;
                            vr.ipd_package_doi = json_data.studyIPDDataPackageDoi;
                            vr.additional_info = json_data.additionalInformation;
                            vr.date_created = json_data.createdDate;
                            vr.date_updated = json_data.updatedDate;

                            // Store record in database
                            repo.StoreStudyRecord(vr);
                        }

                        if (record_type == "d")
                        {
                            PackageRecord pr = new();
                            pr.id = seqnum;
                            pr.vivli_id = json_data.id;
                            pr.vivli_study_id = json_data.studyId;
                            pr.package_doi = json_data.dataPackageDoi;
                            pr.doi_stem = json_data.doiStem;
                            pr.package_title = json_data.title;
                            pr.status = json_data.status;
                            pr.downloadable = (json_data.dataPackagePolicy.downloadable is not null) ?
                                                json_data.dataPackagePolicy.downloadable : null;
                            pr.files_dlable_before = (json_data.dataPackagePolicy.filesDownloadableBeforeSubmitted is not null) ?
                                                    json_data.dataPackagePolicy.filesDownloadableBeforeSubmitted : null;
                            pr.package_type = json_data.dataPackageType;
                            if (json_data.associatedSecondaryAnalysisDOIs is not null)
                            {
                                var assoc_dois = json_data.associatedSecondaryAnalysisDOIs.ToArray();
                                if (assoc_dois.Length > 0)
                                {
                                    int n = 0;
                                    foreach (var doi in assoc_dois)
                                    {
                                        n++;
                                        string sdoi = doi.ToString();
                                        pr.sec_anal_dois += (n == 1) ? sdoi : ", " + sdoi;
                                    }
                                }
                            }

                            repo.StorePackageRecord(pr);

                            if (json_data.dataPackageFileDescriptors is not null)
                            {
                                var files = json_data.dataPackageFileDescriptors.ToArray();
                                if (files.Length > 0)
                                {
                                    List<ObjectRecord> object_recs = new();
                                    int n = 0;
                                    foreach (dynamic item in files)
                                    {
                                        n++;
                                        ObjectRecord obr = new();

                                        obr.id = (seqnum * 100) + n;
                                        obr.package_id = seqnum;
                                        obr.object_type = item.type;
                                        obr.object_name = item.name;
                                        obr.comment = item.comment;
                                        obr.is_complete = (item.isComplete is not null) ?
                                                           item.isComplete : null;
                                        obr.size_kb = item.sizeInKb.ToString();
                                        obr.updated = item.updatedDateTime;
                                        obr.package_doi = item.datapackageDoiFileNumber;

                                        object_recs.Add(obr);
                                    }

                                    repo.StoreObjectRecs(vch.data_object_copyhelper, object_recs);
                                }
                            }
                        }
                   
                    }*/
                }
            }
        }
    }
}
