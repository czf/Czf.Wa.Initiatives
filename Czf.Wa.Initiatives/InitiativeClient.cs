using Czf.Wa.Initiatives.Dto;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Czf.Wa.Initiatives;

public class InitiativeClient : IInitiativeClient
{
    const string INITIATIVE_HEADERS_URL = "https://www2.sos.wa.gov/elections/initiatives/initiatives.aspx?y={0}&t={1}&o=SubmissionDate";
    const string INITIATIVE_URL = "https://www2.sos.wa.gov/elections/initiatives/InitData.asmx/fetchInitiativeData";
    const string INITIATIVE_PEOPLE_TYPE = "p";
    const string INITIATIVE_LEGISTLATURE_TYPE = "l";
    private static TimeZoneInfo _pacifictTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
    internal DateTimeOffset Now;
    private readonly HttpClient _httpClient;
    public InitiativeClient(HttpClient httpClient) 
    {
        _httpClient = httpClient;
    }

    internal InitiativeClient(HttpClient httpClient, DateTimeOffset now) : this(httpClient)
    {
        Now = now;
    }

    /// <summary>
    /// Get all the headers for the current year
    /// </summary>
    /// <returns></returns>
    public async Task<List<InitiativeHeader>> GetInitiativeToThePeopleHeaders() 
    {
        return await GetInitiativeHeaders(INITIATIVE_PEOPLE_TYPE);

    }

    public async Task<List<InitiativeHeader>> GetInitiativeToTheLegislatureHeaders()
    {
        return await GetInitiativeHeaders(INITIATIVE_LEGISTLATURE_TYPE);
    }    

