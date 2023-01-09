using Czf.Wa.Initiatives.Dto;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;


namespace Czf.Wa.Initiatives;

public class InitiativeClient : IInitiativeClient
{
    const string INITIATIVE_URL = "https://www2.sos.wa.gov/elections/initiatives/initiatives.aspx?y={0}&t={1}&o=SubmissionDate";
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

    private async Task<List<InitiativeHeader>> GetInitiativeHeaders(string initiativeType)
    {
        List<InitiativeHeader> result = new();


        var currentYear = Now != DateTimeOffset.MinValue ? Now.Year :
            DateTimeOffset.UtcNow.ToOffset(_pacifictTimeZone.GetUtcOffset(DateTime.Now)).Year;

        var response = await _httpClient.GetAsync(string.Format(INITIATIVE_URL, currentYear, initiativeType));
        if (response != null)
        {
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var htmlContent = await response.Content.ReadAsStringAsync();
                HtmlDocument htmlDocument = new();
                htmlDocument.LoadHtml(htmlContent);
                HtmlNodeCollection initiativeTables = htmlDocument.DocumentNode.SelectNodes("""//div[@id="Canvas"]//div[@class="table-responsive"]//table[@class="item table"]/tr[@class="listing"]""");

                foreach (var row in initiativeTables)
                {
                    int id = row.GetAttributeValue("id", 0);
                    var headerNodes = row.ChildNodes;
                    string fallbackDate = string.Empty;
                    if (!DateTimeOffset.TryParse(headerNodes[0].InnerText, out DateTimeOffset dateFiled))
                    {
                        fallbackDate = headerNodes[0].InnerText;
                    }

                    int.TryParse(headerNodes[1].InnerText, out int assignedNumber);
                    var sponsor = headerNodes[2].InnerText;
                    var subject = headerNodes[3].InnerText;

                    var initiativeHeader = new InitiativeHeader(
                        id,
                        dateFiled,
                        fallbackDate,
                        assignedNumber,
                        sponsor,
                        subject);
                    result.Add(initiativeHeader);
                }

            }
        }
        return result;
    }
}
