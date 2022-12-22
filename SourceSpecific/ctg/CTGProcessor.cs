using MDR_Downloader.Helpers;
using System.Xml;

namespace MDR_Downloader.ctg;
    
public class CTG_Processor
{
    /*
    public ctg_basics ObtainBasicDetails(XmlNode fs)
    {
        ctg_basics ctg = new ();

        // need the identity and status modules from the protocol section
        string protocol_path = "Struct [@Name='Study']/Struct [@Name='ProtocolSection']/";
        string id_path = "Struct [@Name='IdentificationModule']/Field [@Name='NCTId']";
        string sd_sid = fs.SelectSingleNode(protocol_path + id_path)?.InnerText ?? "";

        string last_updated_path = "Struct [@Name='StatusModule']/Struct [@Name='LastUpdatePostDateStruct']/Field [@Name='LastUpdatePostDate']";
        string last_updated = fs.SelectSingleNode(protocol_path + last_updated_path)?.InnerText ?? ""; 

        ctg.sd_sid = sd_sid;
        ctg.last_updated = last_updated.FetchDateTimeFromDateString();
        ctg.file_name = sd_sid + ".xml";
        ctg.file_path = sd_sid[..7] + "xxxx";
        ctg.remote_url = "https://clinicaltrials.gov/ct2/show/" + sd_sid;

        return ctg;

    }
        */
}