    public async Task<Initiative?> GetInitiative(InitiativeHeader initiativeHeader)
    {
        
        Initiative? result = null;
        var response = await _httpClient.PostAsync(INITIATIVE_URL,
            new StringContent(
                JsonSerializer.Serialize(new { InitID = initiativeHeader.id, newVersionFlag = Convert.ToInt16(initiativeHeader.flag) }))
            {
                Headers = { ContentType = new MediaTypeHeaderValue("application/json") }
            });
        if (response != null)
        {
            if(response.StatusCode == HttpStatusCode.OK)
            {
                var rawJson = await response.Content.ReadAsStringAsync();
                var details = JsonSerializer.Deserialize<InitiativeResponseDTO>(rawJson, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                if (details == null)
                {
                    return result;
                }
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(details.D);
                var rowData = htmlDocument.DocumentNode.SelectNodes("//td");

                

                var contactInfo = ParseContactInfo(rowData);
                var summaryCell = ParseSummaryCell(rowData);
                result = new Initiative(initiativeHeader.id, initiativeHeader.dateFiled, initiativeHeader.fallbackDate, initiativeHeader.assignedNumber, initiativeHeader.primarySponsor, initiativeHeader.subject,
                    contactInfo, summaryCell.BallotTitle, summaryCell.Summary, summaryCell.BallotTitleLetter, summaryCell.CompleteText, summaryCell.FullText);

            }
        }
        return result;

        
    }
    private static ContactInfo ParseContactInfo(HtmlNodeCollection rowData)
    {
        var contactInfoCellLines = rowData.ElementAt(1).InnerHtml.Split("<br>")[1..].ToList();
        var emailIndex = contactInfoCellLines.FindIndex(0, x => x.Contains("mailto:", StringComparison.InvariantCultureIgnoreCase));
        string email = string.Empty;
        if (emailIndex > -1)
        {
            HtmlNode n = HtmlNode.CreateNode(contactInfoCellLines[emailIndex]);
            email = n.InnerText;
        }

        var websiteIndex = contactInfoCellLines.FindIndex(0, x => x.Contains("Website", StringComparison.InvariantCultureIgnoreCase));
        Uri? website = null;
        if (websiteIndex > -1)
        {
            HtmlNode n = HtmlNode.CreateNode(contactInfoCellLines[websiteIndex]);
            var urlString = n.GetAttributeValue<string?>("href", null);
            if (urlString != null)
            {
                website = new Uri(urlString);
            }
        }

        var contactIndex = contactInfoCellLines.FindIndex(0, x => x.Contains("Public Contact Information", StringComparison.InvariantCultureIgnoreCase)); 
        
        return new(
            contactInfoCellLines[contactIndex+1] + "\n" + contactInfoCellLines[contactIndex+2],
            contactInfoCellLines[contactIndex + 3],
            email, website);
    }
    private static SummaryCell ParseSummaryCell(HtmlNodeCollection rowData)
    {
        SummaryCell summaryCell = new SummaryCell();
        string ballotTitle = string.Empty;
        HtmlNode n = rowData[2];
        var anchors = n.SelectNodes(".//a");
        if (anchors == null)
        {
            summaryCell.FullText = n.InnerHtml.Replace("<p></p><p><b>Full Text</b></p><br>", string.Empty).Replace("<br>", "\n");
        }
        else
        {
            var resourceAnchors = anchors.Where(x => !String.IsNullOrEmpty(x.InnerText));

            foreach (var anchor in anchors)
            {
                if (anchor.InnerText == "Ballot Title Letter")
                {
                    summaryCell.BallotTitleLetter = new Uri("https://www2.sos.wa.gov"+anchor.GetAttributeValue<string>("href", string.Empty));
                }
                else if (anchor.InnerText == "View Complete Text")
                {
                    summaryCell.CompleteText = new Uri(anchor.GetAttributeValue<string>("href", string.Empty));
                }
            }

            var paragraphs = n.SelectNodes("//p").Where(x => !string.IsNullOrEmpty(x.InnerText)).ToList();
            if (paragraphs.Count > 2)
            {
                summaryCell.BallotTitle = paragraphs[2].InnerText;
                summaryCell.Summary = paragraphs[4].LastChild.InnerText;
            }

        }

        return summaryCell;

    }
  


    //private static DateTimeOffset ParseSubmittedDate(HtmlNodeCollection rowData, ref string fallbackDate)
    //{
    //    DateTimeOffset submittedDate;
    //    var dateCellText = rowData.First().InnerText;
    //    var delimiterIndex = dateCellText.IndexOf(":");
    //    var titleCopyIndex = dateCellText.IndexOf("Ballot title issued");
    //    if (!DateTimeOffset.TryParse(dateCellText[(delimiterIndex + 1)..titleCopyIndex], out submittedDate))
    //    {
    //        submittedDate = DateTimeOffset.MinValue;
    //        fallbackDate = dateCellText[(delimiterIndex + 1)..titleCopyIndex];
    //    }

    //    return submittedDate;
    //}

    private async Task<List<InitiativeHeader>> GetInitiativeHeaders(string initiativeType)
    {
        List<InitiativeHeader> result = new();
        var pacificStandardTime = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

        var currentYear = Now != DateTimeOffset.MinValue ? Now.Year :
            DateTimeOffset.UtcNow.ToOffset(_pacifictTimeZone.GetUtcOffset(DateTime.Now)).Year;

        var response = await _httpClient.GetAsync(string.Format(INITIATIVE_HEADERS_URL, currentYear, initiativeType));
        if (response != null)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var htmlContent = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(htmlContent);
                HtmlNodeCollection initiativeTables = htmlDocument.DocumentNode.SelectNodes("//div[@id=\"Canvas\"]//div[@class=\"table-responsive\"]//table[@class=\"item table\"]/tr[@class=\"listing\"]");
                if (initiativeTables != null)
                {
                    foreach (var row in initiativeTables)
                    {
                        int id = row.GetAttributeValue("id", 0);
                        bool flag = row.GetAttributeValue<bool>("flag", false);
                        var headerNodes = row.ChildNodes;
                        string fallbackDate = string.Empty;
                        if (!DateTimeOffset.TryParse(headerNodes[0].InnerText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset dateFiled))
                        {
                            fallbackDate = headerNodes[0].InnerText;
                        }
                        else
                        {
                            dateFiled = new DateTimeOffset(dateFiled.DateTime, pacificStandardTime.GetUtcOffset(dateFiled.DateTime));
                        }

                        int.TryParse(headerNodes[1].InnerText, out int assignedNumber);
                        var sponsor = headerNodes[2].InnerText;
                        var subject = headerNodes[3].InnerText;

                        var initiativeHeader = new InitiativeHeader(
                            id,
                            flag,
                            dateFiled,
                            fallbackDate,
                            assignedNumber,
                            sponsor,
                            subject);
                        result.Add(initiativeHeader);
                    }
                }
            }
        }
        return result;
    }

    private class InitiativeResponseDTO
    {
        public string? D { get; set; }
    }
    private class SummaryCell
    {
        public Uri? BallotTitleLetter { get; set; }
        public Uri? CompleteText { get; set; }
        public string? BallotTitle { get; set; }
        public string? Summary { get; set; }
        public string? FullText { get; set; }
    }
}
